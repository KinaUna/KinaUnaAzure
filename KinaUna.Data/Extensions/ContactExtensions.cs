using KinaUna.Data.Models;
using System;

namespace KinaUna.Data.Extensions
{
    public static class ContactExtensions
    {
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
    }
}
