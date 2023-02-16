using KinaUnaProgenyApi.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using KinaUna.Data;
using KinaUna.Data.Extensions;
using KinaUna.Data.Models;

namespace KinaUnaProgenyApi.Controllers
{
    [Authorize(AuthenticationSchemes = "Bearer")]
    [Produces("application/json")]
    [Route("api/[controller]")]
    [ApiController]
    public class ContactsController : ControllerBase
    {
        private readonly ImageStore _imageStore;
        private readonly IUserInfoService _userInfoService;
        private readonly IUserAccessService _userAccessService;
        private readonly IContactService _contactService;
        private readonly ILocationService _locationService;
        private readonly ITimelineService _timelineService;
        private readonly IProgenyService _progenyService;
        private readonly AzureNotifications _azureNotifications;

        public ContactsController(ImageStore imageStore, AzureNotifications azureNotifications, IUserInfoService userInfoService, IUserAccessService userAccessService,
            IContactService contactService, ILocationService locationService, ITimelineService timelineService, IProgenyService progenyService)
        {
            _imageStore = imageStore;
            _azureNotifications = azureNotifications;
            _userInfoService = userInfoService;
            _userAccessService = userAccessService;
            _contactService = contactService;
            _locationService = locationService;
            _timelineService = timelineService;
            _progenyService = progenyService;
        }

        // GET api/contacts/progeny/[id]
        [HttpGet]
        [Route("[action]/{id}")]
        public async Task<IActionResult> Progeny(int id, [FromQuery] int accessLevel = 5)
        {
            string userEmail = User.GetEmail() ?? Constants.DefaultUserEmail;
            UserAccess userAccess = await _userAccessService.GetProgenyUserAccessForUser(id, userEmail);
            if (userAccess != null || id == Constants.DefaultChildId)
            {
                List<Contact> contactsList = await _contactService.GetContactsList(id);
                contactsList = contactsList.Where(c => c.AccessLevel >= (userAccess?.AccessLevel ?? accessLevel)).ToList();
                if (contactsList.Any())
                {
                    return Ok(contactsList);
                }
            }

            return NotFound();
        }

        // GET api/contacts/5
        [HttpGet("{id}")]
        public async Task<IActionResult> GetContactItem(int id)
        {
            Contact result = await _contactService.GetContact(id);

            string userEmail = User.GetEmail() ?? Constants.DefaultUserEmail;
            UserAccess userAccess = await _userAccessService.GetProgenyUserAccessForUser(result.ProgenyId, userEmail);
            if (userAccess != null || id == Constants.DefaultChildId)
            {
                return Ok(result);
            }

            return NotFound();
        }

