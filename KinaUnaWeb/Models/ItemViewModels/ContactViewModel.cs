using System;
using System.Collections.Generic;
using KinaUna.Data.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace KinaUnaWeb.Models.ItemViewModels
{
    public class ContactViewModel: BaseItemsViewModel
    {
        public List<SelectListItem> ProgenyList { get; set; }
        public List<SelectListItem> AccessLevelListEn { get; set; }
        public List<SelectListItem> AccessLevelListDa { get; set; }
        public List<SelectListItem> AccessLevelListDe { get; set; }
        public string FileName { get; set; }
        public IFormFile File { get; init; }
        public string TagFilter { get; set; }
        public Address AddressItem { get; init; } = new();
        public Contact ContactItem { get; set; } = new();
        
        public ContactViewModel()
        {
            ProgenyList = [];
            SetAccessLevelList();
        }

        public ContactViewModel(BaseItemsViewModel baseItemsViewModel)
        {
            SetBaseProperties(baseItemsViewModel);
            SetAccessLevelList();
            ProgenyList = [];
        }

        public ContactViewModel(Contact contact, bool isAdmin, UserInfo userInfo)
        {
            CurrentUser = userInfo;
            SetPropertiesFromContact(contact, isAdmin);
            SetAccessLevelList();
            ProgenyList = [];
        }

        public void SetProgenyList()
        {
            ContactItem.ProgenyId = CurrentProgenyId;
            foreach (SelectListItem item in ProgenyList)
            {
                if (item.Value == CurrentProgenyId.ToString())
                {
                    item.Selected = true;
                }
                else
                {
                    item.Selected = false;
                }
            }
        }

        public void SetAccessLevelList()
        {
            AccessLevelList accessLevelList = new();
            AccessLevelListEn = accessLevelList.AccessLevelListEn;
            AccessLevelListDa = accessLevelList.AccessLevelListDa;
            AccessLevelListDe = accessLevelList.AccessLevelListDe;

            AccessLevelListEn[ContactItem.AccessLevel].Selected = true;
            AccessLevelListDa[ContactItem.AccessLevel].Selected = true;
            AccessLevelListDe[ContactItem.AccessLevel].Selected = true;

            if (LanguageId == 2)
            {
                AccessLevelListEn = AccessLevelListDe;
            }

            if (LanguageId == 3)
            {
                AccessLevelListEn = AccessLevelListDa;
            }
        }

        public void SetPropertiesFromContact(Contact contact, bool isAdmin)
        {
            ContactItem.ProgenyId = contact.ProgenyId;
            ContactItem.AccessLevel = contact.AccessLevel;
            ContactItem.FirstName = contact.FirstName;
            ContactItem.MiddleName = contact.MiddleName;
            ContactItem.LastName = contact.LastName;
            ContactItem.DisplayName = contact.DisplayName;
            ContactItem.AddressIdNumber = contact.AddressIdNumber;
            ContactItem.Email1 = contact.Email1;
            ContactItem.Email2 = contact.Email2;
            ContactItem.PhoneNumber = contact.PhoneNumber;
            ContactItem.MobileNumber = contact.MobileNumber;
            ContactItem.Notes = contact.Notes;
            ContactItem.PictureLink = contact.PictureLink;
            ContactItem.Active = contact.Active;
            ContactItem.ContactId = contact.ContactId;
            ContactItem.Context = contact.Context;
            ContactItem.Website = contact.Website;
            ContactItem.Tags = Tags = contact.Tags;
            ContactItem.Author = contact.Author ?? CurrentUser.UserId;
            IsCurrentUserProgenyAdmin = isAdmin;

            DateTime tempTime = contact.DateAdded ?? DateTime.UtcNow;
            ContactItem.DateAdded = TimeZoneInfo.ConvertTimeFromUtc(tempTime, TimeZoneInfo.FindSystemTimeZoneById(CurrentUser.Timezone));

            if (contact.Address == null) return;

            ContactItem.AddressIdNumber = contact.AddressIdNumber;
            AddressItem.AddressLine1 = contact.Address.AddressLine1;
            AddressItem.AddressLine2 = contact.Address.AddressLine2;
            AddressItem.City = contact.Address.City;
            AddressItem.State = contact.Address.State;
            AddressItem.PostalCode = contact.Address.PostalCode;
            AddressItem.Country = contact.Address.Country;
        }

        public Contact CreateContact()
        {
            Contact contactItem = new()
            {
                ContactId = ContactItem.ContactId,
                FirstName = ContactItem.FirstName,
                MiddleName = ContactItem.MiddleName,
                LastName = ContactItem.LastName,
                DisplayName = ContactItem.DisplayName,
                Email1 = ContactItem.Email1,
                Email2 = ContactItem.Email2,
                PhoneNumber = ContactItem.PhoneNumber,
                MobileNumber = ContactItem.MobileNumber,
                Notes = ContactItem.Notes,
                Website = ContactItem.Website,
                Active = true,
                Context = ContactItem.Context,
                AccessLevel = ContactItem.AccessLevel,
                Author = ContactItem.Author ?? CurrentUser.UserId,
                ProgenyId = ContactItem.ProgenyId,
                PictureLink = ContactItem.PictureLink
            };

            if (ContactItem.DateAdded == null)
            {
                contactItem.DateAdded = DateTime.UtcNow;
            }
            else
            {
                contactItem.DateAdded = TimeZoneInfo.ConvertTimeToUtc(ContactItem.DateAdded.Value, TimeZoneInfo.FindSystemTimeZoneById(CurrentUser.Timezone));
            }
            
            if (!string.IsNullOrEmpty(ContactItem.Tags))
            {
                contactItem.Tags = ContactItem.Tags.TrimEnd(',', ' ').TrimStart(',', ' ');
            }

            if (AddressItem.AddressLine1 + AddressItem.AddressLine2 + AddressItem.City + AddressItem.Country + AddressItem.PostalCode + AddressItem.State == "") return contactItem;

            Address address = new()
            {
                AddressLine1 = AddressItem.AddressLine1,
                AddressLine2 = AddressItem.AddressLine2,
                City = AddressItem.City,
                PostalCode = AddressItem.PostalCode,
                State = AddressItem.State,
                Country = AddressItem.Country
            };
            contactItem.Address = address;

            return contactItem;
        }
    }
}
