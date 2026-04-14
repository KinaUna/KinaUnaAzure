using KinaUna.Data;
using KinaUna.Data.Extensions;
using KinaUna.Data.Models;
using KinaUna.Data.Models.AccessManagement;
using KinaUnaProgenyApi.Services;
using KinaUnaProgenyApi.Services.AccessManagementService;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using KinaUna.Data.Models.Family;
using KinaUnaProgenyApi.Services.FamiliesServices;

namespace KinaUnaProgenyApi.Controllers
{
    /// <summary>
    /// API endpoints for Contacts.
    /// </summary>
    /// <param name="imageStore"></param>
    /// <param name="userInfoService"></param>
    /// <param name="contactService"></param>
    /// <param name="locationService"></param>
    /// <param name="timelineService"></param>
    /// <param name="progenyService"></param>
    /// <param name="webNotificationsService"></param>
    [Authorize(Policy = "UserOrClient")]
    [Produces("application/json")]
    [Route("api/[controller]")]
    [ApiController]
    public class ContactsController(
        IImageStore imageStore,
        IUserInfoService userInfoService,
        IContactService contactService,
        ILocationService locationService,
        ITimelineService timelineService,
        IProgenyService progenyService,
        IFamiliesService familiesService,
        IWebNotificationsService webNotificationsService,
        IAccessManagementService accessManagementService,
        HttpClient httpClient)
        : ControllerBase
    {
        /// <summary>
        /// Retrieve all contacts for a Progeny with the given id and access level.
        /// </summary>
        /// <param name="id">The ProgenyId of the Progeny to get Contacts for.</param>
        /// <returns>List of all Contacts the user has access to for this Progeny.</returns>
        // GET api/contacts/progeny/[id]
        [HttpGet]
        [Route("[action]/{id:int}")]
        public async Task<IActionResult> Progeny(int id)
        {
            UserInfo currentUserInfo = await userInfoService.GetUserInfoByUserId(User.GetUserId());

            List<Contact> contactsList = await contactService.GetContactsList(id, 0, currentUserInfo);
            
            if (contactsList.Count != 0)
            {
                return Ok(contactsList);
            }

            return NotFound();
        }

        /// <summary>
        /// Retrieve all contacts for a Progeny with the given id and access level.
        /// </summary>
        /// <param name="id">The ProgenyId of the Progeny to get Contacts for.</param>
        /// <returns>List of all Contacts the user has access to for this Progeny.</returns>
        // GET api/contacts/progeny/[id]
        [HttpGet]
        [Route("[action]/{id:int}")]
        public async Task<IActionResult> Family(int id)
        {
            UserInfo currentUserInfo = await userInfoService.GetUserInfoByUserId(User.GetUserId());

            List<Contact> contactsList = await contactService.GetContactsList(0, id, currentUserInfo);

            if (contactsList.Count != 0)
            {
                return Ok(contactsList);
            }

            return NotFound();
        }

        /// <summary>
        /// Retrieves a Contact with the given id.
        /// </summary>
        /// <param name="id">The ContactId of the Contact entity to get.</param>
        /// <returns>The Contact object, if the user has access to it, else NotFoundResult.</returns>
        // GET api/contacts/5
        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetContactItem(int id)
        {
            UserInfo currentUserInfo = await userInfoService.GetUserInfoByUserId(User.GetUserId());
            Contact contact = await contactService.GetContact(id, currentUserInfo);
            
            return Ok(contact);
        }

        /// <summary>
        /// Adds a new Contact entity to the database.
        /// Then adds the corresponding TimeLineItem and sends notifications to users who have access to the Contact item.
        /// </summary>
        /// <param name="value">The Contact object to add.</param>
        /// <returns>The added Contact object.</returns>
        // POST api/contact
        [HttpPost]
        public async Task<IActionResult> Post([FromBody] Contact value)
        {
            // A contact can either be linked to a Progeny or a Family, not both.
            if (value.ProgenyId > 0 && value.FamilyId > 0)
            {
                return BadRequest("A contact cannot be linked to both a Progeny and a Family.");
            }
            if (value.ProgenyId == 0 && value.FamilyId == 0)
            {
                return BadRequest("A contact must be linked to either a Progeny or a Family.");
            }

            UserInfo currentUserInfo = await userInfoService.GetUserInfoByUserId(User.GetUserId());
            bool hasAccess = false;
            if (value.ProgenyId > 0)
            {
                if (await accessManagementService.HasProgenyPermission(value.ProgenyId, currentUserInfo, PermissionLevel.Add))
                {
                    hasAccess = true;
                }
            }

            if (value.FamilyId > 0)
            {
                if (await accessManagementService.HasFamilyPermission(value.FamilyId, currentUserInfo, PermissionLevel.Add))
                {
                    hasAccess = true;
                }
            }

            if (!hasAccess)
            {
                return Unauthorized();
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
            value.CreatedBy = User.GetUserId();

            Contact contactItem = await contactService.AddContact(value, currentUserInfo);
            if (contactItem == null)
            {
                return Unauthorized();
            }

            TimeLineItem timeLineItem = new();
            timeLineItem.CopyContactPropertiesForAdd(contactItem);

            await timelineService.AddTimeLineItem(timeLineItem, currentUserInfo);

            string nameString = "";
            if (contactItem.ProgenyId > 0)
            {
                Progeny progeny = await progenyService.GetProgeny(contactItem.ProgenyId, currentUserInfo);
                nameString = progeny.NickName;
            }
            if (contactItem.FamilyId > 0)
            {
                Family family = await familiesService.GetFamilyById(contactItem.FamilyId, currentUserInfo);
                nameString = family.Name;
            }
            string notificationTitle = "Contact added for " + nameString;
            await webNotificationsService.SendContactNotification(contactItem, currentUserInfo, notificationTitle);

            contactItem = await contactService.GetContact(contactItem.ContactId, currentUserInfo);

            return Ok(contactItem);
        }

        /// <summary>
        /// Updates an existing Contact entity in the database.
        /// Then updates the corresponding TimelineItem.
        /// </summary>
        /// <param name="id">The ContactId of the Contact entity.</param>
        /// <param name="value">Contact object with the properties to update.</param>
        /// <returns>The updated Contact object.</returns>
        // PUT api/contacts/5
        [HttpPut("{id:int}")]
        public async Task<IActionResult> Put(int id, [FromBody] Contact value)
        {
            if (id != value.ContactId)
            {
                return BadRequest();
            }
            // A contact can either be linked to a Progeny or a Family, not both.
            if (value.ProgenyId > 0 && value.FamilyId > 0)
            {
                return BadRequest("A contact cannot be linked to both a Progeny and a Family.");
            }
            if (value.ProgenyId == 0 && value.FamilyId == 0)
            {
                return BadRequest("A contact must be linked to either a Progeny or a Family.");
            }

            UserInfo currentUserInfo = await userInfoService.GetUserInfoByUserId(User.GetUserId());
            if (!await accessManagementService.HasItemPermission(KinaUnaTypes.TimeLineType.Contact, value.ContactId, currentUserInfo, PermissionLevel.Edit))
            {
                return Unauthorized();
            }

            Contact contactItem = await contactService.GetContact(id, currentUserInfo);
            if (contactItem == null)
            {
                return NotFound();
            }
            
            if (value.PictureLink == Constants.DefaultPictureLink)
            {
                value.PictureLink = Constants.ProfilePictureUrl;
            }

            value.ModifiedBy = User.GetUserId();
            
            // Todo: Refactor move Address handling to ContactService.
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

            contactItem.CopyPropertiesForUpdate(value);

            contactItem = await contactService.UpdateContact(contactItem, currentUserInfo);
            if (contactItem == null)
            {
                return Unauthorized();
            }

            contactItem.Author = User.GetUserId();

            TimeLineItem timeLineItem = await timelineService.GetTimeLineItemByItemId(contactItem.ContactId.ToString(), (int)KinaUnaTypes.TimeLineType.Contact, currentUserInfo);
            if (timeLineItem == null || !timeLineItem.CopyContactItemPropertiesForUpdate(contactItem)) return Ok(contactItem);

            _ = await timelineService.UpdateTimeLineItem(timeLineItem, currentUserInfo);

            contactItem = await contactService.GetContact(contactItem.ContactId, currentUserInfo);

            return Ok(contactItem);
        }

        /// <summary>
        /// Deletes a Contact entity from the database.
        /// Then deletes the corresponding TimeLineItem.
        /// Then sends notifications to users with admin access to the Progeny.
        /// </summary>
        /// <param name="id">The ContactId of the Contact entity to delete.</param>
        /// <returns>No content if deleted successfully, UnauthorizedResult if the user doesn't have the access rights, NotFoundResult if the item doesn't exist.</returns>
        // DELETE api/contacts/5
        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(int id)
        {
            UserInfo currentUserInfo = await userInfoService.GetUserInfoByUserId(User.GetUserId());
            Contact contactItem = await contactService.GetContact(id, currentUserInfo);
            if (contactItem == null) return NotFound();
            
            if (!await accessManagementService.HasItemPermission(KinaUnaTypes.TimeLineType.Contact, contactItem.ContactId, currentUserInfo, PermissionLevel.Admin))
            {
                return Unauthorized();
            }
            
            contactItem.ModifiedBy = User.GetUserId();

            Contact deletedContact = await contactService.DeleteContact(contactItem, currentUserInfo);
            if (deletedContact == null)
            {
                return Unauthorized();
            } 
            
            TimeLineItem timeLineItem = await timelineService.GetTimeLineItemByItemId(contactItem.ContactId.ToString(), (int)KinaUnaTypes.TimeLineType.Contact, currentUserInfo);
            if (timeLineItem != null)
            {
                _ = await timelineService.DeleteTimeLineItem(timeLineItem, currentUserInfo);
            }

            if (contactItem.AddressIdNumber != null)
            {
                Address address = await locationService.GetAddressItem(contactItem.AddressIdNumber.Value);
                if (address != null)
                {
                    await locationService.RemoveAddressItem(address.AddressId);
                }
            }
            
            contactItem.Author = User.GetUserId();
            string nameString = "";
            if (contactItem.ProgenyId > 0)
            {
                Progeny progeny = await progenyService.GetProgeny(contactItem.ProgenyId, currentUserInfo);
                nameString = progeny.NickName;
            }
            if (contactItem.FamilyId > 0)
            {
                Family family = await familiesService.GetFamilyById(contactItem.FamilyId, currentUserInfo);
                nameString = family.Name;
            }
            string notificationTitle = "Contact deleted for " + nameString;
            
            if (timeLineItem == null) return NoContent();

            await webNotificationsService.SendContactNotification(contactItem, currentUserInfo, notificationTitle);

            return NoContent();

        }
        
        /// <summary>
        /// Download a Contact's profile picture from a URL in the Contact's PictureLink and save it to the image store.
        /// </summary>
        /// <param name="contactId">The ContactId of the Contact.</param>
        /// <returns>The Contact entity with the updated PictureLink.</returns>
        [HttpGet]
        [Route("[action]/{contactId:int}")]
        public async Task<IActionResult> DownloadPicture(int contactId)
        {
            UserInfo currentUserInfo = await userInfoService.GetUserInfoByUserId(User.GetUserId());
            Contact contact = await contactService.GetContact(contactId, currentUserInfo);
            if (contact == null || contact.ContactId == 0 || contact.ItemPerMission.PermissionLevel < PermissionLevel.Edit)
            {
                return NotFound();
            }
            
            if (!contact.PictureLink.StartsWith("http", System.StringComparison.CurrentCultureIgnoreCase)) return NotFound();

            await using (Stream stream = await GetStreamFromUrl(contact.PictureLink))
            {
                contact.PictureLink = await imageStore.SaveImage(stream, BlobContainers.Contacts, contact.GetPictureFileContentType());
            }

            contact = await contactService.UpdateContact(contact, currentUserInfo);

            return Ok(contact);

        }

        //Todo: Refactor, this method is also in the FriendsController.
        /// <summary>
        /// Helper method to get a Stream from a URL.
        /// </summary>
        /// <param name="url">The url to get the stream for.</param>
        /// <returns>The stream object for the URL.</returns>
        private async Task<Stream> GetStreamFromUrl(string url)
        {
            using HttpResponseMessage response = await httpClient.GetAsync(url);
            
            return await response.Content.ReadAsStreamAsync();
        }
    }
}
