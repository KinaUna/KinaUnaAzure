using KinaUnaProgenyApi.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
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
        private readonly IImageStore _imageStore;
        private readonly IUserInfoService _userInfoService;
        private readonly IUserAccessService _userAccessService;
        private readonly IContactService _contactService;
        private readonly ILocationService _locationService;
        private readonly ITimelineService _timelineService;
        private readonly IProgenyService _progenyService;
        private readonly IAzureNotifications _azureNotifications;
        private readonly IWebNotificationsService _webNotificationsService;

        public ContactsController(IImageStore imageStore, IAzureNotifications azureNotifications, IUserInfoService userInfoService, IUserAccessService userAccessService,
            IContactService contactService, ILocationService locationService, ITimelineService timelineService, IProgenyService progenyService, IWebNotificationsService webNotificationsService)
        {
            _imageStore = imageStore;
            _azureNotifications = azureNotifications;
            _userInfoService = userInfoService;
            _userAccessService = userAccessService;
            _contactService = contactService;
            _locationService = locationService;
            _timelineService = timelineService;
            _progenyService = progenyService;
            _webNotificationsService = webNotificationsService;
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

            value.Author = User.GetUserId();

            if (value.PictureLink == Constants.DefaultPictureLink)
            {
                value.PictureLink = Constants.ProfilePictureUrl;
            }

            if (value.Address != null)
            {
                if (value.Address.HasValues())
                {
                    value.Address = await _locationService.AddAddressItem(value.Address);
                    value.AddressIdNumber = value.Address.AddressId;
                }
            }

            Contact contactItem = await _contactService.AddContact(value);

            TimeLineItem timeLineItem = new();
            timeLineItem.CopyContactPropertiesForAdd(contactItem);

            await _timelineService.AddTimeLineItem(timeLineItem);

            UserInfo userInfo = await _userInfoService.GetUserInfoByEmail(userEmail);
            string notificationTitle = "Contact added for " + progeny.NickName;
            string notificationMessage = userInfo.FullName() + " added a new contact for " + progeny.NickName;
            await _azureNotifications.ProgenyUpdateNotification(notificationTitle, notificationMessage, timeLineItem, userInfo.ProfilePicture);
            await _webNotificationsService.SendContactNotification(contactItem, userInfo, notificationTitle);

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

            contactItem.CopyPropertiesForUpdate(value);

            if (contactItem.AddressIdNumber != null && contactItem.AddressIdNumber.Value != 0)
            {
                Address existingAddress = await _locationService.GetAddressItem(contactItem.AddressIdNumber.Value);
                if (contactItem.Address != null)
                {
                    existingAddress.CopyPropertiesForUpdate(contactItem.Address);

                    contactItem.Address = await _locationService.UpdateAddressItem(existingAddress);
                }
                else
                {
                    int removedAddressId = existingAddress.AddressId;
                    contactItem.AddressIdNumber = null;
                    await _locationService.RemoveAddressItem(removedAddressId);
                }
            }
            else
            {
                if (contactItem.Address.HasValues())
                {
                    Address address = new();
                    address.CopyPropertiesForAdd(contactItem.Address);
                    address = await _locationService.AddAddressItem(address);
                    contactItem.AddressIdNumber = address.AddressId;
                }
            }

            contactItem = await _contactService.UpdateContact(contactItem);

            contactItem.Author = User.GetUserId();

            TimeLineItem timeLineItem = await _timelineService.GetTimeLineItemByItemId(contactItem.ContactId.ToString(), (int)KinaUnaTypes.TimeLineType.Contact);
            if (timeLineItem != null && timeLineItem.CopyContactItemPropertiesForUpdate(contactItem))
            {
                _ = await _timelineService.UpdateTimeLineItem(timeLineItem);
                UserInfo userInfo = await _userInfoService.GetUserInfoByEmail(userEmail);
                string notificationTitle = "Contact edited for " + progeny.NickName;
                string notificationMessage = userInfo.FullName() + " edited a contact for " + progeny.NickName;

                await _azureNotifications.ProgenyUpdateNotification(notificationTitle, notificationMessage, timeLineItem, userInfo.ProfilePicture);
                await _webNotificationsService.SendContactNotification(contactItem, userInfo, notificationTitle);
            }

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
                Progeny progeny = await _progenyService.GetProgeny(contactItem.ProgenyId);
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

                TimeLineItem timeLineItem = await _timelineService.GetTimeLineItemByItemId(contactItem.ContactId.ToString(), (int)KinaUnaTypes.TimeLineType.Contact);
                if (timeLineItem != null)
                {
                    _ = await _timelineService.DeleteTimeLineItem(timeLineItem);
                }

                if (contactItem.AddressIdNumber != null)
                {
                    Address address = await _locationService.GetAddressItem(contactItem.AddressIdNumber.Value);
                    if (address != null)
                    {
                        await _locationService.RemoveAddressItem(address.AddressId);
                    }
                }

                await _imageStore.DeleteImage(contactItem.PictureLink, BlobContainers.Contacts);

                _ = await _contactService.DeleteContact(contactItem);

                contactItem.Author = User.GetUserId();
                UserInfo userInfo = await _userInfoService.GetUserInfoByEmail(userEmail);

                string notificationTitle = "Contact deleted for " + progeny.NickName;
                string notificationMessage = userInfo.FullName() + " deleted a contact for " + progeny.NickName + ". Contact: " + contactItem.DisplayName;

                if (timeLineItem != null)
                {
                    contactItem.AccessLevel = timeLineItem.AccessLevel = 0;
                    await _azureNotifications.ProgenyUpdateNotification(notificationTitle, notificationMessage, timeLineItem, userInfo.ProfilePicture);
                    await _webNotificationsService.SendContactNotification(contactItem, userInfo, notificationTitle);
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

                    result.PictureLink = _imageStore.UriFor(result.PictureLink, BlobContainers.Contacts);

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
                foreach (Contact contact in contactsList)
                {
                    if (contact.AddressIdNumber.HasValue)
                    {
                        contact.Address = await _locationService.GetAddressItem(contact.AddressIdNumber.Value);
                    }

                    contact.PictureLink = _imageStore.UriFor(contact.PictureLink, BlobContainers.Contacts);
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
                await using (Stream stream = await GetStreamFromUrl(contact.PictureLink))
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
            using HttpClient client = new();
            using HttpResponseMessage response = await client.GetAsync(url);
            
            return await response.Content.ReadAsStreamAsync();
        }
    }
}
