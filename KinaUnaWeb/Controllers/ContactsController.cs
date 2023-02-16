﻿using KinaUnaWeb.Models.ItemViewModels;
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
using Microsoft.AspNetCore.Mvc.Rendering;
using KinaUnaWeb.Models;

namespace KinaUnaWeb.Controllers
{
    public class ContactsController : Controller
    {
        private readonly ILocationsHttpClient _locationsHttpClient;
        private readonly IContactsHttpClient _contactsHttpClient;
        private readonly ImageStore _imageStore;
        private readonly INotificationsService _notificationsService;
        private readonly IViewModelSetupService _viewModelSetupService;
        public ContactsController(ImageStore imageStore, ILocationsHttpClient locationsHttpClient, IContactsHttpClient contactsHttpClient,
            INotificationsService notificationsService, IViewModelSetupService viewModelSetupService)
        {
            _imageStore = imageStore;
            _locationsHttpClient = locationsHttpClient;
            _contactsHttpClient = contactsHttpClient;
            _notificationsService = notificationsService;
            _viewModelSetupService = viewModelSetupService;
        }

        [AllowAnonymous]
        public async Task<IActionResult> Index(int childId = 0, string tagFilter = "")
        {
            BaseItemsViewModel baseModel = await _viewModelSetupService.SetupViewModel(Request.GetLanguageIdFromCookie(), User.GetEmail(), childId);
            ContactListViewModel model = new ContactListViewModel(baseModel);
            
            List<string> tagsList = new List<string>();

            List<Contact> contactList = await _contactsHttpClient.GetContactsList(model.CurrentProgenyId);

            if (!string.IsNullOrEmpty(tagFilter))
            {
                contactList = contactList.Where(c => c.Tags != null && c.Tags.ToUpper().Contains(tagFilter.ToUpper())).ToList();
            }

            if (contactList.Count != 0)
            {
                foreach (Contact contact in contactList)
                {
                    if (contact.AddressIdNumber.HasValue)
                    {
                        contact.Address = await _locationsHttpClient.GetAddress(contact.AddressIdNumber.Value);
                    }

                    ContactViewModel contactViewModel = new ContactViewModel(contact, model.IsCurrentUserProgenyAdmin, model.CurrentUser);
                    
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

                    contactViewModel.PictureLink = _imageStore.UriFor(contactViewModel.PictureLink, "contacts");

                    model.ContactsList.Add(contactViewModel);

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
                notfoundContactViewModel.CurrentProgenyId = model.CurrentProgenyId;
                notfoundContactViewModel.DisplayName = "No friends found.";
                notfoundContactViewModel.PictureLink = Constants.ProfilePictureUrl;
                notfoundContactViewModel.IsCurrentUserProgenyAdmin = model.IsCurrentUserProgenyAdmin;
                model.ContactsList.Add(notfoundContactViewModel);
            }

            model.TagFilter = tagFilter;
            
            return View(model);

        }

        [AllowAnonymous]
        public async Task<IActionResult> ContactDetails(int contactId, string tagFilter)
        {
            Contact contact = await _contactsHttpClient.GetContact(contactId);
            BaseItemsViewModel baseModel = await _viewModelSetupService.SetupViewModel(Request.GetLanguageIdFromCookie(), User.GetEmail(), contact.ProgenyId);
            ContactViewModel model = new ContactViewModel(baseModel);
            
            if (contact.AccessLevel < model.CurrentAccessLevel)
            {
                RedirectToAction("Index");
            }

            if (contact.AddressIdNumber.HasValue)
            {
                contact.Address = await _locationsHttpClient.GetAddress(contact.AddressIdNumber.Value);
            }

            model.SetPropertiesFromContact(contact, model.IsCurrentUserProgenyAdmin);

            model.PictureLink = _imageStore.UriFor(model.PictureLink, "contacts");

            List<string> tagsList = new List<string>();
            List<Contact> contactsList1 = await _contactsHttpClient.GetContactsList(model.CurrentProgenyId, model.CurrentAccessLevel); 
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
            BaseItemsViewModel baseModel = await _viewModelSetupService.SetupViewModel(Request.GetLanguageIdFromCookie(), User.GetEmail(), 0);
            ContactViewModel model = new ContactViewModel(baseModel);

            if (model.CurrentUser == null)
            {
                return RedirectToAction("Index");
            }

            if (User.Identity != null && User.Identity.IsAuthenticated && model.CurrentUser.UserEmail != null && model.CurrentUser.UserId != null)
            {
                model.ProgenyList = await _viewModelSetupService.GetProgenySelectList(model.CurrentUser);
            }

            List<string> tagsList = new List<string>();
            foreach (SelectListItem item in model.ProgenyList)
            {
                if (int.TryParse(item.Value, out int progenyId))
                {
                    List<Contact> contactsList1 = await _contactsHttpClient.GetContactsList(progenyId, 0);
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
            }

            model.TagsList = "[";
            if (tagsList.Any())
            {
                foreach (string tagstring in tagsList)
                {
                    model.TagsList = model.TagsList + "'" + tagstring + "',";
                }

                model.TagsList = model.TagsList.Remove(model.TagsList.Length - 1);

            }
            model.TagsList = model.TagsList + "]";
            
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [RequestSizeLimit(100_000_000)]
        public async Task<IActionResult> AddContact(ContactViewModel model)
        {
            BaseItemsViewModel baseModel = await _viewModelSetupService.SetupViewModel(Request.GetLanguageIdFromCookie(), User.GetEmail(), model.CurrentProgenyId);
            model.SetBaseProperties(baseModel);

            
            if (!model.CurrentProgeny.IsInAdminList(model.CurrentUser.UserEmail))
            {
                // Todo: Show no access info.
                return RedirectToAction("Index");
            }

            Contact contactItem = model.CreateContact();
            
            if (model.File != null)
            {
                model.FileName = model.File.FileName;
                await using Stream stream = model.File.OpenReadStream();
                contactItem.PictureLink = await _imageStore.SaveImage(stream, BlobContainers.Contacts);
            }
            else
            {
                contactItem.PictureLink = Constants.ProfilePictureUrl;
            }
            
            contactItem = await _contactsHttpClient.AddContact(contactItem);

            await _notificationsService.SendContactNotification(contactItem, model.CurrentUser);
            
            return RedirectToAction("Index", "Contacts");
        }

        [HttpGet]
        public async Task<IActionResult> EditContact(int itemId)
        {
            Contact contact = await _contactsHttpClient.GetContact(itemId);
            BaseItemsViewModel baseModel = await _viewModelSetupService.SetupViewModel(Request.GetLanguageIdFromCookie(), User.GetEmail(), contact.ProgenyId);
            ContactViewModel model = new ContactViewModel(baseModel);
            
            if (!model.CurrentProgeny.IsInAdminList(model.CurrentUser.UserEmail))
            {
                // Todo: Show no access info.
                return RedirectToAction("Index");
            }

            if (contact.AddressIdNumber != null)
            {
                contact.Address = await _locationsHttpClient.GetAddress(contact.AddressIdNumber.Value);
            }

            model.PictureLink = _imageStore.UriFor(contact.PictureLink, "contacts");

            model.SetPropertiesFromContact(contact, model.IsCurrentUserProgenyAdmin);
            model.SetAccessLevelList();
            
            List<string> tagsList = new List<string>();
            List<Contact> contactsList1 = await _contactsHttpClient.GetContactsList(model.CurrentProgenyId, 0);
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
            
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [RequestSizeLimit(100_000_000)]
        public async Task<IActionResult> EditContact(ContactViewModel model)
        {
            BaseItemsViewModel baseModel = await _viewModelSetupService.SetupViewModel(Request.GetLanguageIdFromCookie(), User.GetEmail(), model.CurrentProgenyId);
            model.SetBaseProperties(baseModel);

            if (!model.CurrentProgeny.IsInAdminList(model.CurrentUser.UserEmail))
            {
                // Todo: Show no access info.
                return RedirectToAction("Index");
            }
            
            if (ModelState.IsValid)
            {
                Contact editedContact = model.CreateContact();
                
                if (model.File != null && model.File.Name != string.Empty)
                {
                    Contact originalContact = await _contactsHttpClient.GetContact(model.ContactId);
                    model.FileName = model.File.FileName;
                    await using (Stream stream = model.File.OpenReadStream())
                    {
                        editedContact.PictureLink = await _imageStore.SaveImage(stream, "contacts");
                    }

                    await _imageStore.DeleteImage(originalContact.PictureLink, "contacts");
                }
                else
                {

                    editedContact.PictureLink = Constants.KeepExistingLink;
                }
                
                await _contactsHttpClient.UpdateContact(editedContact);
            }

            return RedirectToAction("Index", "Contacts");
        }

        [HttpGet]
        public async Task<IActionResult> DeleteContact(int itemId)
        {
            Contact contact = await _contactsHttpClient.GetContact(itemId);
            BaseItemsViewModel baseModel = await _viewModelSetupService.SetupViewModel(Request.GetLanguageIdFromCookie(), User.GetEmail(), contact.ProgenyId);
            ContactViewModel model = new ContactViewModel(baseModel);
            
            if (!model.CurrentProgeny.IsInAdminList(model.CurrentUser.UserEmail))
            {
                // Todo: Show no access info.
                return RedirectToAction("Index");
            }

            model.Contact = contact;

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteContact(ContactViewModel model)
        {
            Contact contact = await _contactsHttpClient.GetContact(model.Contact.ContactId);
            BaseItemsViewModel baseModel = await _viewModelSetupService.SetupViewModel(Request.GetLanguageIdFromCookie(), User.GetEmail(), contact.ProgenyId);
            model.SetBaseProperties(baseModel);
            
            if (!model.CurrentProgeny.IsInAdminList(model.CurrentUser.UserEmail))
            {
                // Todo: Show no access info.
                return RedirectToAction("Index");
            }

            await _contactsHttpClient.DeleteContact(contact.ContactId);

            return RedirectToAction("Index", "Contacts");
        }
    }
}