        // POST api/contact
        [HttpPost]
        public async Task<IActionResult> Post([FromBody] Contact value)
        {
            Progeny progeny = await _progenyService.GetProgeny(value.ProgenyId);
            string userEmail = User.GetEmail() ?? Constants.DefaultUserEmail;
            if (progeny != null)
            {
            
                if (!progeny.IsInAdminList(userEmail))
                {
                    return Unauthorized();
                }
            }
            else
            {
                return NotFound();
            }

            if (value.PictureLink == Constants.DefaultPictureLink)
            {
                value.PictureLink = Constants.ProfilePictureUrl;
            }

            Contact contactItem = new Contact();
            contactItem.AccessLevel = value.AccessLevel;
            contactItem.Active = value.Active;
            contactItem.AddressString = value.AddressString;
            contactItem.ProgenyId = value.ProgenyId;
            contactItem.Author = value.Author;
            contactItem.DateAdded = value.DateAdded ?? DateTime.UtcNow;
            contactItem.Context = value.Context;
            contactItem.DisplayName = value.DisplayName;
            contactItem.Email1 = value.Email1;
            contactItem.Email2 = value.Email2;
            contactItem.FirstName = value.FirstName;
            contactItem.LastName = value.LastName;
            contactItem.MiddleName = value.MiddleName;
            contactItem.MobileNumber = value.MobileNumber;
            contactItem.Notes = value.Notes;
            contactItem.PhoneNumber = value.PhoneNumber;
            contactItem.PictureLink = value.PictureLink;
            contactItem.Tags = value.Tags;
            contactItem.Website = value.Website;
            contactItem.Address = value.Address;

            if (contactItem.Address != null)
            {
                if (contactItem.Address.AddressLine1 + contactItem.Address.AddressLine2 + contactItem.Address.City + contactItem.Address.Country + contactItem.Address.PostalCode + contactItem.Address.State != "")
                {
                    Address address = new Address();
                    address.AddressLine1 = contactItem.Address.AddressLine1;
                    address.AddressLine2 = contactItem.Address.AddressLine2;
                    address.City = contactItem.Address.City;
                    address.PostalCode = contactItem.Address.PostalCode;
                    address.State = contactItem.Address.State;
                    address.Country = contactItem.Address.Country;
                    address = await _locationService.AddAddressItem(address);
                    contactItem.AddressIdNumber = address.AddressId;
                }
            }

            contactItem = await _contactService.AddContact(contactItem);
            
            TimeLineItem tItem = new TimeLineItem();
            tItem.ProgenyId = contactItem.ProgenyId;
            tItem.AccessLevel = contactItem.AccessLevel;
            tItem.ItemType = (int)KinaUnaTypes.TimeLineType.Contact;
            tItem.ItemId = contactItem.ContactId.ToString();
            UserInfo userinfo = await _userInfoService.GetUserInfoByEmail(userEmail);
            tItem.CreatedBy = userinfo?.UserId ?? "User not found";
            tItem.CreatedTime = DateTime.UtcNow;
            if (contactItem.DateAdded.HasValue)
            {
                tItem.ProgenyTime = contactItem.DateAdded.Value;
            }
            else
            {
                tItem.ProgenyTime = DateTime.UtcNow;
            }

            await _timelineService.AddTimeLineItem(tItem);

            string title = "Contact added for " + progeny.NickName;
            if (userinfo != null)
            {
                string message = userinfo.FirstName + " " + userinfo.MiddleName + " " + userinfo.LastName + " added a new contact for " + progeny.NickName;
                await _azureNotifications.ProgenyUpdateNotification(title, message, tItem, userinfo.ProfilePicture);
            }

            return Ok(contactItem);
        }

