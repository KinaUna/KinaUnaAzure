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
    public class ContactService: IContactService
    {
        private readonly ProgenyDbContext _context;
        private readonly IDistributedCache _cache;
        private readonly DistributedCacheEntryOptions _cacheOptions = new DistributedCacheEntryOptions();
        private readonly DistributedCacheEntryOptions _cacheOptionsSliding = new DistributedCacheEntryOptions();

        public ContactService(ProgenyDbContext context, IDistributedCache cache)
        {
            _context = context;
            _cache = cache;
            _cacheOptions.SetAbsoluteExpiration(new System.TimeSpan(0, 5, 0)); // Expire after 5 minutes.
            _cacheOptionsSliding.SetSlidingExpiration(new System.TimeSpan(7, 0, 0, 0)); // Expire after a week.
        }

        public async Task<Contact> GetContact(int id)
        {
            Contact contact = await GetContactFromCache(id);
            if (contact == null || contact.ContactId == 0)
            {
                contact = await SetContactInCache(id);
            }
            
            return contact;
        }

        public async Task<Contact> AddContact(Contact contact)
        {
            Contact contactToAdd = new Contact();
            contactToAdd.CopyPropertiesForAdd(contact);

            _context.ContactsDb.Add(contactToAdd);
            _ = await _context.SaveChangesAsync();
            _ = await SetContactInCache(contactToAdd.ContactId);

            return contactToAdd;
        }

        private async Task<Contact> GetContactFromCache(int id)
        {
            Contact contact = new Contact();
            string cachedContact = await _cache.GetStringAsync(Constants.AppName + Constants.ApiVersion + "contact" + id);
            if (!string.IsNullOrEmpty(cachedContact))
            {
                contact = JsonConvert.DeserializeObject<Contact>(cachedContact);
            }

            return contact;
        }

        public async Task<Contact> SetContactInCache(int id)
        {
            Contact contact = await _context.ContactsDb.AsNoTracking().SingleOrDefaultAsync(c => c.ContactId == id);
            if (contact != null)
            {
                await _cache.SetStringAsync(Constants.AppName + Constants.ApiVersion + "contact" + id, JsonConvert.SerializeObject(contact), _cacheOptionsSliding);

                _ = await SetContactsListInCache(contact.ProgenyId);
            }
            
            return contact;
        }

        public async Task<Contact> UpdateContact(Contact contact)
        {
            Contact contactToUpdate = await _context.ContactsDb.SingleOrDefaultAsync(c => c.ContactId == contact.ContactId);
            if (contactToUpdate != null)
            {
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

                _ = await SetContactInCache(contactToUpdate.ContactId);
            }
            

            return contact;
        }

        public async Task<Contact> DeleteContact(Contact contact)
        {
            Contact contactToDelete = await _context.ContactsDb.SingleOrDefaultAsync(c => c.ContactId == contact.ContactId);
            if (contactToDelete != null)
            {
                _context.ContactsDb.Remove(contactToDelete);
                _ = await _context.SaveChangesAsync();
                await RemoveContactFromCache(contact.ContactId, contact.ProgenyId);
            }
            
            return contact;
        }
        public async Task RemoveContactFromCache(int id, int progenyId)
        {
            await _cache.RemoveAsync(Constants.AppName + Constants.ApiVersion + "contact" + id);

            _ = await SetContactsListInCache(progenyId);
        }

        public async Task<List<Contact>> GetContactsList(int progenyId)
        {
            List<Contact> contactsList = await GetContactsListFromCache(progenyId);
            if (!contactsList.Any())
            {
                contactsList = await SetContactsListInCache(progenyId);
            }
            
            return contactsList;
        }

        private async Task<List<Contact>> GetContactsListFromCache(int progenyId)
        {
            List<Contact> contactsList = new List<Contact>();
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

    }
}
