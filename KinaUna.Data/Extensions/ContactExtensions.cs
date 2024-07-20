using KinaUna.Data.Models;
using System;

namespace KinaUna.Data.Extensions
{
    /// <summary>
    /// Extension methods for the Contact class.
    /// </summary>
    public static class ContactExtensions
    {
        /// <summary>
        /// Copies the properties needed for updating a Contact entity from one Contact object to another.
        /// </summary>
        /// <param name="currentContact"></param>
        /// <param name="otherContact"></param>
        public static void CopyPropertiesForUpdate(this Contact currentContact, Contact otherContact )
        {
            currentContact.ContactId = otherContact.ContactId;
            currentContact.AccessLevel = otherContact.AccessLevel;
            currentContact.Active = otherContact.Active;
            currentContact.AddressIdNumber = otherContact.AddressIdNumber;
            currentContact.AddressString = otherContact.AddressString;
            currentContact.ProgenyId = otherContact.ProgenyId;
            currentContact.Author = otherContact.Author;
            if (otherContact.DateAdded.HasValue)
            {
                currentContact.DateAdded = otherContact.DateAdded.Value;
            }
            else
            {
                currentContact.DateAdded = DateTime.UtcNow;
            }

            currentContact.Context = otherContact.Context;
            currentContact.DisplayName = otherContact.DisplayName;
            currentContact.Email1 = otherContact.Email1;
            currentContact.Email2 = otherContact.Email2;
            currentContact.FirstName = otherContact.FirstName;
            currentContact.LastName = otherContact.LastName;
            currentContact.MiddleName = otherContact.MiddleName;
            currentContact.MobileNumber = otherContact.MobileNumber;
            currentContact.Notes = otherContact.Notes;
            currentContact.PhoneNumber = otherContact.PhoneNumber;
            if (otherContact.PictureLink != Constants.KeepExistingLink)
            {
                currentContact.PictureLink = otherContact.PictureLink;
            }

            currentContact.Tags = otherContact.Tags;
            currentContact.Website = otherContact.Website;
            currentContact.Address = otherContact.Address;
        }

        /// <summary>
        /// Copies the properties needed for adding a Contact entity from one Contact object to another.
        /// </summary>
        /// <param name="currentContact"></param>
        /// <param name="otherContact"></param>
        public static void CopyPropertiesForAdd(this Contact currentContact, Contact otherContact)
        {
            currentContact.AccessLevel = otherContact.AccessLevel;
            currentContact.Active = otherContact.Active;
            currentContact.AddressString = otherContact.AddressString;
            currentContact.ProgenyId = otherContact.ProgenyId;
            currentContact.Author = otherContact.Author;
            currentContact.DateAdded = otherContact.DateAdded ?? DateTime.UtcNow;
            currentContact.Context = otherContact.Context;
            currentContact.DisplayName = otherContact.DisplayName;
            currentContact.Email1 = otherContact.Email1;
            currentContact.Email2 = otherContact.Email2;
            currentContact.FirstName = otherContact.FirstName;
            currentContact.LastName = otherContact.LastName;
            currentContact.MiddleName = otherContact.MiddleName;
            currentContact.MobileNumber = otherContact.MobileNumber;
            currentContact.Notes = otherContact.Notes;
            currentContact.PhoneNumber = otherContact.PhoneNumber;
            currentContact.PictureLink = otherContact.PictureLink;
            currentContact.Tags = otherContact.Tags;
            currentContact.Website = otherContact.Website;
            currentContact.Address = otherContact.Address;
        }

        /// <summary>
        /// Produces a string with the url to the profile picture for a contact.
        /// </summary>
        /// <param name="contact"></param>
        /// <returns>String with the url</returns>
        public static string GetProfilePictureUrl(this Contact contact)
        {
            if (contact == null || contact.PictureLink == null)
            {
                return Constants.ProfilePictureUrl;
            }

            if (contact.PictureLink.StartsWith("http", StringComparison.CurrentCultureIgnoreCase))
            {
                return contact.PictureLink;
            }

            string pictureUrl = "/Contacts/ProfilePicture/" + contact.ContactId + "?imageId=" + contact.PictureLink;

            return pictureUrl;
        }

        /// <summary>
        /// Gets the MIME content type for the contact's profile picture.
        /// </summary>
        /// <param name="contact"></param>
        /// <returns>String with the content type.</returns>
        public static string GetPictureFileContentType(this Contact contact )
        {
            string contentType = FileContentTypeHelpers.GetContentTypeString(contact.PictureLink);

            return contentType;
        }
    }
}
