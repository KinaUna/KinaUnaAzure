using System;
using KinaUnaWeb.Models.ItemViewModels;
using KinaUnaWeb.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using KinaUna.Data;
using KinaUna.Data.Extensions;
using KinaUna.Data.Models;
using KinaUna.Data.Contexts;
using Microsoft.AspNetCore.Mvc.Rendering;

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
        private readonly WebDbContext _context;
        private readonly IPushMessageSender _pushMessageSender;

        public ContactsController(IProgenyHttpClient progenyHttpClient, ImageStore imageStore, IUserInfosHttpClient userInfosHttpClient, ILocationsHttpClient locationsHttpClient,
            IContactsHttpClient contactsHttpClient, IUserAccessHttpClient userAccessHttpClient, IPushMessageSender pushMessageSender, WebDbContext context)
        {
            _progenyHttpClient = progenyHttpClient;
            _imageStore = imageStore;
            _userInfosHttpClient = userInfosHttpClient;
            _locationsHttpClient = locationsHttpClient;
            _contactsHttpClient = contactsHttpClient;
            _userAccessHttpClient = userAccessHttpClient;
            _pushMessageSender = pushMessageSender;
            _context = context;
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
            List<Contact> contactsList1 = await _contactsHttpClient.GetContactsList(model.ProgenyId, userAccessLevel); 
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

        [HttpGet]
        public async Task<IActionResult> AddContact()
        {
            ContactViewModel model = new ContactViewModel();
            model.LanguageId = Request.GetLanguageIdFromCookie();
            string userEmail = User.GetEmail();
            model.CurrentUser = await _userInfosHttpClient.GetUserInfo(userEmail);

            List<string> tagsList = new List<string>();

            if (model.CurrentUser == null)
            {
                return RedirectToAction("Index");
            }

            List<Progeny> accessList = new List<Progeny>();
            if (User.Identity != null && User.Identity.IsAuthenticated && userEmail != null && model.CurrentUser.UserId != null)
            {

                accessList = await _progenyHttpClient.GetProgenyAdminList(userEmail);
                if (accessList.Any())
                {
                    foreach (Progeny prog in accessList)
                    {
                        SelectListItem selItem = new SelectListItem()
                        {
                            Text = accessList.Single(p => p.Id == prog.Id).NickName,
                            Value = prog.Id.ToString()
                        };
                        if (prog.Id == model.CurrentUser.ViewChild)
                        {
                            selItem.Selected = true;
                        }

                        model.ProgenyList.Add(selItem);
                    }
                }
            }
            foreach (Progeny item in accessList)
            {
                List<Contact> contactsList1 = await _contactsHttpClient.GetContactsList(item.Id, 0);
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
            }

            string tagItems = "[";
            if (tagsList.Any())
            {
                foreach (string tagstring in tagsList)
                {
                    tagItems = tagItems + "'" + tagstring + "',";
                }

                tagItems = tagItems.Remove(tagItems.Length - 1);

            }
            tagItems = tagItems + "]";
            ViewBag.TagsList = tagItems;

            if (model.LanguageId == 2)
            {
                model.AccessLevelListEn = model.AccessLevelListDe;
            }

            if (model.LanguageId == 3)
            {
                model.AccessLevelListEn = model.AccessLevelListDa;
            }

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [RequestSizeLimit(100_000_000)]
        public async Task<IActionResult> AddContact(ContactViewModel model)
        {
            model.LanguageId = Request.GetLanguageIdFromCookie();
            string userEmail = User.GetEmail();
            model.CurrentUser = await _userInfosHttpClient.GetUserInfo(userEmail);

            Progeny prog = await _progenyHttpClient.GetProgeny(model.ProgenyId);
            if (!prog.IsInAdminList(model.CurrentUser.UserEmail))
            {
                // Todo: Show no access info.
                return RedirectToAction("Index");
            }

            Contact contactItem = new Contact();
            contactItem.FirstName = model.FirstName;
            contactItem.MiddleName = model.MiddleName;
            contactItem.LastName = model.LastName;
            contactItem.DisplayName = model.DisplayName;
            contactItem.Email1 = model.Email1;
            contactItem.Email2 = model.Email2;
            contactItem.PhoneNumber = model.PhoneNumber;
            contactItem.MobileNumber = model.MobileNumber;
            contactItem.Notes = model.Notes;
            contactItem.Website = model.Website;
            contactItem.Active = true;
            contactItem.Context = model.Context;
            contactItem.AccessLevel = model.AccessLevel;
            contactItem.Author = model.CurrentUser.UserId;
            contactItem.ProgenyId = model.ProgenyId;
            contactItem.DateAdded = DateTime.UtcNow;
            if (!string.IsNullOrEmpty(model.Tags))
            {
                contactItem.Tags = model.Tags.TrimEnd(',', ' ').TrimStart(',', ' ');
            }
            if (model.File != null)
            {
                model.FileName = model.File.FileName;
                using (Stream stream = model.File.OpenReadStream())
                {
                    model.PictureLink = await _imageStore.SaveImage(stream, "contacts");
                }
            }
            else
            {
                contactItem.PictureLink = Constants.ProfilePictureUrl;
            }

            if (model.AddressLine1 + model.AddressLine2 + model.City + model.Country + model.PostalCode + model.State !=
                "")
            {
                Address address = new Address();
                address.AddressLine1 = model.AddressLine1;
                address.AddressLine2 = model.AddressLine2;
                address.City = model.City;
                address.PostalCode = model.PostalCode;
                address.State = model.State;
                address.Country = model.Country;
                contactItem.Address = address;
            }

            await _contactsHttpClient.AddContact(contactItem);

            string authorName = "";
            if (!string.IsNullOrEmpty(model.CurrentUser.FirstName))
            {
                authorName = model.CurrentUser.FirstName;
            }
            if (!string.IsNullOrEmpty(model.CurrentUser.MiddleName))
            {
                authorName = authorName + " " + model.CurrentUser.MiddleName;
            }
            if (!string.IsNullOrEmpty(model.CurrentUser.LastName))
            {
                authorName = authorName + " " + model.CurrentUser.LastName;
            }

            authorName = authorName.Trim();
            if (string.IsNullOrEmpty(authorName))
            {
                authorName = model.CurrentUser.UserName;
            }
            List<UserAccess> usersToNotif = await _userAccessHttpClient.GetProgenyAccessList(model.ProgenyId);
            Progeny progeny = await _progenyHttpClient.GetProgeny(model.ProgenyId);
            foreach (UserAccess ua in usersToNotif)
            {
                if (ua.AccessLevel <= contactItem.AccessLevel)
                {
                    UserInfo uaUserInfo = await _userInfosHttpClient.GetUserInfo(ua.UserId);
                    if (uaUserInfo.UserId != "Unknown")
                    {
                        WebNotification notification = new WebNotification();
                        notification.To = uaUserInfo.UserId;
                        notification.From = authorName;
                        notification.Message = "Name: " + contactItem.DisplayName + "\r\nContext: " + contactItem.Context;
                        notification.DateTime = DateTime.UtcNow;
                        notification.Icon = model.CurrentUser.ProfilePicture;
                        notification.Title = "A new contact was added for " + progeny.NickName;
                        notification.Link = "/Contacts/ContactDetails?contactId=" + contactItem.ContactId + "&childId=" + progeny.Id;
                        notification.Type = "Notification";
                        await _context.WebNotificationsDb.AddAsync(notification);
                        await _context.SaveChangesAsync();

                        await _pushMessageSender.SendMessage(uaUserInfo.UserId, notification.Title,
                            notification.Message, Constants.WebAppUrl + notification.Link, "kinaunacontact" + progeny.Id);
                    }
                }
            }
            return RedirectToAction("Index", "Contacts");
        }

        [HttpGet]
        public async Task<IActionResult> EditContact(int itemId)
        {
            ContactViewModel model = new ContactViewModel();
            model.LanguageId = Request.GetLanguageIdFromCookie();
            string userEmail = User.GetEmail();
            model.CurrentUser = await _userInfosHttpClient.GetUserInfo(userEmail);

            Contact contact = await _contactsHttpClient.GetContact(itemId);
            Progeny prog = await _progenyHttpClient.GetProgeny(contact.ProgenyId);
            if (!prog.IsInAdminList(model.CurrentUser.UserEmail))
            {
                // Todo: Show no access info.
                return RedirectToAction("Index");
            }
            model.ContactId = contact.ContactId;
            model.Active = contact.Active;
            model.ProgenyId = contact.ProgenyId;
            model.AccessLevel = contact.AccessLevel;
            model.Author = contact.Author;
            model.FirstName = contact.FirstName;
            model.MiddleName = contact.MiddleName;
            model.LastName = contact.LastName;
            model.DisplayName = contact.DisplayName;
            if (contact.AddressIdNumber != null)
            {
                model.AddressIdNumber = contact.AddressIdNumber;
                model.Address = await _locationsHttpClient.GetAddress(model.AddressIdNumber.Value);
                model.AddressLine1 = model.Address.AddressLine1;
                model.AddressLine2 = model.Address.AddressLine2;
                model.City = model.Address.City;
                model.PostalCode = model.Address.PostalCode;
                model.State = model.Address.State;
                model.Country = model.Address.Country;
            }
            model.Email1 = contact.Email1;
            model.Email2 = contact.Email2;
            model.PhoneNumber = contact.PhoneNumber;
            model.MobileNumber = contact.MobileNumber;
            model.Website = contact.Website;
            model.Notes = contact.Notes;
            model.AccessLevelListEn[model.AccessLevel].Selected = true;
            model.AccessLevelListDa[model.AccessLevel].Selected = true;
            model.AccessLevelListDe[model.AccessLevel].Selected = true;
            model.Context = contact.Context;
            model.Notes = contact.Notes;
            model.PictureLink = contact.PictureLink;
            if (!contact.PictureLink.ToLower().StartsWith("http"))
            {
                model.PictureLink = _imageStore.UriFor(contact.PictureLink, "contacts");
            }
            DateTime tempTime = contact.DateAdded ?? DateTime.UtcNow;
            model.DateAdded = TimeZoneInfo.ConvertTimeFromUtc(tempTime, TimeZoneInfo.FindSystemTimeZoneById(model.CurrentUser.Timezone));
            model.Tags = contact.Tags;

            List<string> tagsList = new List<string>();
            List<Contact> contactsList1 = await _contactsHttpClient.GetContactsList(model.ProgenyId, 0);
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

            ViewBag.TagsList = tagItems;

            if (model.LanguageId == 2)
            {
                model.AccessLevelListEn = model.AccessLevelListDe;
            }

            if (model.LanguageId == 3)
            {
                model.AccessLevelListEn = model.AccessLevelListDa;
            }

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [RequestSizeLimit(100_000_000)]
        public async Task<IActionResult> EditContact(ContactViewModel model)
        {
            model.LanguageId = Request.GetLanguageIdFromCookie();
            string userEmail = User.GetEmail();
            model.CurrentUser = await _userInfosHttpClient.GetUserInfo(userEmail);

            Progeny prog = await _progenyHttpClient.GetProgeny(model.ProgenyId);
            if (!prog.IsInAdminList(model.CurrentUser.UserEmail))
            {
                // Todo: Show no access info.
                return RedirectToAction("Index");
            }
            if (ModelState.IsValid)
            {
                Contact editedContact = await _contactsHttpClient.GetContact(model.ContactId);
                editedContact.ContactId = model.ContactId;
                editedContact.ProgenyId = model.ProgenyId;
                editedContact.Active = model.Active;
                editedContact.AccessLevel = model.AccessLevel;
                editedContact.Author = model.Author;
                editedContact.FirstName = model.FirstName;
                editedContact.MiddleName = model.MiddleName;
                editedContact.LastName = model.LastName;
                editedContact.DisplayName = model.DisplayName;
                editedContact.DateAdded = model.DateAdded;
                editedContact.AddressIdNumber = model.AddressIdNumber;
                if (model.AddressLine1 + model.AddressLine2 + model.City + model.Country + model.PostalCode +
                    model.State != "")
                {
                    Address address = new Address();
                    address.AddressLine1 = model.AddressLine1;
                    address.AddressLine2 = model.AddressLine2;
                    address.City = model.City;
                    address.PostalCode = model.PostalCode;
                    address.State = model.State;
                    address.Country = model.Country;
                    editedContact.Address = address;
                }

                editedContact.Email1 = model.Email1;
                editedContact.Email2 = model.Email2;
                editedContact.PhoneNumber = model.PhoneNumber;
                editedContact.MobileNumber = model.MobileNumber;
                editedContact.Notes = model.Notes;
                editedContact.Context = model.Context;
                editedContact.Website = model.Website;
                if (model.File != null && model.File.Name != string.Empty)
                {
                    string oldPictureLink = model.PictureLink;
                    model.FileName = model.File.FileName;
                    using (Stream stream = model.File.OpenReadStream())
                    {
                        editedContact.PictureLink = await _imageStore.SaveImage(stream, "contacts");
                    }

                    if (!oldPictureLink.ToLower().StartsWith("http"))
                    {
                        await _imageStore.DeleteImage(oldPictureLink, "contacts");
                    }
                }

                if (editedContact.DateAdded == null)
                {
                    editedContact.DateAdded = DateTime.UtcNow;
                }
                if (!string.IsNullOrEmpty(model.Tags))
                {
                    editedContact.Tags = model.Tags.TrimEnd(',', ' ').TrimStart(',', ' ');
                }

                await _contactsHttpClient.UpdateContact(editedContact);
            }

            return RedirectToAction("Index", "Contacts");
        }

        [HttpGet]
        public async Task<IActionResult> DeleteContact(int itemId)
        {
            ContactViewModel model = new ContactViewModel();
            model.LanguageId = Request.GetLanguageIdFromCookie();
            string userEmail = User.GetEmail();
            model.CurrentUser = await _userInfosHttpClient.GetUserInfo(userEmail);

            model.Contact = await _contactsHttpClient.GetContact(itemId);

            Progeny prog = await _progenyHttpClient.GetProgeny(model.Contact.ProgenyId);
            if (!prog.IsInAdminList(model.CurrentUser.UserEmail))
            {
                // Todo: Show no access info.
                return RedirectToAction("Index");
            }
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteContact(ContactViewModel model)
        {
            model.LanguageId = Request.GetLanguageIdFromCookie();
            string userEmail = User.GetEmail();
            model.CurrentUser = await _userInfosHttpClient.GetUserInfo(userEmail);

            Contact contact = await _contactsHttpClient.GetContact(model.Contact.ContactId);
            Progeny prog = await _progenyHttpClient.GetProgeny(contact.ProgenyId);
            if (!prog.IsInAdminList(model.CurrentUser.UserEmail))
            {
                // Todo: Show no access info.
                return RedirectToAction("Index");
            }

            await _contactsHttpClient.DeleteContact(contact.ContactId);

            return RedirectToAction("Index", "Contacts");
        }
    }
}