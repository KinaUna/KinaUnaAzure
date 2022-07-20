using KinaUnaWeb.Models.ItemViewModels;
using KinaUnaWeb.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using KinaUna.Data;
using KinaUna.Data.Extensions;
using KinaUna.Data.Models;

namespace KinaUnaWeb.Controllers
{
    public class ContactsController : Controller
    {
        private readonly IProgenyHttpClient _progenyHttpClient;
        private readonly IUserInfosHttpClient _userInfosHttpClient;
        private readonly ILocationsHttpClient _locationsHttpClient;
        private readonly IContactsHttpClient _contactsHttpClient;
        private readonly IUserAccessHttpClient _userAccessHttpClient;
        private readonly ImageStore _imageStore;
        
        public ContactsController(IProgenyHttpClient progenyHttpClient, ImageStore imageStore, IUserInfosHttpClient userInfosHttpClient, ILocationsHttpClient locationsHttpClient, IContactsHttpClient contactsHttpClient, IUserAccessHttpClient userAccessHttpClient)
        {
            _progenyHttpClient = progenyHttpClient;
            _imageStore = imageStore;
            _userInfosHttpClient = userInfosHttpClient;
            _locationsHttpClient = locationsHttpClient;
            _contactsHttpClient = contactsHttpClient;
            _userAccessHttpClient = userAccessHttpClient;
        }

        [AllowAnonymous]
        public async Task<IActionResult> Index(int childId = 0, string tagFilter = "")
        {
            ContactListViewModel model = new ContactListViewModel();
            model.LanguageId = Request.GetLanguageIdFromCookie();
            string userEmail = User.GetEmail();
            model.CurrentUser = await _userInfosHttpClient.GetUserInfo(userEmail);
            
            if (childId == 0 && model.CurrentUser.ViewChild > 0)
            {
                childId = model.CurrentUser.ViewChild;
            }
            
            if (childId == 0)
            {
                childId = Constants.DefaultChildId;
            }


            Progeny progeny = await _progenyHttpClient.GetProgeny(childId);
            List<UserAccess> accessList = await _userAccessHttpClient.GetProgenyAccessList(childId);

            int userAccessLevel = (int)AccessLevel.Public;
            if (accessList.Count != 0)
            {
                UserAccess userAccess = accessList.SingleOrDefault(u => u.UserId.ToUpper() == userEmail.ToUpper());
                if (userAccess != null)
                {
                    userAccessLevel = userAccess.AccessLevel;
                }
            }

            
            if (progeny.IsInAdminList(userEmail))
            {
                model.IsAdmin = true;
                userAccessLevel = (int)AccessLevel.Private;
            }
            
            List<string> tagsList = new List<string>();

            List<Contact> contactList = await _contactsHttpClient.GetContactsList(childId, userAccessLevel);
            if (!string.IsNullOrEmpty(tagFilter))
            {
                contactList = contactList.Where(c => c.Tags != null && c.Tags.ToUpper().Contains(tagFilter.ToUpper())).ToList();
            }

            if (contactList.Count != 0)
            {
                foreach (Contact contact in contactList)
                {
                    ContactViewModel contactViewModel = new ContactViewModel();
                    contactViewModel.ProgenyId = contact.ProgenyId;
                    contactViewModel.AccessLevel = contact.AccessLevel;
                    contactViewModel.FirstName = contact.FirstName;
                    contactViewModel.MiddleName = contact.MiddleName;
                    contactViewModel.LastName = contact.LastName;
                    contactViewModel.DisplayName = contact.DisplayName;
                    contactViewModel.AddressIdNumber = contact.AddressIdNumber;
                    contactViewModel.Email1 = contact.Email1;
                    contactViewModel.Email2 = contact.Email2;
                    contactViewModel.PhoneNumber = contact.PhoneNumber;
                    contactViewModel.MobileNumber = contact.MobileNumber;
                    contactViewModel.Notes = contact.Notes;
                    contactViewModel.PictureLink = contact.PictureLink;
                    contactViewModel.Active = contact.Active;
                    contactViewModel.IsAdmin = model.IsAdmin;
                    contactViewModel.ContactId = contact.ContactId;
                    contactViewModel.Context = contact.Context;
                    contactViewModel.Website = contact.Website;
                    if (contact.AddressIdNumber != null && contact.AddressIdNumber > 0)
                    {
                        Address address = await _locationsHttpClient.GetAddress(contact.AddressIdNumber.Value);
                        if (address != null)
                        {
                            contactViewModel.AddressLine1 = address.AddressLine1;
                            contactViewModel.AddressLine2 = address.AddressLine2;
                            contactViewModel.City = address.City;
                            contactViewModel.State = address.State;
                            contactViewModel.PostalCode = address.PostalCode;
                            contactViewModel.Country = address.Country;
                        }
                        
                    }
                    contactViewModel.Tags = contact.Tags;
                    if (!string.IsNullOrEmpty(contactViewModel.Tags))
                    {
                        List<string> cvmTags = contactViewModel.Tags.Split(',').ToList();
                        foreach (string tagstring in cvmTags)
                        {
                            if (!tagsList.Contains(tagstring.TrimStart(' ', ',').TrimEnd(' ', ',')))
                            {
                                tagsList.Add(tagstring.TrimStart(' ', ',').TrimEnd(' ', ','));
                            }
                        }
                    }

                    if (!contactViewModel.PictureLink.StartsWith("https://"))
                    {
                        contactViewModel.PictureLink = _imageStore.UriFor(contactViewModel.PictureLink, "contacts");
                    }

                    if (contactViewModel.AccessLevel >= userAccessLevel)
                    {
                        model.ContactsList.Add(contactViewModel);
                    }
                }
                model.ContactsList = model.ContactsList.OrderBy(m => m.DisplayName).ToList();

                string tags = "";
                foreach (string tstr in tagsList)
                {
                    tags = tags + tstr + ",";
                }
                model.Tags = tags.TrimEnd(',');
            }
            else
            {
                ContactViewModel notfoundContactViewModel = new ContactViewModel();
                notfoundContactViewModel.ProgenyId = childId;
                notfoundContactViewModel.DisplayName = "No friends found.";
                notfoundContactViewModel.PictureLink = Constants.ProfilePictureUrl;
                notfoundContactViewModel.IsAdmin = model.IsAdmin;
                model.ContactsList.Add(notfoundContactViewModel);
            }

            model.Progeny = progeny;
            model.TagFilter = tagFilter;
            return View(model);

        }

