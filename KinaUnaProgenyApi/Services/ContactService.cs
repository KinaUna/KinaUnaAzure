using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using KinaUna.Data;
using KinaUna.Data.Contexts;
using KinaUna.Data.Extensions;
using KinaUna.Data.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Newtonsoft.Json;

namespace KinaUnaProgenyApi.Services
{
    public class ContactService : IContactService
    {
        private readonly ProgenyDbContext _context;
        private readonly IImageStore _imageStore;
        private readonly IDistributedCache _cache;
        private readonly DistributedCacheEntryOptions _cacheOptions = new();
        private readonly DistributedCacheEntryOptions _cacheOptionsSliding = new();
        
        public ContactService(ProgenyDbContext context, IDistributedCache cache, IImageStore imageStore)
        {
            _context = context;
            _imageStore = imageStore;
            _cache = cache;
            _cacheOptions.SetAbsoluteExpiration(new TimeSpan(0, 5, 0)); // Expire after 5 minutes.
            _cacheOptionsSliding.SetSlidingExpiration(new TimeSpan(7, 0, 0, 0)); // Expire after a week.
        }

        /// <summary>
        /// Gets a Contact by ContactId from the cache.
        /// If the Contact isn't in the cache, it will be looked up in the database and added to the cache.
        /// </summary>
        /// <param name="id">The ContactId of the Contact to get.</param>
        /// <returns>Contact object. Null if a Contact entity with the given ContactId doesn't exist.</returns>
        public async Task<Contact> GetContact(int id)
        {
            Contact contact = await GetContactFromCache(id);
            if (contact == null || contact.ContactId == 0)
            {
                contact = await SetContactInCache(id);
            }

            return contact;
        }

        /// <summary>
        /// Adds a new Contact to the database and the cache.
        /// </summary>
        /// <param name="contact">The Contact object to add.</param>
        /// <returns>The updated Contact object.</returns>
        public async Task<Contact> AddContact(Contact contact)
        {
            Contact contactToAdd = new();
            contactToAdd.CopyPropertiesForAdd(contact);

            _context.ContactsDb.Add(contactToAdd);
            _ = await _context.SaveChangesAsync();
            _ = await SetContactInCache(contactToAdd.ContactId);

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
                contact = JsonConvert.DeserializeObject<Contact>(cachedContact);
            }

            return contact;
        }

        /// <summary>
        /// Gets a Contact by ContactId from the database and adds it to the cache.
        /// Also updates the ContactsList for the Progeny that the Contact belongs to in the cache.
        /// </summary>
        /// <param name="id">The ContactId of the Contact to get and set.</param>
        /// <returns>The Contact object. Null if a Contact with the given ContactId doesn't exist.</returns>
        public async Task<Contact> SetContactInCache(int id)
        {
            Contact contact = await _context.ContactsDb.AsNoTracking().SingleOrDefaultAsync(c => c.ContactId == id);
            if (contact == null) return null;
            
            await _cache.SetStringAsync(Constants.AppName + Constants.ApiVersion + "contact" + id, JsonConvert.SerializeObject(contact), _cacheOptionsSliding);

            _ = await SetContactsListInCache(contact.ProgenyId);

            return contact;
        }

        /// <summary>
        /// Updates a Contact in the database and the cache.
        /// </summary>
        /// <param name="contact">The Contact object with the updated properties.</param>
        /// <returns>The updated Contact object.</returns>
        public async Task<Contact> UpdateContact(Contact contact)
        {
            Contact contactToUpdate = await _context.ContactsDb.SingleOrDefaultAsync(c => c.ContactId == contact.ContactId);
            if (contactToUpdate == null) return null;
            
            string oldPictureLink = contactToUpdate.PictureLink;

            contactToUpdate.AccessLevel = contact.AccessLevel;
            contactToUpdate.Active = contact.Active;
            contactToUpdate.AddressIdNumber = contact.AddressIdNumber;
            contactToUpdate.Address = contact.Address;
            contactToUpdate.ProgenyId = contact.ProgenyId;
            contactToUpdate.AddressString = contact.AddressString;
            contactToUpdate.Author = contact.Author;
            contactToUpdate.Context = contact.Context;
            contactToUpdate.DateAdded = contact.DateAdded;
            contactToUpdate.DisplayName = contact.DisplayName;
            contactToUpdate.FirstName = contact.FirstName;
            contactToUpdate.MiddleName = contact.MiddleName;
            contactToUpdate.LastName = contact.LastName;
            contactToUpdate.PictureLink = contact.PictureLink;
            contactToUpdate.Email1 = contact.Email1;
            contactToUpdate.Email2 = contact.Email2;
            contactToUpdate.PhoneNumber = contact.PhoneNumber;
            contactToUpdate.MobileNumber = contact.MobileNumber;
            contactToUpdate.Notes = contact.Notes;
            contactToUpdate.Progeny = contact.Progeny;
            contactToUpdate.Tags = contact.Tags;
            contactToUpdate.Website = contact.Website;

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
            
            return contact;
        }

