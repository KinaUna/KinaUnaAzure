using System.Collections.Generic;
using System.Threading.Tasks;
using KinaUna.Data.Models;

namespace KinaUnaWeb.Services.HttpClients
{
    /// <summary>
    /// Provides methods for interacting with the Contacts API.
    /// </summary>
    public interface IContactsHttpClient
    {
        /// <summary>
        /// Gets the Contact with a given ContactId.
        /// </summary>
        /// <param name="contactId">The ContactId of the Contact to get.</param>
        /// <returns>Contact with the given ContactId. If not found, a new Contact with ContactId = 0.</returns>
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
        /// <param name="contactId">int: The ContactId of the Contact object to remove.</param>
        /// <returns>bool: True if the Contact was successfully removed.</returns>
        Task<bool> DeleteContact(int contactId);

        /// <summary>
        /// Gets the list of Contact objects for a progeny that a user has access to.
        /// </summary>
        /// <param name="progenyId">The Id of the Progeny.</param>
        /// <param name="accessLevel">The user's access level for the Progeny.</param>
        /// <param name="tagFilter">String to filter the result list by, only items with the tagFilter string in the Tags property are included. If empty string all items are included.</param>
        /// <returns>List of Contact objects.</returns>
        Task<List<Contact>> GetContactsList(int progenyId, int accessLevel = 5, string tagFilter = "");
    }
}
