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
using KinaUnaWeb.Models.TypeScriptModels.Contacts;
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
            
            List<string> tagsList = [];

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
                        List<string> contactTagsList = [.. contactViewModel.Tags.Split(',')];
                        foreach (string tagString in contactTagsList)
                        {
                            string trimmedTagString = tagString.TrimStart(' ', ',').TrimEnd(' ', ',');
                            if (!string.IsNullOrEmpty(trimmedTagString) && !tagsList.Contains(trimmedTagString))
                            { 
                                tagsList.Add(trimmedTagString);
                            }
                        }
                    }

                    contactViewModel.ContactItem.PictureLink = contactViewModel.ContactItem.GetProfilePictureUrl();

                    model.ContactsList.Add(contactViewModel);

                }
                
                model.ContactsList = [.. model.ContactsList.OrderBy(m => m.ContactItem.DisplayName)];

                model.SetTags(tagsList);
            }
            
            model.TagFilter = tagFilter;

            model.ContactsPageParameters = new()
            {
                LanguageId = model.LanguageId,
                TagFilter = tagFilter,
                TotalItems = contactList.Count,
                ProgenyId = model.CurrentProgenyId
            };

            return View(model);

        }

        [AllowAnonymous]
        public async Task<IActionResult> ViewContact(int contactId, string tagFilter, bool partialView = false)
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

            model.ContactItem.PictureLink = model.ContactItem.GetProfilePictureUrl();

            model.TagFilter = tagFilter;
            model.ContactItem.PictureLink = model.ContactItem.GetProfilePictureUrl();
            model.ContactItem.Progeny = model.CurrentProgeny;
            model.ContactItem.Progeny.PictureLink = model.ContactItem.Progeny.GetProfilePictureUrl();

            if (partialView)
            {
                return PartialView("_ContactDetailsPartial", model);
            }

            return View(model);
        }

        [AllowAnonymous]
        [HttpPost]
        public async Task<IActionResult> ContactElement([FromBody] ContactItemParameters parameters)
        {
            parameters ??= new ContactItemParameters();

            if (parameters.LanguageId == 0)
            {
                parameters.LanguageId = Request.GetLanguageIdFromCookie();
            }

            ContactViewModel contactItemResponse = new()
            {
                LanguageId = parameters.LanguageId
            };

            if (parameters.ContactId == 0)
            {
                contactItemResponse.ContactItem = new Contact { ContactId = 0 };
            }
            else
            {
                contactItemResponse.ContactItem = await contactsHttpClient.GetContact(parameters.ContactId);
                contactItemResponse.ContactItem.PictureLink = contactItemResponse.ContactItem.GetProfilePictureUrl();
                BaseItemsViewModel baseModel = await viewModelSetupService.SetupViewModel(parameters.LanguageId, User.GetEmail(), contactItemResponse.ContactItem.ProgenyId);
                contactItemResponse.IsCurrentUserProgenyAdmin = baseModel.IsCurrentUserProgenyAdmin;
            }


            return PartialView("_ContactElementPartial", contactItemResponse);

        }

        [AllowAnonymous]
        [HttpPost]
        public async Task<IActionResult> ContactsList([FromBody] ContactsPageParameters parameters)
        {
            parameters ??= new ContactsPageParameters();

            if (parameters.LanguageId == 0)
            {
                parameters.LanguageId = Request.GetLanguageIdFromCookie();
            }

            if (parameters.CurrentPageNumber < 1)
            {
                parameters.CurrentPageNumber = 1;
            }

            BaseItemsViewModel baseModel = await viewModelSetupService.SetupViewModel(parameters.LanguageId, User.GetEmail(), parameters.ProgenyId);
            List<Contact> contactsList = await contactsHttpClient.GetContactsList(parameters.ProgenyId, baseModel.CurrentAccessLevel, parameters.TagFilter);

            List<string> tagsList = [];

            if (contactsList.Count != 0)
            {
                foreach (Contact contact in contactsList)
                {
                    if (!string.IsNullOrEmpty(contact.Tags))
                    {
                        List<string> contactTagsList = [.. contact.Tags.Split(',')];
                        foreach (string tagString in contactTagsList)
                        {
                            string trimmedTagString = tagString.TrimStart(' ', ',').TrimEnd(' ', ',');
                            if (!string.IsNullOrEmpty(trimmedTagString) && !tagsList.Contains(trimmedTagString))
                            {
                                tagsList.Add(trimmedTagString);
                            }
                        }
                    }

                }
            }

            if (parameters.SortTags == 1)
            {
                tagsList = [.. tagsList.OrderBy(t => t)];
            }

            if (parameters.SortBy == 0)
            {
                contactsList = [.. contactsList.OrderBy(f => f.DateAdded)];
            }
            if (parameters.SortBy == 1)
            {
                contactsList = [.. contactsList.OrderBy(f => f.DisplayName)];
            }
            if (parameters.SortBy == 2)
            {
                contactsList = [.. contactsList.OrderBy(f => f.FirstName)];
            }
            if (parameters.SortBy == 3)
            {
                contactsList = [.. contactsList.OrderBy(f => f.LastName)];
            }

            if (parameters.Sort == 1)
            {
                contactsList.Reverse();
            }

            List<int> contactsIdList = contactsList.Select(c => c.ContactId).ToList();

            return Json(new ContactsPageResponse()
            {
                ContactsList = contactsIdList,
                PageNumber = parameters.CurrentPageNumber,
                TotalItems = contactsList.Count,
                TagsList = tagsList
            });
        }

        [AllowAnonymous]
        public async Task<FileContentResult> ProfilePicture(int id)
        {
            Contact contact = await contactsHttpClient.GetContact(id);
            BaseItemsViewModel baseModel = await viewModelSetupService.SetupViewModel(Request.GetLanguageIdFromCookie(), User.GetEmail(), contact.ProgenyId);
            ContactViewModel model = new(baseModel);

            if (string.IsNullOrEmpty(contact.PictureLink) || contact.AccessLevel < model.CurrentAccessLevel)
            {
                MemoryStream fileContentNoAccess = await imageStore.GetStream("868b62e2-6978-41a1-97dc-1cc1116f65a6.jpg");
                byte[] fileContentBytesNoAccess = fileContentNoAccess.ToArray();
                return new FileContentResult(fileContentBytesNoAccess, "image/jpeg");
            }

            MemoryStream fileContent = await imageStore.GetStream(contact.PictureLink, BlobContainers.Contacts);
            byte[] fileContentBytes = fileContent.ToArray();

            return new FileContentResult(fileContentBytes, contact.GetPictureFileContentType());
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
                string fileFormat = Path.GetExtension(model.File.FileName);
                contactItem.PictureLink = await imageStore.SaveImage(stream, BlobContainers.Contacts, fileFormat);
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

            model.ContactItem.PictureLink = model.ContactItem.GetProfilePictureUrl();
            
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
            Contact originalContact = await contactsHttpClient.GetContact(editedContact.ContactId);

            if (model.File != null && model.File.Name != string.Empty)
            {
                model.FileName = model.File.FileName;
                await using Stream stream = model.File.OpenReadStream();
                string fileFormat = Path.GetExtension(model.File.FileName);
                editedContact.PictureLink = await imageStore.SaveImage(stream, "contacts", fileFormat);
            }
            else
            {
                editedContact.PictureLink = originalContact.PictureLink;
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
            model.ContactItem.PictureLink = model.ContactItem.GetProfilePictureUrl();

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