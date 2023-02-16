using System.Collections.Generic;
using System.Threading.Tasks;
using KinaUna.Data.Models;

namespace KinaUnaWeb.Services
{
    public interface IContactsHttpClient
    {
        /// <summary>
        /// Gets the Contact with a given ContactId.
        /// </summary>
        /// <param name="contactId">int: The Id of the Contact (Contact.ContactId).</param>
        /// <returns>Contact.</returns>
        Task<Contact> GetContact(int contactId);

        /// <summary>
        /// Adds a new Contact.
        /// </summary>
        /// <param name="contact">Contact: The Contact object to add.</param>
        /// <returns>Contact: The Contact object that was added.</returns>
        Task<Contact> AddContact(Contact contact);

        /// <summary>
        /// Updates a Contact. The Contact with the same ContactId will be updated.
        /// </summary>
        /// <param name="contact">Contact: The Contact object to update.</param>
        /// <returns>Contact: The updated Contact object.</returns>
        Task<Contact> UpdateContact(Contact contact);

        /// <summary>
        /// Removes the Contact with the given ContactId.
        /// </summary>
        /// <param name="contactId">int: The Id of the Contact object to remove (Contact.ContactId).</param>
        /// <returns>bool: True if the Contact was successfully removed.</returns>
        Task<bool> DeleteContact(int contactId);

        /// <summary>
        /// Gets the list of Contact objects for a progeny that a user has access to.
        /// </summary>
        /// <param name="progenyId">int: The id of the progeny (Progeny.Id).</param>
        /// <param name="accessLevel">int: The user's access level.</param>
        /// <returns>List of Contact objects.</returns>
        Task<List<Contact>> GetContactsList(int progenyId, int accessLevel = 5);
    }
}
