﻿using KinaUnaProgenyApi.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using KinaUna.Data;
using KinaUna.Data.Extensions;
using KinaUna.Data.Models;
using KinaUnaProgenyApi.Services.UserAccessService;
using KinaUna.Data.Models.DTOs;

namespace KinaUnaProgenyApi.Controllers
{
    /// <summary>
    /// API endpoints for Contacts.
    /// </summary>
    /// <param name="imageStore"></param>
    /// <param name="azureNotifications"></param>
    /// <param name="userInfoService"></param>
    /// <param name="userAccessService"></param>
    /// <param name="contactService"></param>
    /// <param name="locationService"></param>
    /// <param name="timelineService"></param>
    /// <param name="progenyService"></param>
    /// <param name="webNotificationsService"></param>
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
            string userEmail = User.GetEmail() ?? Constants.DefaultUserEmail;
            CustomResult<int> accessLevelResult = await userAccessService.GetValidatedAccessLevel(id, userEmail, null);
            if (!accessLevelResult.IsSuccess)
            {
                return accessLevelResult.ToActionResult();
            }

            List<Contact> contactsList = await contactService.GetContactsList(id, accessLevelResult.Value);
            
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
            Contact contact = await contactService.GetContact(id);

            string userEmail = User.GetEmail() ?? Constants.DefaultUserEmail;
            CustomResult<int> accessLevelResult = await userAccessService.GetValidatedAccessLevel(contact.ProgenyId, userEmail, contact.AccessLevel);
            if (!accessLevelResult.IsSuccess)
            {
                return accessLevelResult.ToActionResult();
            }

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

            contactItem = await contactService.UpdateContact(contactItem);

            contactItem.Author = User.GetUserId();

            TimeLineItem timeLineItem = await timelineService.GetTimeLineItemByItemId(contactItem.ContactId.ToString(), (int)KinaUnaTypes.TimeLineType.Contact);
            if (timeLineItem == null || !timeLineItem.CopyContactItemPropertiesForUpdate(contactItem)) return Ok(contactItem);

            _ = await timelineService.UpdateTimeLineItem(timeLineItem);
            
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
        
        /// <summary>
        /// Download a Contact's profile picture from a URL in the Contact's PictureLink and save it to the image store.
        /// </summary>
        /// <param name="contactId">The ContactId of the Contact.</param>
        /// <returns>The Contact entity with the updated PictureLink.</returns>
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
                contact.PictureLink = await imageStore.SaveImage(stream, BlobContainers.Contacts, contact.GetPictureFileContentType());
            }

            contact = await contactService.UpdateContact(contact);

            return Ok(contact);

        }

        //Todo: Refactor, this method is also in the FriendsController.
        /// <summary>
        /// Helper method to get a Stream from a URL.
        /// </summary>
        /// <param name="url">The url to get the stream for.</param>
        /// <returns>The stream object for the URL.</returns>
        private static async Task<Stream> GetStreamFromUrl(string url)
        {
            using HttpClient client = new();
            using HttpResponseMessage response = await client.GetAsync(url);
            
            return await response.Content.ReadAsStreamAsync();
        }
    }
}
