using System.Collections.Generic;
using System.Threading.Tasks;
using KinaUna.Data.Models;

namespace KinaUnaProgenyApi.Services
{
    public interface IContactService
    {
        /// <summary>
        /// Gets a Contact by ContactId from the cache.
        /// If the Contact isn't in the cache, it will be looked up in the database and added to the cache.
        /// </summary>
        /// <param name="id">The ContactId of the Contact to get.</param>
        /// <returns>Contact object. Null if a Contact entity with the given ContactId doesn't exist.</returns>
        Task<Contact> GetContact(int id);

        /// <summary>
        /// Adds a new Contact to the database and the cache.
        /// </summary>
        /// <param name="contact">The Contact object to add.</param>
        /// <returns>The updated Contact object.</returns>
        Task<Contact> AddContact(Contact contact);

        /// <summary>
        /// Gets a Contact by ContactId from the database and adds it to the cache.
        /// Also updates the ContactsList for the Progeny that the Contact belongs to in the cache.
        /// </summary>
        /// <param name="id">The ContactId of the Contact to get and set.</param>
        /// <returns>The Contact object. Null if a Contact with the given ContactId doesn't exist.</returns>
        Task<Contact> SetContactInCache(int id);

        /// <summary>
        /// Updates a Contact in the database and the cache.
        /// </summary>
        /// <param name="contact">The Contact object with the updated properties.</param>
        /// <returns>The updated Contact object.</returns>
        Task<Contact> UpdateContact(Contact contact);

        /// <summary>
        /// Deletes a Contact from the database and the cache. 
        /// </summary>
        /// <param name="contact">The Contact to delete.</param>
        /// <returns>The deleted Contact.</returns>
        Task<Contact> DeleteContact(Contact contact);

        /// <summary>
        /// Deletes a Contact from the cache and updates the ContactsList for the Progeny that the Contact belongs to.
        /// </summary>
        /// <param name="id">The ContactId of the Contact to delete.</param>
        /// <param name="progenyId">The ProgenyId of the Progeny the Contact item belongs to.</param>
        /// <returns></returns>
        Task RemoveContactFromCache(int id, int progenyId);

        /// <summary>
        /// Gets a list of all Contacts for a Progeny from the cache.
        /// If the list is empty, it will be looked up in the database and added to the cache.
        /// </summary>
        /// <param name="progenyId">The ProgenyId of the Progeny to get all Contacts for.</param>
        /// <param name="accessLevel">The access level required to view the Contact.</param>
        /// <returns>List of Contacts.</returns>
        Task<List<Contact>> GetContactsList(int progenyId, int accessLevel);

        Task<List<Contact>> GetContactsWithTag(int progenyId, string tag, int accessLevel);

        Task<List<Contact>> GetContactsWithContext(int progenyId, string context, int accessLevel);
    }
}
