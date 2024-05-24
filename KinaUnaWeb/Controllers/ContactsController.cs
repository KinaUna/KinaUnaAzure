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
using KinaUnaWeb.Models;
using KinaUnaWeb.Services.HttpClients;

namespace KinaUnaWeb.Controllers
{
    public class ContactsController(ImageStore imageStore, ILocationsHttpClient locationsHttpClient, IContactsHttpClient contactsHttpClient, IViewModelSetupService viewModelSetupService)
        : Controller
    {
        [AllowAnonymous]
        public async Task<IActionResult> Index(int childId = 0, string tagFilter = "")
        {
            BaseItemsViewModel baseModel = await viewModelSetupService.SetupViewModel(Request.GetLanguageIdFromCookie(), User.GetEmail(), childId);
            ContactListViewModel model = new(baseModel);
            
            List<string> tagsList = new();

            List<Contact> contactList = await contactsHttpClient.GetContactsList(model.CurrentProgenyId, model.CurrentAccessLevel, tagFilter);
            
            if (contactList.Count != 0)
            {
                foreach (Contact contact in contactList)
                {
                    if (contact.AddressIdNumber.HasValue)
                    {
                        contact.Address = await locationsHttpClient.GetAddress(contact.AddressIdNumber.Value);
                    }

                    ContactViewModel contactViewModel = new(contact, model.IsCurrentUserProgenyAdmin, model.CurrentUser);
                    
                    if (!string.IsNullOrEmpty(contactViewModel.Tags))
                    {
                        List<string> cvmTags = contactViewModel.Tags.Split(',').ToList();
                        foreach (string tagString in cvmTags)
                        {
                            string trimmedTagString = tagString.TrimStart(' ', ',').TrimEnd(' ', ',');
                            if (!string.IsNullOrEmpty(trimmedTagString) && !tagsList.Contains(trimmedTagString))
                            { 
                                tagsList.Add(trimmedTagString);
                            }
                        }
                    }

                    contactViewModel.ContactItem.PictureLink = imageStore.UriFor(contactViewModel.ContactItem.PictureLink, "contacts");

                    model.ContactsList.Add(contactViewModel);

                }
                
                model.ContactsList = model.ContactsList.OrderBy(m => m.ContactItem.DisplayName).ToList();

                model.SetTags(tagsList);
            }
            
            model.TagFilter = tagFilter;
            
            return View(model);

        }

        [AllowAnonymous]
        public async Task<IActionResult> ContactDetails(int contactId, string tagFilter)
        {
            Contact contact = await contactsHttpClient.GetContact(contactId);
            BaseItemsViewModel baseModel = await viewModelSetupService.SetupViewModel(Request.GetLanguageIdFromCookie(), User.GetEmail(), contact.ProgenyId);
            ContactViewModel model = new(baseModel);
            
            if (contact.AccessLevel < model.CurrentAccessLevel)
            {
                RedirectToAction("Index");
            }

            if (contact.AddressIdNumber.HasValue)
            {
                contact.Address = await locationsHttpClient.GetAddress(contact.AddressIdNumber.Value);
            }

            model.SetPropertiesFromContact(contact, model.IsCurrentUserProgenyAdmin);

            model.ContactItem.PictureLink = imageStore.UriFor(model.ContactItem.PictureLink, "contacts");

            List<string> tagsList = new();
            List<Contact> contactsList1 = await contactsHttpClient.GetContactsList(model.CurrentProgenyId, model.CurrentAccessLevel); 
            foreach (Contact cont in contactsList1)
            {
                if (!string.IsNullOrEmpty(cont.Tags))
                {
                    List<string> cvmTags = cont.Tags.Split(',').ToList();
                    foreach (string tagString in cvmTags)
                    {
                        string trimmedTagString = tagString.TrimStart(' ', ',').TrimEnd(' ', ',');
                        if (!string.IsNullOrEmpty(trimmedTagString) && !tagsList.Contains(trimmedTagString))
                        {
                            tagsList.Add(trimmedTagString);
                        }
                    }
                }
            }

            model.SetTagList(tagsList);
            model.TagFilter = tagFilter;
            
            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> AddContact()
        {
            BaseItemsViewModel baseModel = await viewModelSetupService.SetupViewModel(Request.GetLanguageIdFromCookie(), User.GetEmail(), 0);
            ContactViewModel model = new(baseModel);

            if (model.CurrentUser == null)
            {
                return RedirectToAction("Index");
            }

            model.ProgenyList = await viewModelSetupService.GetProgenySelectList(model.CurrentUser);
            model.SetProgenyList();
            
            model.SetAccessLevelList();

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [RequestSizeLimit(100_000_000)]
        public async Task<IActionResult> AddContact(ContactViewModel model)
        {
            BaseItemsViewModel baseModel = await viewModelSetupService.SetupViewModel(Request.GetLanguageIdFromCookie(), User.GetEmail(), model.ContactItem.ProgenyId);
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
                contactItem.PictureLink = await imageStore.SaveImage(stream, BlobContainers.Contacts);
            }
            else
            {
                contactItem.PictureLink = Constants.ProfilePictureUrl;
            }
            
            _ = await contactsHttpClient.AddContact(contactItem);
            
            return RedirectToAction("Index", "Contacts");
        }

        [HttpGet]
        public async Task<IActionResult> EditContact(int itemId)
        {
            Contact contact = await contactsHttpClient.GetContact(itemId);
            BaseItemsViewModel baseModel = await viewModelSetupService.SetupViewModel(Request.GetLanguageIdFromCookie(), User.GetEmail(), contact.ProgenyId);
            ContactViewModel model = new(baseModel);
            
            if (!model.CurrentProgeny.IsInAdminList(model.CurrentUser.UserEmail))
            {
                // Todo: Show no access info.
                return RedirectToAction("Index");
            }

            if (contact.AddressIdNumber != null)
            {
                contact.Address = await locationsHttpClient.GetAddress(contact.AddressIdNumber.Value);
            }
            
            model.SetPropertiesFromContact(contact, model.IsCurrentUserProgenyAdmin);

            model.ContactItem.PictureLink = imageStore.UriFor(contact.PictureLink, "contacts");
            
            model.SetAccessLevelList();

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [RequestSizeLimit(100_000_000)]
        public async Task<IActionResult> EditContact(ContactViewModel model)
        {
            BaseItemsViewModel baseModel = await viewModelSetupService.SetupViewModel(Request.GetLanguageIdFromCookie(), User.GetEmail(), model.ContactItem.ProgenyId);
            model.SetBaseProperties(baseModel);

            if (!model.CurrentProgeny.IsInAdminList(model.CurrentUser.UserEmail))
            {
                // Todo: Show no access info.
                return RedirectToAction("Index");
            }

            Contact editedContact = model.CreateContact();

            if (model.File != null && model.File.Name != string.Empty)
            {
                Contact originalContact = await contactsHttpClient.GetContact(model.ContactItem.ContactId);
                model.FileName = model.File.FileName;
                await using (Stream stream = model.File.OpenReadStream())
                {
                    editedContact.PictureLink = await imageStore.SaveImage(stream, "contacts");
                }

                await imageStore.DeleteImage(originalContact.PictureLink, "contacts");
            }
            else
            {

                editedContact.PictureLink = Constants.KeepExistingLink;
            }

            _ = await contactsHttpClient.UpdateContact(editedContact);

            return RedirectToAction("Index", "Contacts");
        }

        [HttpGet]
        public async Task<IActionResult> DeleteContact(int itemId)
        {
            Contact contact = await contactsHttpClient.GetContact(itemId);
            BaseItemsViewModel baseModel = await viewModelSetupService.SetupViewModel(Request.GetLanguageIdFromCookie(), User.GetEmail(), contact.ProgenyId);
            ContactViewModel model = new(baseModel);
            
            if (!model.CurrentProgeny.IsInAdminList(model.CurrentUser.UserEmail))
            {
                // Todo: Show no access info.
                return RedirectToAction("Index");
            }

            model.ContactItem = contact;
            model.ContactItem.PictureLink = imageStore.UriFor(model.ContactItem.PictureLink, BlobContainers.Contacts);

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteContact(ContactViewModel model)
        {
            Contact contact = await contactsHttpClient.GetContact(model.ContactItem.ContactId);
            BaseItemsViewModel baseModel = await viewModelSetupService.SetupViewModel(Request.GetLanguageIdFromCookie(), User.GetEmail(), contact.ProgenyId);
            model.SetBaseProperties(baseModel);
            
            if (!model.CurrentProgeny.IsInAdminList(model.CurrentUser.UserEmail))
            {
                // Todo: Show no access info.
                return RedirectToAction("Index");
            }

            _ = await contactsHttpClient.DeleteContact(contact.ContactId);

            return RedirectToAction("Index", "Contacts");
        }
    }
}