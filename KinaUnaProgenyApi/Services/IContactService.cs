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
        /// <param name="currentUserInfo">The UserInfo object for the current user, to check permissions.</param>
        /// <returns>Contact object. Null if a Contact entity with the given ContactId doesn't exist.</returns>
        Task<Contact> GetContact(int id, UserInfo currentUserInfo);

        /// <summary>
        /// Adds a new Contact to the database and the cache.
        /// </summary>
        /// <param name="contact">The Contact object to add.</param>
        /// <param name="currentUserInfo"></param>
        /// <returns>The updated Contact object.</returns>
        Task<Contact> AddContact(Contact contact, UserInfo currentUserInfo);

        /// <summary>
        /// Updates a Contact in the database and the cache.
        /// </summary>
        /// <param name="contact">The Contact object with the updated properties.</param>
        /// <param name="currentUserInfo"></param>
        /// <returns>The updated Contact object.</returns>
        Task<Contact> UpdateContact(Contact contact, UserInfo currentUserInfo);

        /// <summary>
        /// Deletes a Contact from the database and the cache. 
        /// </summary>
        /// <param name="contact">The Contact to delete.</param>
        /// <param name="currentUserInfo"></param>
        /// <returns>The deleted Contact.</returns>
        Task<Contact> DeleteContact(Contact contact, UserInfo currentUserInfo);

        /// <summary>
        /// Gets a list of all Contacts for a Progeny from the cache.
        /// If the list is empty, it will be looked up in the database and added to the cache.
        /// </summary>
        /// <param name="progenyId">The ProgenyId of the Progeny to get all Contacts for.</param>
        /// <param name="familyId"></param>
        /// <param name="currentUserInfo">The UserInfo object for the current user, to check permissions.</param>
        /// <returns>List of Contacts.</returns>
        Task<List<Contact>> GetContactsList(int progenyId, int familyId, UserInfo currentUserInfo);

        /// <summary>
        /// Gets a list of all Contacts for a Progeny with the given tag.
        /// </summary>
        /// <param name="progenyId">The ProgenyId of the Progeny to get all Contacts for.</param>
        /// <param name="familyId"></param>
        /// <param name="tag">The tag to filter contacts by.</param>
        /// <param name="currentUserInfo">The UserInfo object for the current user, to check permissions.</param>
        /// <returns></returns>
        Task<List<Contact>> GetContactsWithTag(int progenyId, int familyId, string tag, UserInfo currentUserInfo);

        /// <summary>
        /// Gets a list of all Contacts for a Progeny with the given context.
        /// </summary>
        /// <param name="progenyId">The ProgenyId of the Progeny to get all Contacts for.</param>
        /// <param name="familyId"></param>
        /// <param name="context">The context to filter contacts by.</param>
        /// <param name="currentUserInfo">The UserInfo object for the current user, to check permissions.</param>
        /// <returns></returns>
        Task<List<Contact>> GetContactsWithContext(int progenyId, int familyId, string context, UserInfo currentUserInfo);
    }
}
