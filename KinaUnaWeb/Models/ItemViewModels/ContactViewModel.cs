using System;
using System.Collections.Generic;
using KinaUna.Data.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace KinaUnaWeb.Models.ItemViewModels
{
    public class ContactViewModel: BaseItemsViewModel
    {
        public int ContactId { get; set; }

        public bool Active { get; set; }
        public string FirstName { get; set; }
        public string MiddleName { get; set; }
        public string LastName { get; set; }
        public string DisplayName { get; set; }
        public int? AddressIdNumber { get; set; }
        public string Email1 { get; set; }
        public string Email2 { get; set; }
        public string PhoneNumber { get; set; }
        public string MobileNumber { get; set; }
        public string Notes { get; set; }
        public string PictureLink { get; set; }
        public string Website { get; set; }
        public string Context { get; set; }
        public string Author { get; set; }
        public List<SelectListItem> ProgenyList { get; set; }
        public List<SelectListItem> AccessLevelListEn { get; set; }
        public List<SelectListItem> AccessLevelListDa { get; set; }
        public List<SelectListItem> AccessLevelListDe { get; set; }
        
        public Address Address { get; set; }
        public string AddressLine1 { get; set; }
        public string AddressLine2 { get; set; }
        public string City { get; set; }
        public string State { get; set; }
        public string PostalCode { get; set; }
        public string Country { get; set; }
        public string FileName { get; set; }
        public IFormFile File { get; set; }
        public string TagFilter { get; set; }
        public DateTime? DateAdded { get; set; }
        
        public Contact Contact { get; set; }
        
        public ContactViewModel()
        {
            ProgenyList = new List<SelectListItem>();
            SetAccessLevelList();
        }

        public ContactViewModel(BaseItemsViewModel baseItemsViewModel)
        {
            SetBaseProperties(baseItemsViewModel);
            SetAccessLevelList();
            ProgenyList = new List<SelectListItem>();
        }

        public ContactViewModel(Contact contact, bool isAdmin, UserInfo userInfo)
        {
            CurrentUser = userInfo;
            SetPropertiesFromContact(contact, isAdmin);
            SetAccessLevelList();
            ProgenyList = new List<SelectListItem>();
        }

        public void SetAccessLevelList()
        {
            AccessLevelList accList = new AccessLevelList();
            AccessLevelListEn = accList.AccessLevelListEn;
            AccessLevelListDa = accList.AccessLevelListDa;
            AccessLevelListDe = accList.AccessLevelListDe;

            AccessLevelListEn[CurrentAccessLevel].Selected = true;
            AccessLevelListDa[CurrentAccessLevel].Selected = true;
            AccessLevelListDe[CurrentAccessLevel].Selected = true;

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
            CurrentProgenyId = contact.ProgenyId;
            CurrentAccessLevel = contact.AccessLevel;
            FirstName = contact.FirstName;
            MiddleName = contact.MiddleName;
            LastName = contact.LastName;
            DisplayName = contact.DisplayName;
            AddressIdNumber = contact.AddressIdNumber;
            Email1 = contact.Email1;
            Email2 = contact.Email2;
            PhoneNumber = contact.PhoneNumber;
            MobileNumber = contact.MobileNumber;
            Notes = contact.Notes;
            PictureLink = contact.PictureLink;
            Active = contact.Active;
            ContactId = contact.ContactId;
            Context = contact.Context;
            Website = contact.Website;
            Tags = contact.Tags;
            Author = contact.Author;
            IsCurrentUserProgenyAdmin = isAdmin;

            DateTime tempTime = contact.DateAdded ?? DateTime.UtcNow;
            DateAdded = TimeZoneInfo.ConvertTimeFromUtc(tempTime, TimeZoneInfo.FindSystemTimeZoneById(CurrentUser.Timezone));

            if (contact.Address != null)
            {
                AddressIdNumber = contact.AddressIdNumber;
                AddressLine1 = contact.Address.AddressLine1;
                AddressLine2 = contact.Address.AddressLine2;
                City = contact.Address.City;
                State = contact.Address.State;
                PostalCode = contact.Address.PostalCode;
                Country = contact.Address.Country;
            }
        }

        public Contact CreateContact()
        {
            Contact contactItem = new Contact();
            contactItem.ContactId = ContactId;
            contactItem.FirstName = FirstName;
            contactItem.MiddleName = MiddleName;
            contactItem.LastName = LastName;
            contactItem.DisplayName = DisplayName;
            contactItem.Email1 = Email1;
            contactItem.Email2 = Email2;
            contactItem.PhoneNumber = PhoneNumber;
            contactItem.MobileNumber = MobileNumber;
            contactItem.Notes = Notes;
            contactItem.Website = Website;
            contactItem.Active = true;
            contactItem.Context = Context;
            contactItem.AccessLevel = CurrentAccessLevel;
            contactItem.Author = CurrentUser.UserId;
            contactItem.ProgenyId = CurrentProgenyId;
            contactItem.PictureLink = PictureLink;

            if (DateAdded == null)
            {
                contactItem.DateAdded = DateTime.UtcNow;
            }
            else
            {
                contactItem.DateAdded = TimeZoneInfo.ConvertTimeToUtc(DateAdded.Value, TimeZoneInfo.FindSystemTimeZoneById(CurrentUser.Timezone));
            }
            
            if (!string.IsNullOrEmpty(Tags))
            {
                contactItem.Tags = Tags.TrimEnd(',', ' ').TrimStart(',', ' ');
            }

            if (AddressLine1 + AddressLine2 + City + Country + PostalCode + State != "")
            {
                Address address = new Address();
                address.AddressLine1 = AddressLine1;
                address.AddressLine2 = AddressLine2;
                address.City = City;
                address.PostalCode = PostalCode;
                address.State = State;
                address.Country = Country;
                contactItem.Address = address;
            }

            return contactItem;
        }
    }
}