        /// <summary>
        /// Deletes a Contact from the database and the cache. 
        /// </summary>
        /// <param name="contact">The Contact to delete.</param>
        /// <returns>The deleted Contact.</returns>
        public async Task<Contact> DeleteContact(Contact contact)
        {
            Contact contactToDelete = await _context.ContactsDb.SingleOrDefaultAsync(c => c.ContactId == contact.ContactId);
            if (contactToDelete == null) return null;

            _context.ContactsDb.Remove(contactToDelete);
            _ = await _context.SaveChangesAsync();
            await RemoveContactFromCache(contact.ContactId, contact.ProgenyId);

            List<Contact> contactsWithThisPicture = await _context.ContactsDb.AsNoTracking().Where(c => c.PictureLink == contactToDelete.PictureLink).ToListAsync();
            if (contactsWithThisPicture.Count == 0)
            {
                await _imageStore.DeleteImage(contactToDelete.PictureLink, BlobContainers.Contacts);
            }
            return contact;
        }

        /// <summary>
        /// Deletes a Contact from the cache and updates the ContactsList for the Progeny that the Contact belongs to.
        /// </summary>
        /// <param name="id">The ContactId of the Contact to delete.</param>
        /// <param name="progenyId">The ProgenyId of the Progeny the Contact item belongs to.</param>
        /// <returns></returns>
        public async Task RemoveContactFromCache(int id, int progenyId)
        {
            await _cache.RemoveAsync(Constants.AppName + Constants.ApiVersion + "contact" + id);

            _ = await SetContactsListInCache(progenyId);
        }

        /// <summary>
        /// Gets a list of all Contacts for a Progeny from the cache.
        /// If the list is empty, it will be looked up in the database and added to the cache.
        /// </summary>
        /// <param name="progenyId">The ProgenyId of the Progeny to get all Contacts for.</param>
        /// <param name="accessLevel">The access level required to view the Contact.</param>
        /// <returns>List of Contacts.</returns>
        public async Task<List<Contact>> GetContactsList(int progenyId, int accessLevel)
        {
            List<Contact> contactsList = await GetContactsListFromCache(progenyId);
            if (contactsList.Count == 0)
            {
                contactsList = await SetContactsListInCache(progenyId);
            }

            contactsList = contactsList.Where(p => p.AccessLevel >= accessLevel).ToList();

            return contactsList;
        }

        /// <summary>
        /// Gets a list of all Contacts for a Progeny from the cache.
        /// </summary>
        /// <param name="progenyId">The ProgenyId of the Progeny to get all Contacts for.</param>
        /// <returns>List of Contacts.</returns>
        private async Task<List<Contact>> GetContactsListFromCache(int progenyId)
        {
            List<Contact> contactsList = [];
            string cachedContactsList = await _cache.GetStringAsync(Constants.AppName + Constants.ApiVersion + "contactslist" + progenyId);
            if (!string.IsNullOrEmpty(cachedContactsList))
            {
                contactsList = JsonConvert.DeserializeObject<List<Contact>>(cachedContactsList);
            }

            return contactsList;
        }

        private async Task<List<Contact>> SetContactsListInCache(int progenyId)
        {
            List<Contact> contactsList = await _context.ContactsDb.AsNoTracking().Where(c => c.ProgenyId == progenyId).ToListAsync();
            await _cache.SetStringAsync(Constants.AppName + Constants.ApiVersion + "contactslist" + progenyId, JsonConvert.SerializeObject(contactsList), _cacheOptionsSliding);

            return contactsList;
        }

        public async Task<List<Contact>> GetContactsWithTag(int progenyId, string tag, int accessLevel)
        {
            List<Contact> allItems = await GetContactsList(progenyId, accessLevel);
            if (!string.IsNullOrEmpty(tag))
            {
                allItems = [.. allItems.Where(c => c.Tags != null && c.Tags.Contains(tag, StringComparison.CurrentCultureIgnoreCase))];
            }

            return allItems;
        }

        public async Task<List<Contact>> GetContactsWithContext(int progenyId, string context, int accessLevel)
        {
            List<Contact> allItems = await GetContactsList(progenyId, accessLevel);
            if (!string.IsNullOrEmpty(context))
            {
                allItems = [.. allItems.Where(c => c.Context != null && c.Context.Contains(context, StringComparison.CurrentCultureIgnoreCase))];
            }
            return allItems;
        }
    }
}
