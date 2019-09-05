using KinaUnaWeb.Models.ItemViewModels;
using KinaUnaWeb.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
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
        private int _progId = Constants.DefaultChildId;
        private readonly IProgenyHttpClient _progenyHttpClient;
        private readonly ImageStore _imageStore;
        private bool _userIsProgenyAdmin;
        private readonly string _defaultUser = Constants.DefaultUserEmail;

        public ContactsController(IProgenyHttpClient progenyHttpClient, ImageStore imageStore)
        {
            _progenyHttpClient = progenyHttpClient;
            _imageStore = imageStore;
        }

        [AllowAnonymous]
        public async Task<IActionResult> Index(int childId = 0, string tagFilter = "")
        {
            _progId = childId;
            string userEmail = HttpContext.User.FindFirst("email")?.Value ?? _defaultUser;
            
            UserInfo userinfo = await _progenyHttpClient.GetUserInfo(userEmail);
            if (childId == 0 && userinfo.ViewChild > 0)
            {
                _progId = userinfo.ViewChild;
            }
            else
            {
                _progId = childId;
            }

            if (_progId == 0)
            {
                _progId = Constants.DefaultChildId;
            }


            Progeny progeny = await _progenyHttpClient.GetProgeny(_progId);
            List<UserAccess> accessList = await _progenyHttpClient.GetProgenyAccessList(_progId);

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
                _userIsProgenyAdmin = true;
                userAccessLevel = (int)AccessLevel.Private;
            }

            List<ContactViewModel> model = new List<ContactViewModel>();
            
            List<string> tagsList = new List<string>();

            List<Contact> contactList = await _progenyHttpClient.GetContactsList(_progId, userAccessLevel); // _context.ContactsDb.AsNoTracking().Where(w => w.ProgenyId == _progId).ToList();
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
                    contactViewModel.IsAdmin = _userIsProgenyAdmin;
                    contactViewModel.ContactId = contact.ContactId;
                    contactViewModel.Context = contact.Context;
                    contactViewModel.Website = contact.Website;
                    if (contact.AddressIdNumber != null)
                    {
                        Address address = await _progenyHttpClient.GetAddress(contact.AddressIdNumber.Value); // _context.AddressDb.AsNoTracking().SingleAsync(a => a.AddressId == contact.AddressIdNumber);
                        contactViewModel.AddressLine1 = address.AddressLine1;
                        contactViewModel.AddressLine2 = address.AddressLine2;
                        contactViewModel.City = address.City;
                        contactViewModel.State = address.State;
                        contactViewModel.PostalCode = address.PostalCode;
                        contactViewModel.Country = address.Country;
                    }
                    contactViewModel.Tags = contact.Tags;
                    if (!String.IsNullOrEmpty(contactViewModel.Tags))
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
                        model.Add(contactViewModel);
                    }
                }
                model = model.OrderBy(m => m.DisplayName).ToList();

                string tags = "";
                foreach (string tstr in tagsList)
                {
                    tags = tags + tstr + ",";
                }
                ViewBag.Tags = tags.TrimEnd(',');
            }
            else
            {
                ContactViewModel notfoundContactViewModel = new ContactViewModel();
                notfoundContactViewModel.ProgenyId = _progId;
                notfoundContactViewModel.DisplayName = "No friends found.";
                notfoundContactViewModel.PictureLink = Constants.ProfilePictureUrl;
                notfoundContactViewModel.IsAdmin = _userIsProgenyAdmin;
                model.Add(notfoundContactViewModel);
            }

            model[0].Progeny = progeny;
            ViewBag.TagFilter = tagFilter;
            return View(model);

        }

        [AllowAnonymous]
        public async Task<IActionResult> ContactDetails(int contactId, string tagFilter)
        {
            string userEmail = HttpContext.User.FindFirst("email")?.Value ?? _defaultUser;

            Contact contact = await _progenyHttpClient.GetContact(contactId); // _context.ContactsDb.AsNoTracking().SingleAsync(c => c.ContactId == contactId);
            Progeny progeny = await _progenyHttpClient.GetProgeny(contact.ProgenyId);
            List<UserAccess> accessList = await _progenyHttpClient.GetProgenyAccessList(_progId);

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
                _userIsProgenyAdmin = true;
                userAccessLevel = (int)AccessLevel.Private;
            }


            ContactViewModel model = new ContactViewModel();
            
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
            if (contact.AddressIdNumber != null)
            {
                Address address = await _progenyHttpClient.GetAddress(contact.AddressIdNumber.Value); // _context.AddressDb.AsNoTracking().SingleAsync(a => a.AddressId == contact.AddressIdNumber);
                model.AddressLine1 = address.AddressLine1;
                model.AddressLine2 = address.AddressLine2;
                model.City = address.City;
                model.State = address.State;
                model.PostalCode = address.PostalCode;
                model.Country = address.Country;
            }

            model.Progeny = progeny;

            if (!model.PictureLink.StartsWith("https://"))
            {
                model.PictureLink = _imageStore.UriFor(model.PictureLink, "contacts");
            }

            List<string> tagsList = new List<string>();
            var contactsList1 = await _progenyHttpClient.GetContactsList(model.ProgenyId, userAccessLevel); // _context.ContactsDb.AsNoTracking().Where(c => c.ProgenyId == model.ProgenyId).ToList();
            foreach (Contact cont in contactsList1)
            {
                if (!String.IsNullOrEmpty(cont.Tags))
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

            ViewBag.TagsList = tagItems;
            ViewBag.TagFilter = tagFilter;

            model.IsAdmin = _userIsProgenyAdmin;

            return View(model);
        }
    }
}