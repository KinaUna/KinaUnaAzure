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
    public class ContactsController(
        IImageStore imageStore,
        IAzureNotifications azureNotifications,
        IUserInfoService userInfoService,
        IUserAccessService userAccessService,
        IContactService contactService,
        ILocationService locationService,
        ITimelineService timelineService,
        IProgenyService progenyService,
        IWebNotificationsService webNotificationsService)
        : ControllerBase
    {
        // GET api/contacts/progeny/[id]
        [HttpGet]
        [Route("[action]/{id:int}")]
        public async Task<IActionResult> Progeny(int id, [FromQuery] int accessLevel = 5)
        {
            string userEmail = User.GetEmail() ?? Constants.DefaultUserEmail;
            UserAccess userAccess = await userAccessService.GetProgenyUserAccessForUser(id, userEmail);
            if (userAccess == null && id != Constants.DefaultChildId) return NotFound();

            List<Contact> contactsList = await contactService.GetContactsList(id);
            contactsList = contactsList.Where(c => c.AccessLevel >= (userAccess?.AccessLevel ?? accessLevel)).ToList();
            if (contactsList.Count != 0)
            {
                return Ok(contactsList);
            }

            return NotFound();
        }

        // GET api/contacts/5
        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetContactItem(int id)
        {
            Contact result = await contactService.GetContact(id);

            string userEmail = User.GetEmail() ?? Constants.DefaultUserEmail;
            UserAccess userAccess = await userAccessService.GetProgenyUserAccessForUser(result.ProgenyId, userEmail);
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
            Progeny progeny = await progenyService.GetProgeny(value.ProgenyId);
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
                    value.Address = await locationService.AddAddressItem(value.Address);
                    value.AddressIdNumber = value.Address.AddressId;
                }
            }

            Contact contactItem = await contactService.AddContact(value);

            TimeLineItem timeLineItem = new();
            timeLineItem.CopyContactPropertiesForAdd(contactItem);

            await timelineService.AddTimeLineItem(timeLineItem);

            UserInfo userInfo = await userInfoService.GetUserInfoByEmail(userEmail);
            string notificationTitle = "Contact added for " + progeny.NickName;
            string notificationMessage = userInfo.FullName() + " added a new contact for " + progeny.NickName;
            await azureNotifications.ProgenyUpdateNotification(notificationTitle, notificationMessage, timeLineItem, userInfo.ProfilePicture);
            await webNotificationsService.SendContactNotification(contactItem, userInfo, notificationTitle);

            return Ok(contactItem);
        }

        // PUT api/contacts/5
        [HttpPut("{id:int}")]
        public async Task<IActionResult> Put(int id, [FromBody] Contact value)
        {
            Contact contactItem = await contactService.GetContact(id);
            if (contactItem == null)
            {
                return NotFound();
            }

            Progeny progeny = await progenyService.GetProgeny(value.ProgenyId);
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
                Address existingAddress = await locationService.GetAddressItem(contactItem.AddressIdNumber.Value);
                if (contactItem.Address != null)
                {
                    existingAddress.CopyPropertiesForUpdate(contactItem.Address);

                    contactItem.Address = await locationService.UpdateAddressItem(existingAddress);
                }
                else
                {
                    int removedAddressId = existingAddress.AddressId;
                    contactItem.AddressIdNumber = null;
                    await locationService.RemoveAddressItem(removedAddressId);
                }
            }
            else
            {
                if (contactItem.Address.HasValues())
                {
                    Address address = new();
                    address.CopyPropertiesForAdd(contactItem.Address);
                    address = await locationService.AddAddressItem(address);
                    contactItem.AddressIdNumber = address.AddressId;
                }
            }

            contactItem = await contactService.UpdateContact(contactItem);

            contactItem.Author = User.GetUserId();

            TimeLineItem timeLineItem = await timelineService.GetTimeLineItemByItemId(contactItem.ContactId.ToString(), (int)KinaUnaTypes.TimeLineType.Contact);
            if (timeLineItem == null || !timeLineItem.CopyContactItemPropertiesForUpdate(contactItem)) return Ok(contactItem);

            _ = await timelineService.UpdateTimeLineItem(timeLineItem);
            
            //UserInfo userInfo = await userInfoService.GetUserInfoByEmail(userEmail);
            //string notificationTitle = "Contact edited for " + progeny.NickName;
            //string notificationMessage = userInfo.FullName() + " edited a contact for " + progeny.NickName;

            // await azureNotifications.ProgenyUpdateNotification(notificationTitle, notificationMessage, timeLineItem, userInfo.ProfilePicture);
            // await webNotificationsService.SendContactNotification(contactItem, userInfo, notificationTitle);

            return Ok(contactItem);
        }

        // DELETE api/contacts/5
        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(int id)
        {
            Contact contactItem = await contactService.GetContact(id);
            if (contactItem == null) return NotFound();

            string userEmail = User.GetEmail() ?? Constants.DefaultUserEmail;
            Progeny progeny = await progenyService.GetProgeny(contactItem.ProgenyId);
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

            TimeLineItem timeLineItem = await timelineService.GetTimeLineItemByItemId(contactItem.ContactId.ToString(), (int)KinaUnaTypes.TimeLineType.Contact);
            if (timeLineItem != null)
            {
                _ = await timelineService.DeleteTimeLineItem(timeLineItem);
            }

            if (contactItem.AddressIdNumber != null)
            {
                Address address = await locationService.GetAddressItem(contactItem.AddressIdNumber.Value);
                if (address != null)
                {
                    await locationService.RemoveAddressItem(address.AddressId);
                }
            }

            await imageStore.DeleteImage(contactItem.PictureLink, BlobContainers.Contacts);

            _ = await contactService.DeleteContact(contactItem);

            contactItem.Author = User.GetUserId();
            UserInfo userInfo = await userInfoService.GetUserInfoByEmail(userEmail);

            string notificationTitle = "Contact deleted for " + progeny.NickName;
            string notificationMessage = userInfo.FullName() + " deleted a contact for " + progeny.NickName + ". Contact: " + contactItem.DisplayName;

            if (timeLineItem == null) return NoContent();

            contactItem.AccessLevel = timeLineItem.AccessLevel = 0;
            await azureNotifications.ProgenyUpdateNotification(notificationTitle, notificationMessage, timeLineItem, userInfo.ProfilePicture);
            await webNotificationsService.SendContactNotification(contactItem, userInfo, notificationTitle);

            return NoContent();

        }

        [HttpGet("[action]/{id:int}")]
        public async Task<IActionResult> GetContactMobile(int id)
        {
            Contact contact = await contactService.GetContact(id);

            if (contact == null) return NotFound();

            string userEmail = User.GetEmail() ?? Constants.DefaultUserEmail;
            UserAccess userAccess = await userAccessService.GetProgenyUserAccessForUser(contact.ProgenyId, userEmail);

            if (userAccess == null && contact.ProgenyId != Constants.DefaultChildId) return NotFound();

            if (contact.AddressIdNumber.HasValue)
            {
                contact.Address = await locationService.GetAddressItem(contact.AddressIdNumber.Value);
            }

            contact.PictureLink = imageStore.UriFor(contact.PictureLink, BlobContainers.Contacts);

            return Ok(contact);

        }

        [HttpGet]
        [Route("[action]/{id:int}/{accessLevel:int}")]
        public async Task<IActionResult> ProgenyMobile(int id, int accessLevel = 5)
        {
            List<Contact> contactsList = await contactService.GetContactsList(id);
            contactsList = contactsList.Where(c => c.AccessLevel >= accessLevel).ToList();

            string userEmail = User.GetEmail() ?? Constants.DefaultUserEmail;
            UserAccess userAccess = await userAccessService.GetProgenyUserAccessForUser(id, userEmail);
            if ((userAccess == null && id != Constants.DefaultChildId) || contactsList.Count == 0) return Ok(new List<Contact>());

            foreach (Contact contact in contactsList)
            {
                if (contact.AddressIdNumber.HasValue)
                {
                    contact.Address = await locationService.GetAddressItem(contact.AddressIdNumber.Value);
                }

                contact.PictureLink = imageStore.UriFor(contact.PictureLink, BlobContainers.Contacts);
            }
            return Ok(contactsList);

        }

        [HttpGet]
        [Route("[action]/{contactId:int}")]
        public async Task<IActionResult> DownloadPicture(int contactId)
        {
            Contact contact = await contactService.GetContact(contactId);
            if (contact == null)
            {
                return NotFound();
            }

            string userEmail = User.GetEmail() ?? Constants.DefaultUserEmail;
            UserAccess userAccess = await userAccessService.GetProgenyUserAccessForUser(contact.ProgenyId, userEmail);
            if (userAccess == null || userAccess.AccessLevel <= 0 || !contact.PictureLink.StartsWith("http", System.StringComparison.CurrentCultureIgnoreCase)) return NotFound();

            await using (Stream stream = await GetStreamFromUrl(contact.PictureLink))
            {
                string fileFormat = "";
                if (contact.PictureLink.ToLower().EndsWith(".jpg"))
                {
                    fileFormat = ".jpg";
                }

                if (contact.PictureLink.ToLower().EndsWith(".png"))
                {
                    fileFormat = ".png";
                }

                if (contact.PictureLink.ToLower().EndsWith(".gif"))
                {
                    fileFormat = ".gif";
                }

                if (contact.PictureLink.ToLower().EndsWith(".jpeg"))
                {
                    fileFormat = ".jpg";
                }

                if (contact.PictureLink.ToLower().EndsWith(".bmp"))
                {
                    fileFormat = ".bmp";
                }

                if (contact.PictureLink.ToLower().EndsWith(".tif"))
                {
                    fileFormat = ".tif";
                }
                

                contact.PictureLink = await imageStore.SaveImage(stream, BlobContainers.Contacts, fileFormat);
            }

            contact = await contactService.UpdateContact(contact);

            return Ok(contact);

        }

        private static async Task<Stream> GetStreamFromUrl(string url)
        {
            using HttpClient client = new();
            using HttpResponseMessage response = await client.GetAsync(url);
            
            return await response.Content.ReadAsStreamAsync();
        }
    }
}