        // PUT api/contacts/5
        [HttpPut("{id}")]
        public async Task<IActionResult> Put(int id, [FromBody] Contact value)
        {
            Contact contactItem = await _contactService.GetContact(id);
            if (contactItem == null)
            {
                return NotFound();
            }

            // Check if child exists.
            Progeny prog = await _progenyService.GetProgeny(value.ProgenyId);
            string userEmail = User.GetEmail() ?? Constants.DefaultUserEmail;
            if (prog != null)
            {
                // Check if user is allowed to edit contacts for this child.

                if (!prog.IsInAdminList(userEmail))
                {
                    return Unauthorized();
                }
            }
            else
            {
                return NotFound();
            }

            if (value.PictureLink == Constants.DefaultPictureLink)
            {
                value.PictureLink = Constants.ProfilePictureUrl;
            }

            contactItem.AccessLevel = value.AccessLevel;
            contactItem.Active = value.Active;
            contactItem.AddressIdNumber = value.AddressIdNumber;
            contactItem.AddressString = value.AddressString;
            contactItem.ProgenyId = value.ProgenyId;
            contactItem.Author = value.Author;
            if (value.DateAdded.HasValue)
            {
                contactItem.DateAdded = value.DateAdded.Value;
            }
            else
            {
                contactItem.DateAdded = DateTime.UtcNow;
            }
            
            contactItem.Context = value.Context;
            contactItem.DisplayName = value.DisplayName;
            contactItem.Email1 = value.Email1;
            contactItem.Email2 = value.Email2;
            contactItem.FirstName = value.FirstName;
            contactItem.LastName = value.LastName;
            contactItem.MiddleName = value.MiddleName;
            contactItem.MobileNumber = value.MobileNumber;
            contactItem.Notes = value.Notes;
            contactItem.PhoneNumber = value.PhoneNumber;
            if (value.PictureLink != Constants.KeepExistingLink)
            {
                contactItem.PictureLink = value.PictureLink;
            }
            contactItem.Tags = value.Tags;
            contactItem.Website = value.Website;
            contactItem.Address = value.Address;
            if (contactItem.AddressIdNumber != null && contactItem.AddressIdNumber.Value != 0)
            {
                Address addressOld = await _locationService.GetAddressItem(contactItem.AddressIdNumber.Value);
                if (contactItem.Address != null)
                {
                    addressOld.AddressLine1 = contactItem.Address.AddressLine1;
                    addressOld.AddressLine2 = contactItem.Address.AddressLine2;
                    addressOld.City = contactItem.Address.City;
                    addressOld.PostalCode = contactItem.Address.PostalCode;
                    addressOld.State = contactItem.Address.State;
                    addressOld.Country = contactItem.Address.Country;
                    contactItem.Address = addressOld;

                    await _locationService.UpdateAddressItem(addressOld);
                    
                }
                else
                {
                    int removedAddressId = addressOld.AddressId;
                    contactItem.AddressIdNumber = null;
                    await _locationService.RemoveAddressItem(removedAddressId);
                }
            }
            else
            {
                if (contactItem.Address.AddressLine1 + contactItem.Address.AddressLine2 + contactItem.Address.City + contactItem.Address.Country + contactItem.Address.PostalCode + contactItem.Address.State !=
                    "")
                {
                    Address address = new Address();
                    address.AddressLine1 = contactItem.Address.AddressLine1;
                    address.AddressLine2 = contactItem.Address.AddressLine2;
                    address.City = contactItem.Address.City;
                    address.PostalCode = contactItem.Address.PostalCode;
                    address.State = contactItem.Address.State;
                    address.Country = contactItem.Address.Country;
                    address = await _locationService.AddAddressItem(address);
                    contactItem.AddressIdNumber = address.AddressId;
                }
            }

            contactItem = await _contactService.UpdateContact(contactItem);
            
            TimeLineItem tItem = await _timelineService.GetTimeLineItemByItemId(contactItem.ContactId.ToString(), (int)KinaUnaTypes.TimeLineType.Contact);
            if (tItem != null && contactItem.DateAdded.HasValue)
            {
                tItem.ProgenyTime = contactItem.DateAdded.Value;
                tItem.AccessLevel = contactItem.AccessLevel;
                _ = await _timelineService.UpdateTimeLineItem(tItem);
            }

            UserInfo userinfo = await _userInfoService.GetUserInfoByEmail(userEmail);
            string title = "Contact edited for " + prog.NickName;
            string message = userinfo.FirstName + " " + userinfo.MiddleName + " " + userinfo.LastName + " edited a contact for " + prog.NickName;
            await _azureNotifications.ProgenyUpdateNotification(title, message, tItem, userinfo.ProfilePicture);

            return Ok(contactItem);
        }

        // DELETE api/contacts/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            Contact contactItem = await _contactService.GetContact(id);
            if (contactItem != null)
            {
                string userEmail = User.GetEmail() ?? Constants.DefaultUserEmail;
                Progeny prog = await _progenyService.GetProgeny(contactItem.ProgenyId);
                if (prog != null)
                {
                    if (!prog.IsInAdminList(userEmail))
                    {
                        return Unauthorized();
                    }
                }
                else
                {
                    return NotFound();
                }

                TimeLineItem tItem = await _timelineService.GetTimeLineItemByItemId(contactItem.ContactId.ToString(), (int)KinaUnaTypes.TimeLineType.Contact);
                if (tItem != null)
                {
                    _ = await _timelineService.DeleteTimeLineItem(tItem);
                }

                if (contactItem.AddressIdNumber != null)
                {
                    Address address = await _locationService.GetAddressItem(contactItem.AddressIdNumber.Value);
                    if (address != null)
                    {
                        await _locationService.RemoveAddressItem(address.AddressId);
                    }
                }
                if (!contactItem.PictureLink.ToLower().StartsWith("http"))
                {
                    await _imageStore.DeleteImage(contactItem.PictureLink, BlobContainers.Contacts);
                }

                _ = await _contactService.DeleteContact(contactItem);
                
                UserInfo userinfo = await _userInfoService.GetUserInfoByEmail(userEmail);
                string title = "Contact deleted for " + prog.NickName;
                string message = userinfo.FirstName + " " + userinfo.MiddleName + " " + userinfo.LastName + " deleted a contact for " + prog.NickName + ". Contact: " + contactItem.DisplayName;
                if (tItem != null)
                {
                    tItem.AccessLevel = 0;
                    await _azureNotifications.ProgenyUpdateNotification(title, message, tItem, userinfo.ProfilePicture);
                }

                return NoContent();
            }

