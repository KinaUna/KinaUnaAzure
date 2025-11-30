using KinaUna.Data;
using KinaUna.Data.Contexts;
using KinaUna.Data.Extensions;
using KinaUna.Data.Models;
using KinaUna.Data.Models.AccessManagement;
using KinaUna.Data.Models.CacheManagement;
using KinaUnaProgenyApi.Services.AccessManagementService;
using KinaUnaProgenyApi.Services.CacheServices;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace KinaUnaProgenyApi.Services
{
    public class ContactService : IContactService
    {
        private readonly ProgenyDbContext _context;
        private readonly IAccessManagementService _accessManagementService;
        private readonly IImageStore _imageStore;
        private readonly IDistributedCache _cache;
        private readonly IKinaUnaCacheService _kinaUnaCacheService;
        private readonly DistributedCacheEntryOptions _cacheOptions = new();
        private readonly DistributedCacheEntryOptions _cacheOptionsSliding = new();

        public ContactService(ProgenyDbContext context, IDistributedCache cache, IImageStore imageStore, IAccessManagementService accessManagementService, IKinaUnaCacheService kinaUnaCacheService)
        {
            _context = context;
            _accessManagementService = accessManagementService;
            _imageStore = imageStore;
            _cache = cache;
            _kinaUnaCacheService = kinaUnaCacheService;
            _cacheOptions.SetAbsoluteExpiration(new TimeSpan(0, 5, 0)); // Expire after 5 minutes.
            _cacheOptionsSliding.SetSlidingExpiration(new TimeSpan(7, 0, 0, 0)); // Expire after a week.
        }

        /// <summary>
        /// Gets a Contact by ContactId from the cache.
        /// If the Contact isn't in the cache, it will be looked up in the database and added to the cache.
        /// </summary>
        /// <param name="id">The ContactId of the Contact to get.</param>
        /// <param name="currentUserInfo">The UserInfo object for the current user, to check permissions.</param>
        /// <returns>Contact object. Null if a Contact entity with the given ContactId doesn't exist.</returns>
        public async Task<Contact> GetContact(int id, UserInfo currentUserInfo)
        {
            if (!await _accessManagementService.HasItemPermission(KinaUnaTypes.TimeLineType.Contact, id, currentUserInfo, PermissionLevel.View))
            {
                return null;
            }
            Contact contact = await GetContactFromCache(id);
            if (contact == null || contact.ContactId == 0)
            {
                contact = await SetContactInCache(id);
            }

            if (contact == null)
            {
                return null;
            }

            contact.ItemPerMission = await _accessManagementService.GetItemPermissionForUser(KinaUnaTypes.TimeLineType.Contact, id, contact.ProgenyId, contact.FamilyId, currentUserInfo);

            return contact;
        }

        /// <summary>
        /// Adds a new Contact to the database and the cache.
        /// </summary>
        /// <param name="contact">The Contact object to add.</param>
        /// <param name="currentUserInfo">The UserInfo object for the current user, to check permissions.</param>
        /// <returns>The updated Contact object.</returns>
        public async Task<Contact> AddContact(Contact contact, UserInfo currentUserInfo)
        {
            bool hasAccess = false;
            if (contact.ProgenyId > 0)
            {
                if (await _accessManagementService.HasProgenyPermission(contact.ProgenyId, currentUserInfo, PermissionLevel.Add))
                {
                    hasAccess = true;
                }
            }

            if (contact.FamilyId > 0)
            {
                if (await _accessManagementService.HasFamilyPermission(contact.FamilyId, currentUserInfo, PermissionLevel.Add))
                {
                    hasAccess = true;
                }
            }

            if (!hasAccess)
            {
                return null;
            }

            Contact contactToAdd = new();
            contactToAdd.CopyPropertiesForAdd(contact);

            _context.ContactsDb.Add(contactToAdd);
            _ = await _context.SaveChangesAsync();
            _ = await SetContactInCache(contactToAdd.ContactId);

            await _accessManagementService.AddItemPermissions(KinaUnaTypes.TimeLineType.Contact, contactToAdd.ContactId, contactToAdd.ProgenyId, contactToAdd.FamilyId, contactToAdd.ItemPermissionsDtoList, currentUserInfo);

            _kinaUnaCacheService.SetProgenyOrFamilyTimelineUpdatedCache(contactToAdd.ProgenyId, contactToAdd.FamilyId, KinaUnaTypes.TimeLineType.Contact);
            return contactToAdd;
        }

        /// <summary>
        /// Gets a Contact by ContactId from the cache.
        /// </summary>
        /// <param name="id"></param>
        /// <returns>The Contact object if found in the cache. A new Contact object if not found, check for ContactId == 0 to determine if it's found.</returns>
        private async Task<Contact> GetContactFromCache(int id)
        {
            Contact contact = new();
            string cachedContact = await _cache.GetStringAsync(Constants.AppName + Constants.ApiVersion + "contact" + id);
            if (!string.IsNullOrEmpty(cachedContact))
            {
                contact = JsonSerializer.Deserialize<Contact>(cachedContact, JsonSerializerOptions.Web);
            }

            return contact;
        }

        /// <summary>
        /// Gets a Contact by ContactId from the database and adds it to the cache.
        /// Also updates the ContactsList for the Progeny that the Contact belongs to in the cache.
        /// </summary>
        /// <param name="id">The ContactId of the Contact to get and set.</param>
        /// <returns>The Contact object. Null if a Contact with the given ContactId doesn't exist.</returns>
        private async Task<Contact> SetContactInCache(int id)
        {
            Contact contact = await _context.ContactsDb.AsNoTracking().SingleOrDefaultAsync(c => c.ContactId == id);
            if (contact == null) return null;
            
            await _cache.SetStringAsync(Constants.AppName + Constants.ApiVersion + "contact" + id, JsonSerializer.Serialize(contact, JsonSerializerOptions.Web), _cacheOptionsSliding);

            _ = await SetContactsListInCache(contact.ProgenyId, contact.FamilyId);

            return contact;
        }

        /// <summary>
        /// Updates a Contact in the database and the cache.
        /// </summary>
        /// <param name="contact">The Contact object with the updated properties.</param>
        /// <param name="currentUserInfo"></param>
        /// <returns>The updated Contact object.</returns>
        public async Task<Contact> UpdateContact(Contact contact, UserInfo currentUserInfo)
        {
            if (!await _accessManagementService.HasItemPermission(KinaUnaTypes.TimeLineType.Contact, contact.ContactId, currentUserInfo, PermissionLevel.Edit))
            {
                return null;
            }

            Contact contactToUpdate = await _context.ContactsDb.SingleOrDefaultAsync(c => c.ContactId == contact.ContactId);
            if (contactToUpdate == null || contactToUpdate.ProgenyId != contact.ProgenyId || contactToUpdate.FamilyId != contact.FamilyId) return null;
            
            string oldPictureLink = contactToUpdate.PictureLink;

            contactToUpdate.CopyPropertiesForUpdate(contact);

            _context.ContactsDb.Update(contactToUpdate);
            _ = await _context.SaveChangesAsync();

            if (oldPictureLink != contact.PictureLink)
            {
                List<Contact> contactsWithThisPicture = await _context.ContactsDb.AsNoTracking().Where(c => c.PictureLink == oldPictureLink).ToListAsync();
                if (contactsWithThisPicture.Count == 0)
                {
                    await _imageStore.DeleteImage(oldPictureLink, BlobContainers.Contacts);
                }
            }

            _ = await SetContactInCache(contactToUpdate.ContactId);

            await _accessManagementService.UpdateItemPermissions(KinaUnaTypes.TimeLineType.Contact, contactToUpdate.ContactId, contactToUpdate.ProgenyId, contactToUpdate.FamilyId, contactToUpdate.ItemPermissionsDtoList,
                currentUserInfo);

            _kinaUnaCacheService.SetProgenyOrFamilyTimelineUpdatedCache(contactToUpdate.ProgenyId, contactToUpdate.FamilyId, KinaUnaTypes.TimeLineType.Contact);

            return contact;
        }

        /// <summary>
        /// Deletes a Contact from the database and the cache. 
        /// </summary>
        /// <param name="contact">The Contact to delete.</param>
        /// <param name="currentUserInfo"></param>
        /// <returns>The deleted Contact.</returns>
        public async Task<Contact> DeleteContact(Contact contact, UserInfo currentUserInfo)
        {
            if (!await _accessManagementService.HasItemPermission(KinaUnaTypes.TimeLineType.Contact, contact.ContactId, currentUserInfo, PermissionLevel.Admin))
            {
                return null;
            }

            Contact contactToDelete = await _context.ContactsDb.SingleOrDefaultAsync(c => c.ContactId == contact.ContactId);
            if (contactToDelete == null) return null;

            _context.ContactsDb.Remove(contactToDelete);
            _ = await _context.SaveChangesAsync();
            await RemoveContactFromCache(contact.ContactId, contact.ProgenyId, contact.FamilyId);

            List<Contact> contactsWithThisPicture = await _context.ContactsDb.AsNoTracking().Where(c => c.PictureLink == contactToDelete.PictureLink).ToListAsync();
            if (contactsWithThisPicture.Count == 0)
            {
                await _imageStore.DeleteImage(contactToDelete.PictureLink, BlobContainers.Contacts);
            }

            // Revoke all permissions associated with this contact.
            List<TimelineItemPermission> timelineItemPermissionsList = await _accessManagementService.GetTimelineItemPermissionsList(KinaUnaTypes.TimeLineType.Contact, contactToDelete.ContactId, currentUserInfo);
            foreach (TimelineItemPermission permission in timelineItemPermissionsList)
            {
                await _accessManagementService.RevokeItemPermission(permission, currentUserInfo);
            }

            _kinaUnaCacheService.SetProgenyOrFamilyTimelineUpdatedCache(contactToDelete.ProgenyId, contactToDelete.FamilyId, KinaUnaTypes.TimeLineType.Contact);

            return contact;
        }

        /// <summary>
        /// Deletes a Contact from the cache and updates the ContactsList for the Progeny that the Contact belongs to.
        /// </summary>
        /// <param name="id">The ContactId of the Contact to delete.</param>
        /// <param name="progenyId">The ProgenyId of the Progeny the Contact item belongs to.</param>
        /// <param name="familyId"></param>
        /// <returns></returns>
        private async Task RemoveContactFromCache(int id, int progenyId, int familyId)
        {
            await _cache.RemoveAsync(Constants.AppName + Constants.ApiVersion + "contact" + id);

            _ = await SetContactsListInCache(progenyId, familyId);
        }

        /// <summary>
        /// Gets a list of all Contacts for a Progeny from the cache.
        /// If the list is empty, it will be looked up in the database and added to the cache.
        /// </summary>
        /// <param name="progenyId">The ProgenyId of the Progeny to get all Contacts for.</param>
        /// <param name="familyId"></param>
        /// <param name="currentUserInfo">The UserInfo object for the current user, to check permissions.</param>
        /// <returns>List of Contacts.</returns>
        public async Task<List<Contact>> GetContactsList(int progenyId, int familyId, UserInfo currentUserInfo)
        {
            ContactsListCacheEntry cacheEntry = _kinaUnaCacheService.GetContactsListCache(currentUserInfo.UserId, progenyId, familyId);
            TimelineUpdatedCacheEntry timelineUpdatedCacheEntry = _kinaUnaCacheService.GetProgenyOrFamilyTimelineUpdatedCache(progenyId, familyId, KinaUnaTypes.TimeLineType.Contact);
            if (cacheEntry != null && timelineUpdatedCacheEntry != null)
            {
                if (cacheEntry.UpdateTime >= timelineUpdatedCacheEntry.UpdateTime)
                {
                    return cacheEntry.ContactsList.ToList();
                }
            }

            Contact[] contactsList = await GetContactsListFromCache(progenyId, familyId);
            if (contactsList.Length == 0)
            {
                contactsList = await SetContactsListInCache(progenyId, familyId);
            }

            List<Contact> accessibleContacts = [];
            foreach (Contact contact in contactsList)
            {
                if (await _accessManagementService.HasItemPermission(KinaUnaTypes.TimeLineType.Contact, contact.ContactId, currentUserInfo, PermissionLevel.View))
                {
                    //contact.ItemPerMission = await _accessManagementService.GetItemPermissionForUser(KinaUnaTypes.TimeLineType.Contact, contact.ContactId, contact.ProgenyId, contact.FamilyId, currentUserInfo);
                    accessibleContacts.Add(contact);
                }
            }

            _kinaUnaCacheService.SetContactsListCache(currentUserInfo.UserId, progenyId, familyId, accessibleContacts.ToArray());

            return accessibleContacts;
        }

        /// <summary>
        /// Gets a list of all Contacts for a Progeny from the cache.
        /// </summary>
        /// <param name="progenyId">The ProgenyId of the Progeny to get all Contacts for.</param>
        /// <param name="familyId"></param>
        /// <returns>List of Contacts.</returns>
        private async Task<Contact[]> GetContactsListFromCache(int progenyId, int familyId)
        {
            Contact[] contactsList = [];
            string cachedContactsList = await _cache.GetStringAsync(Constants.AppName + Constants.ApiVersion + "contactslist" + progenyId + "_family_" + familyId);
            if (!string.IsNullOrEmpty(cachedContactsList))
            {
                contactsList = JsonSerializer.Deserialize<Contact[]>(cachedContactsList, JsonSerializerOptions.Web);
            }

            return contactsList;
        }

        /// <summary>
        /// Retrieves the list of contacts for the specified progeny and stores it in the cache.
        /// </summary>
        /// <remarks>The contacts are retrieved from the database and cached using a sliding expiration
        /// policy.  The cache key is constructed using the application name, API version, and the progeny ID.</remarks>
        /// <param name="progenyId">The unique identifier of the progeny whose contacts are to be retrieved and cached.</param>
        /// <param name="familyId"></param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the list of contacts associated
        /// with the specified progeny.</returns>
        private async Task<Contact[]> SetContactsListInCache(int progenyId, int familyId)
        {
            Contact[] contactsList = await _context.ContactsDb.AsNoTracking().Where(c => c.ProgenyId == progenyId && c.FamilyId == familyId).ToArrayAsync();
            await _cache.SetStringAsync(Constants.AppName + Constants.ApiVersion + "contactslist" + progenyId + "_family_" + familyId, JsonSerializer.Serialize(contactsList, JsonSerializerOptions.Web), _cacheOptionsSliding);

            return contactsList;
        }

        /// <summary>
        /// Gets a list of all Contacts for a Progeny with the given tag.
        /// </summary>
        /// <param name="progenyId">The ProgenyId of the Progeny to get all Contacts for.</param>
        /// <param name="familyId"></param>
        /// <param name="tag">The tag to filter contacts by.</param>
        /// <param name="currentUserInfo">The UserInfo object for the current user, to check permissions.</param>
        /// <returns></returns>
        public async Task<List<Contact>> GetContactsWithTag(int progenyId, int familyId, string tag, UserInfo currentUserInfo)
        {
            List<Contact> allItems = await GetContactsList(progenyId, familyId, currentUserInfo);
            if (!string.IsNullOrEmpty(tag))
            {
                allItems = [.. allItems.Where(c => c.Tags != null && c.Tags.Contains(tag, StringComparison.CurrentCultureIgnoreCase))];
            }

            return allItems;
        }

        /// <summary>
        /// Gets a list of all Contacts for a Progeny with the given context.
        /// </summary>
        /// <param name="progenyId">The ProgenyId of the Progeny to get all Contacts for.</param>
        /// <param name="familyId"></param>
        /// <param name="context">The context to filter contacts by.</param>
        /// <param name="currentUserInfo">The UserInfo object for the current user, to check permissions.</param>
        /// <returns></returns>
        public async Task<List<Contact>> GetContactsWithContext(int progenyId, int familyId, string context, UserInfo currentUserInfo)
        {
            List<Contact> allItems = await GetContactsList(progenyId, familyId, currentUserInfo);
            if (!string.IsNullOrEmpty(context))
            {
                allItems = [.. allItems.Where(c => c.Context != null && c.Context.Contains(context, StringComparison.CurrentCultureIgnoreCase))];
            }
            return allItems;
        }
    }
}
