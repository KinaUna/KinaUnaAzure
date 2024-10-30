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
using KinaUnaWeb.Models;
using KinaUnaWeb.Models.TypeScriptModels.Contacts;
using KinaUnaWeb.Services.HttpClients;

namespace KinaUnaWeb.Controllers
{
    /// <summary>
    /// Provides page and API endpoints for managing contacts.
    /// </summary>
    /// <param name="imageStore"></param>
    /// <param name="locationsHttpClient"></param>
    /// <param name="contactsHttpClient"></param>
    /// <param name="viewModelSetupService"></param>
    public class ContactsController(ImageStore imageStore,
        ILocationsHttpClient locationsHttpClient,
        IContactsHttpClient contactsHttpClient,
        IViewModelSetupService viewModelSetupService,
        IProgenyHttpClient progenyHttpClient)
        : Controller
    {
        /// <summary>
        /// Index page for contacts. Shows a list of contacts
        /// </summary>
        /// <param name="childId">The Id of the Progeny to show Contacts for.</param>
        /// <param name="tagFilter">Filter the list of contacts by Tags. If empty string shows all contacts.</param>
        /// <param name="sort">Sort order. 0 = oldest first, 1 = newest first.</param>
        /// <param name="sortBy">Property to sort Contacts by. 0 = Date, 1 = DisplayName, 2 = FirstName, 3 = LastName.</param>
        /// <param name="sortTags">Sort the list of all tags. 0 = no sorting, 1 = sort alphabetically.</param>
        /// <param name="contactId">The ContactId of the Contact to show details for. If 0, no contact details popup is shown.</param>
        /// <returns>View with ContactListViewModel.</returns>
        [AllowAnonymous]
        public async Task<IActionResult> Index(int childId = 0, string tagFilter = "", int sort = 0, int sortBy = 0, int sortTags = 0, int contactId = 0)
        {
            BaseItemsViewModel baseModel = await viewModelSetupService.SetupViewModel(Request.GetLanguageIdFromCookie(), User.GetEmail(), childId);
            ContactListViewModel model = new(baseModel);
            
            List<Contact> contactList = await contactsHttpClient.GetContactsList(model.CurrentProgenyId, tagFilter);
            
            model.TagFilter = tagFilter;

            model.ContactsPageParameters = new()
            {
                LanguageId = model.LanguageId,
                TagFilter = tagFilter,
                TotalItems = contactList.Count,
                ProgenyId = model.CurrentProgenyId,
                Sort = sort,
                SortBy = sortBy,
                SortTags = sortTags
            };

            model.ContactId = contactId;

            return View(model);

        }

        /// <summary>
        /// Shows details for a single contact.
        /// </summary>
        /// <param name="contactId">The ContactId of the Contact to display.</param>
        /// <param name="tagFilter">Filter tags. Empty string includes all tags.</param>
        /// <param name="partialView">Return partial view, for fetching HTML inline to show in a modal/popup.</param>
        /// <returns>View or PartialView with ContactViewModel.</returns>
        [AllowAnonymous]
        public async Task<IActionResult> ViewContact(int contactId, string tagFilter, bool partialView = false)
        {
            Contact contact = await contactsHttpClient.GetContact(contactId);
            BaseItemsViewModel baseModel = await viewModelSetupService.SetupViewModel(Request.GetLanguageIdFromCookie(), User.GetEmail(), contact.ProgenyId);
            ContactViewModel model = new(baseModel);
            
            if (contact.AccessLevel < model.CurrentAccessLevel)
            {
                if (partialView)
                {
                    return PartialView("_AccessDeniedPartial");
                }

                return View("_AccessDeniedPartial");
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

        /// <summary>
        /// Gets a partial view with a Contact element, for contact lists to fetch HTML for each contact.
        /// </summary>
        /// <param name="parameters">ContactItemParameters object with the Contact details.</param>
        /// <returns>PartialView with ContactViewModel.</returns>
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
                contactItemResponse.ContactItem.Progeny = await progenyHttpClient.GetProgeny(contactItemResponse.ContactItem.ProgenyId);
            }


            return PartialView("_ContactElementPartial", contactItemResponse);

        }

        /// <summary>
        /// HttpPost endpoint for fetching a list of Contacts.
        /// </summary>
        /// <param name="parameters">ContactsPageParameters object.</param>
        /// <returns>Json of ContactsPageResponse</returns>
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
            
            List<Contact> contactsList = [];

            foreach (int progenyId in parameters.Progenies)
            {
                List<Contact> progenyContacts = await contactsHttpClient.GetContactsList(progenyId, parameters.TagFilter);
                contactsList.AddRange(progenyContacts);
            }

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
                contactsList = [.. contactsList.OrderBy(c => c.DateAdded)];
            }
            if (parameters.SortBy == 1)
            {
                contactsList = [.. contactsList.OrderBy(c => c.DisplayName)];
            }
            if (parameters.SortBy == 2)
            {
                contactsList = [.. contactsList.OrderBy(c => c.FirstName)];
            }
            if (parameters.SortBy == 3)
            {
                contactsList = [.. contactsList.OrderBy(c => c.LastName)];
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

        /// <summary>
        /// Gets the image file for a Contact's profile picture.
        /// Checks if the user has access to the Contact. If not, returns a default image.
        /// </summary>
        /// <param name="id">The ContactId of the Contact to get a profile picture for.</param>
        /// <returns>FileContentResult with the image file.</returns>
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

        /// <summary>
        /// Page for adding a new Contact.
        /// </summary>
        /// <returns>View with ContactViewModel.</returns>
        [HttpGet]
        public async Task<IActionResult> AddContact()
        {
            BaseItemsViewModel baseModel = await viewModelSetupService.SetupViewModel(Request.GetLanguageIdFromCookie(), User.GetEmail(), 0);
            ContactViewModel model = new(baseModel);

            if (model.CurrentUser == null)
            {
                return PartialView("_AccessDeniedPartial");
            }

            model.ProgenyList = await viewModelSetupService.GetProgenySelectList(model.CurrentUser);
            model.SetProgenyList();
            
            model.SetAccessLevelList();

            return PartialView("_AddContactPartial" , model);
        }

        /// <summary>
        /// HttpPost endpoint for adding a new Contact.
        /// </summary>
        /// <param name="model">ContactViewModel with the properties for the new Contact.</param>
        /// <returns></returns>
        [HttpPost]
        [ValidateAntiForgeryToken]
        [RequestSizeLimit(100_000_000)]
        public async Task<IActionResult> AddContact(ContactViewModel model)
        {
            BaseItemsViewModel baseModel = await viewModelSetupService.SetupViewModel(Request.GetLanguageIdFromCookie(), User.GetEmail(), model.ContactItem.ProgenyId);
            model.SetBaseProperties(baseModel);
            
            if (!model.CurrentProgeny.IsInAdminList(model.CurrentUser.UserEmail))
            {
                return PartialView("_AccessDeniedPartial");
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
            
            model.ContactItem = await contactsHttpClient.AddContact(contactItem);
            if (model.ContactItem.DateAdded.HasValue)
            {
                model.ContactItem.DateAdded = TimeZoneInfo.ConvertTimeFromUtc(model.ContactItem.DateAdded.Value, TimeZoneInfo.FindSystemTimeZoneById(model.CurrentUser.Timezone));
            }

            if (model.ContactItem.AddressIdNumber.HasValue)
            {
                model.ContactItem.Address = await locationsHttpClient.GetAddress(model.ContactItem.AddressIdNumber.Value);
            }

            return PartialView("_ContactAddedPartial", model);
        }

        /// <summary>
        /// Page for editing a Contact.
        /// </summary>
        /// <param name="itemId">The ContactId of the Contact to update.</param>
        /// <returns>View with ContactViewModel.</returns>
        [HttpGet]
        public async Task<IActionResult> EditContact(int itemId)
        {
            Contact contact = await contactsHttpClient.GetContact(itemId);
            BaseItemsViewModel baseModel = await viewModelSetupService.SetupViewModel(Request.GetLanguageIdFromCookie(), User.GetEmail(), contact.ProgenyId);
            ContactViewModel model = new(baseModel);
            
            if (!model.CurrentProgeny.IsInAdminList(model.CurrentUser.UserEmail))
            {
                return PartialView("_AccessDeniedPartial");
            }

            if (contact.AddressIdNumber != null)
            {
                contact.Address = await locationsHttpClient.GetAddress(contact.AddressIdNumber.Value);
            }
            
            model.SetPropertiesFromContact(contact, model.IsCurrentUserProgenyAdmin);

            model.ContactItem.PictureLink = model.ContactItem.GetProfilePictureUrl();
            
            model.SetAccessLevelList();

            return PartialView("_EditContactPartial", model);
        }

        /// <summary>
        /// HttpPost endpoint for updating a Contact.
        /// </summary>
        /// <param name="model">ContactViewModel with the updated properties for the Contact.</param>
        /// <returns>Redirects to Contacts/Index page.</returns>
        [HttpPost]
        [ValidateAntiForgeryToken]
        [RequestSizeLimit(100_000_000)]
        public async Task<IActionResult> EditContact(ContactViewModel model)
        {
            BaseItemsViewModel baseModel = await viewModelSetupService.SetupViewModel(Request.GetLanguageIdFromCookie(), User.GetEmail(), model.ContactItem.ProgenyId);
            model.SetBaseProperties(baseModel);

            if (!model.CurrentProgeny.IsInAdminList(model.CurrentUser.UserEmail))
            {
                return PartialView("_AccessDeniedPartial");
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

            model.ContactItem = await contactsHttpClient.UpdateContact(editedContact);
            if (model.ContactItem.DateAdded.HasValue)
            {
                model.ContactItem.DateAdded = TimeZoneInfo.ConvertTimeFromUtc(model.ContactItem.DateAdded.Value, TimeZoneInfo.FindSystemTimeZoneById(model.CurrentUser.Timezone));
            }

            if (model.ContactItem.AddressIdNumber.HasValue)
            {
                model.ContactItem.Address = await locationsHttpClient.GetAddress(model.ContactItem.AddressIdNumber.Value);
            }

            return PartialView("_ContactUpdatedPartial", model);
        }

        /// <summary>
        /// Page for deleting a Contact.
        /// </summary>
        /// <param name="itemId">The ContactId of the Contact to delete.</param>
        /// <returns>View with ContactViewModel.</returns>
        [HttpGet]
        public async Task<IActionResult> DeleteContact(int itemId)
        {
            Contact contact = await contactsHttpClient.GetContact(itemId);
            BaseItemsViewModel baseModel = await viewModelSetupService.SetupViewModel(Request.GetLanguageIdFromCookie(), User.GetEmail(), contact.ProgenyId);
            ContactViewModel model = new(baseModel);
            
            if (!model.CurrentProgeny.IsInAdminList(model.CurrentUser.UserEmail))
            {
                return PartialView("_AccessDeniedPartial");
            }

            model.ContactItem = contact;
            model.ContactItem.PictureLink = model.ContactItem.GetProfilePictureUrl();

            return View(model);
        }

        /// <summary>
        /// HttpPost endpoint for deleting a Contact.
        /// </summary>
        /// <param name="model">ContactViewModel with the properties of the Contact to delete.</param>
        /// <returns>Redirects to Contacts/Index page.</returns>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteContact(ContactViewModel model)
        {
            Contact contact = await contactsHttpClient.GetContact(model.ContactItem.ContactId);
            BaseItemsViewModel baseModel = await viewModelSetupService.SetupViewModel(Request.GetLanguageIdFromCookie(), User.GetEmail(), contact.ProgenyId);
            model.SetBaseProperties(baseModel);
            
            if (!model.CurrentProgeny.IsInAdminList(model.CurrentUser.UserEmail))
            {
                return PartialView("_AccessDeniedPartial");
            }

            _ = await contactsHttpClient.DeleteContact(contact.ContactId);

            return RedirectToAction("Index", "Contacts");
        }

        /// <summary>
        /// Page for copying a Contact.
        /// </summary>
        /// <param name="itemId">The ContactId of the Contact to copy.</param>
        /// <returns>View with ContactViewModel.</returns>
        [HttpGet]
        public async Task<IActionResult> CopyContact(int itemId)
        {
            Contact contact = await contactsHttpClient.GetContact(itemId);
            BaseItemsViewModel baseModel = await viewModelSetupService.SetupViewModel(Request.GetLanguageIdFromCookie(), User.GetEmail(), contact.ProgenyId);
            ContactViewModel model = new(baseModel);

            if (model.CurrentAccessLevel > contact.AccessLevel)
            {
                return PartialView("_AccessDeniedPartial");
            }

            model.ProgenyList = await viewModelSetupService.GetProgenySelectList(model.CurrentUser);
            model.SetProgenyList();

            if (contact.AddressIdNumber != null)
            {
                contact.Address = await locationsHttpClient.GetAddress(contact.AddressIdNumber.Value);
            }

            model.SetPropertiesFromContact(contact, model.IsCurrentUserProgenyAdmin);

            model.ContactItem.PictureLink = model.ContactItem.GetProfilePictureUrl();

            model.SetAccessLevelList();

            return PartialView("_CopyContactPartial", model);
        }

        /// <summary>
        /// HttpPost endpoint for copying a Contact.
        /// </summary>
        /// <param name="model">ContactViewModel with the properties for the Contact to add.</param>
        /// <returns>PartialView with the added Contact item.</returns>
        [HttpPost]
        [ValidateAntiForgeryToken]
        [RequestSizeLimit(100_000_000)]
        public async Task<IActionResult> CopyContact(ContactViewModel model)
        {
            BaseItemsViewModel baseModel = await viewModelSetupService.SetupViewModel(Request.GetLanguageIdFromCookie(), User.GetEmail(), model.ContactItem.ProgenyId);
            model.SetBaseProperties(baseModel);

            if (!model.CurrentProgeny.IsInAdminList(model.CurrentUser.UserEmail))
            {
                return PartialView("_AccessDeniedPartial");
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

            editedContact.ContactId = 0;
            editedContact.AddressIdNumber = 0;
            if (editedContact.Address != null)
            {
                editedContact.Address.AddressId = 0;
            }

            model.ContactItem = await contactsHttpClient.AddContact(editedContact);
            if (model.ContactItem.DateAdded.HasValue)
            {
                model.ContactItem.DateAdded = TimeZoneInfo.ConvertTimeFromUtc(model.ContactItem.DateAdded.Value, TimeZoneInfo.FindSystemTimeZoneById(model.CurrentUser.Timezone));
            }

            if (model.ContactItem.AddressIdNumber.HasValue)
            {
                model.ContactItem.Address = await locationsHttpClient.GetAddress(model.ContactItem.AddressIdNumber.Value);
            }

            return PartialView("_ContactCopiedPartial", model);
        }
    }
}