            return NotFound();
        }

        [HttpGet("[action]/{id}")]
        public async Task<IActionResult> GetContactMobile(int id)
        {
            Contact result = await _contactService.GetContact(id);

            if (result != null)
            {
                string userEmail = User.GetEmail() ?? Constants.DefaultUserEmail;
                UserAccess userAccess = await _userAccessService.GetProgenyUserAccessForUser(result.ProgenyId, userEmail);

                if (userAccess != null || result.ProgenyId == Constants.DefaultChildId)
                {
                    if (result.AddressIdNumber.HasValue)
                    {
                        result.Address = await _locationService.GetAddressItem(result.AddressIdNumber.Value);
                    }
                    if (!result.PictureLink.ToLower().StartsWith("http"))
                    {
                        result.PictureLink = _imageStore.UriFor(result.PictureLink, BlobContainers.Contacts);
                    }
                    return Ok(result);
                }
            }
            
            return NotFound();
        }

        [HttpGet]
        [Route("[action]/{id}/{accessLevel}")]
        public async Task<IActionResult> ProgenyMobile(int id, int accessLevel = 5)
        {
            List<Contact> contactsList = await _contactService.GetContactsList(id); 
            contactsList = contactsList.Where(c => c.AccessLevel >= accessLevel).ToList();

            string userEmail = User.GetEmail() ?? Constants.DefaultUserEmail;
            UserAccess userAccess = await _userAccessService.GetProgenyUserAccessForUser(id, userEmail); 
            
            if ((userAccess != null || id == Constants.DefaultChildId) && contactsList.Any())
            {
                foreach (Contact cont in contactsList)
                {
                    if (cont.AddressIdNumber.HasValue)
                    {
                        cont.Address = await _locationService.GetAddressItem(cont.AddressIdNumber.Value);
                    }

                    if (!cont.PictureLink.ToLower().StartsWith("http"))
                    {
                        cont.PictureLink = _imageStore.UriFor(cont.PictureLink, BlobContainers.Contacts);
                    }
                }
                return Ok(contactsList);
            }

            return Ok(new List<Contact>());
        }

        [HttpGet]
        [Route("[action]/{contactId}")]
        public async Task<IActionResult> DownloadPicture(int contactId)
        {
            Contact contact = await _contactService.GetContact(contactId);
            if (contact == null)
            {
                return NotFound();
            }

            string userEmail = User.GetEmail() ?? Constants.DefaultUserEmail;
            UserAccess userAccess = await _userAccessService.GetProgenyUserAccessForUser(contact.ProgenyId, userEmail);

            if (userAccess != null && userAccess.AccessLevel > 0 && contact.PictureLink.ToLower().StartsWith("http"))
            {
                using (Stream stream = await GetStreamFromUrl(contact.PictureLink))
                {
                    contact.PictureLink = await _imageStore.SaveImage(stream, BlobContainers.Contacts);
                }

                contact = await _contactService.UpdateContact(contact);
                
                return Ok(contact);
            }

            return NotFound();
        }

        private static async Task<Stream> GetStreamFromUrl(string url)
        {
            using HttpClient client = new HttpClient();
            using HttpResponseMessage response = await client.GetAsync(url);
            await using Stream streamToReadFrom = await response.Content.ReadAsStreamAsync();
            return streamToReadFrom;
        }
    }
}
