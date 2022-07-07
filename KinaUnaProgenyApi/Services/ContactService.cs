using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using KinaUna.Data;
using KinaUna.Data.Contexts;
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
            Contact contact;
            string cachedContact = await _cache.GetStringAsync(Constants.AppName + Constants.ApiVersion + "contact" + id);
            if (!string.IsNullOrEmpty(cachedContact))
            {
                contact = JsonConvert.DeserializeObject<Contact>(cachedContact);
            }
            else
            {
                contact = await _context.ContactsDb.AsNoTracking().SingleOrDefaultAsync(c => c.ContactId == id);
                await _cache.SetStringAsync(Constants.AppName + Constants.ApiVersion + "contact" + id, JsonConvert.SerializeObject(contact), _cacheOptionsSliding);
            }

            return contact;
        }

        public async Task<Contact> AddContact(Contact contact)
        {
            _context.ContactsDb.Add(contact);
            await _context.SaveChangesAsync();
            await SetContact(contact.ContactId);

            return contact;
        }
        public async Task<Contact> SetContact(int id)
        {
            Contact contact = await _context.ContactsDb.AsNoTracking().SingleOrDefaultAsync(c => c.ContactId == id);
            if (contact != null)
            {
                await _cache.SetStringAsync(Constants.AppName + Constants.ApiVersion + "contact" + id, JsonConvert.SerializeObject(contact), _cacheOptionsSliding);

                List<Contact> contactsList = await _context.ContactsDb.AsNoTracking().Where(c => c.ProgenyId == contact.ProgenyId).ToListAsync();
                await _cache.SetStringAsync(Constants.AppName + Constants.ApiVersion + "contactslist" + contact.ProgenyId, JsonConvert.SerializeObject(contactsList), _cacheOptionsSliding);
            }
            
            return contact;
        }

        public async Task<Contact> UpdateContact(Contact contact)
        {
            _context.ContactsDb.Update(contact);
            await _context.SaveChangesAsync();
            await SetContact(contact.ContactId);

            return contact;
        }

        public async Task<Contact> DeleteContact(Contact contact)
        {
            _context.ContactsDb.Remove(contact);
            await _context.SaveChangesAsync();
            await RemoveContact(contact.ContactId, contact.ProgenyId);

            return contact;
        }
        public async Task RemoveContact(int id, int progenyId)
        {
            await _cache.RemoveAsync(Constants.AppName + Constants.ApiVersion + "contact" + id);

            List<Contact> contactsList = await _context.ContactsDb.AsNoTracking().Where(c => c.ProgenyId == progenyId).ToListAsync();
            await _cache.SetStringAsync(Constants.AppName + Constants.ApiVersion + "contactslist" + progenyId, JsonConvert.SerializeObject(contactsList), _cacheOptionsSliding);
        }

        public async Task<List<Contact>> GetContactsList(int progenyId)
        {
            List<Contact> contactsList;
            string cachedContactsList = await _cache.GetStringAsync(Constants.AppName + Constants.ApiVersion + "contactslist" + progenyId);
            if (!string.IsNullOrEmpty(cachedContactsList))
            {
                contactsList = JsonConvert.DeserializeObject<List<Contact>>(cachedContactsList);
            }
            else
            {
                contactsList = await _context.ContactsDb.AsNoTracking().Where(c => c.ProgenyId == progenyId).ToListAsync();
                await _cache.SetStringAsync(Constants.AppName + Constants.ApiVersion + "contactslist" + progenyId, JsonConvert.SerializeObject(contactsList), _cacheOptionsSliding);
            }

            return contactsList;
        }

    }
}