        [AllowAnonymous]
        public async Task<IActionResult> ContactDetails(int contactId, string tagFilter)
        {
            ContactViewModel model = new ContactViewModel();
            model.LanguageId = Request.GetLanguageIdFromCookie();
            string userEmail = User.GetEmail();
            model.CurrentUser = await _userInfosHttpClient.GetUserInfo(userEmail);

            Contact contact = await _contactsHttpClient.GetContact(contactId);
            Progeny progeny = await _progenyHttpClient.GetProgeny(contact.ProgenyId);
            List<UserAccess> accessList = await _userAccessHttpClient.GetProgenyAccessList(contact.ProgenyId);

            int userAccessLevel =  (int)AccessLevel.Public;
            if (accessList.Count != 0)
            {
                UserAccess userAccess = accessList.SingleOrDefault(u => u.UserId.ToUpper() == userEmail.ToUpper());
                if (userAccess != null)
                {
                    userAccessLevel = userAccess.AccessLevel;
                }
            }

            if (progeny.IsInAdminList(userEmail))
            {
                model.IsAdmin = true;
                userAccessLevel = (int)AccessLevel.Private;
            }
            
            if (contact.AccessLevel < userAccessLevel)
            {
                RedirectToAction("Index");
            }

            model.ProgenyId = contact.ProgenyId;
            model.Context = contact.Context;
            model.Notes = contact.Notes;
            model.AccessLevel = contact.AccessLevel;
            model.ContactId = contact.ContactId;
            model.PictureLink = contact.PictureLink;
            model.Active = contact.Active;
            model.FirstName = contact.FirstName;
            model.MiddleName = contact.MiddleName;
            model.LastName = contact.LastName;
            model.DisplayName = contact.DisplayName;
            model.AddressIdNumber = contact.AddressIdNumber;
            model.Email1 = contact.Email1;
            model.Email2 = contact.Email2;
            model.PhoneNumber = contact.PhoneNumber;
            model.MobileNumber = contact.MobileNumber;
            model.Website = contact.Website;
            model.Tags = contact.Tags;
            if (contact.AddressIdNumber != null && contact.AddressIdNumber > 0)
            {
                Address address = await _locationsHttpClient.GetAddress(contact.AddressIdNumber.Value);
                if (address != null)
                {
                    model.AddressLine1 = address.AddressLine1;
                    model.AddressLine2 = address.AddressLine2;
                    model.City = address.City;
                    model.State = address.State;
                    model.PostalCode = address.PostalCode;
                    model.Country = address.Country;
                }
            }

            model.Progeny = progeny;

            if (!model.PictureLink.StartsWith("https://"))
            {
                model.PictureLink = _imageStore.UriFor(model.PictureLink, "contacts");
            }

            List<string> tagsList = new List<string>();
            var contactsList1 = await _contactsHttpClient.GetContactsList(model.ProgenyId, userAccessLevel); 
            foreach (Contact cont in contactsList1)
            {
                if (!string.IsNullOrEmpty(cont.Tags))
                {
                    List<string> cvmTags = cont.Tags.Split(',').ToList();
                    foreach (string tagstring in cvmTags)
                    {
                        if (!tagsList.Contains(tagstring.TrimStart(' ', ',').TrimEnd(' ', ',')))
                        {
                            tagsList.Add(tagstring.TrimStart(' ', ',').TrimEnd(' ', ','));
                        }
                    }
                }
            }

            string tagItems = "[";
            if (tagsList.Any())
            {
                foreach (string tagstring in tagsList)
                {
                    tagItems = tagItems + "'" + tagstring + "',";
                }

                tagItems = tagItems.Remove(tagItems.Length - 1);
                tagItems = tagItems + "]";
            }

            model.TagsList = tagItems;
            model.TagFilter = tagFilter;
            
            return View(model);
        }
    }
}