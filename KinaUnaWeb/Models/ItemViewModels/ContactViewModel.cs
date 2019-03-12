using System;
using System.Collections.Generic;
using KinaUna.Data.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace KinaUnaWeb.Models.ItemViewModels
{
    public class ContactViewModel
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
        public int AccessLevel { get; set; }
        public string Author { get; set; }
        public int ProgenyId { get; set; }
        public Progeny Progeny { get; set; }
        public List<SelectListItem> ProgenyList { get; set; }
        public bool IsAdmin { get; set; }
        public List<SelectListItem> AccessLevelListEn { get; set; }
        public List<SelectListItem> AccessLevelListDa { get; set; }
        public List<SelectListItem> AccessLevelListDe { get; set; }
        public string Context { get; set; }

        public Address Address { get; set; }
        public string AddressLine1 { get; set; }
        public string AddressLine2 { get; set; }
        public string City { get; set; }
        public string State { get; set; }
        public string PostalCode { get; set; }
        public string Country { get; set; }
        public string FileName { get; set; }
        public IFormFile File { get; set; }
        public string Tags { get; set; }
        public DateTime? DateAdded { get; set; }
        public ContactViewModel()
        {
            ProgenyList = new List<SelectListItem>();

            AccessLevelList accList = new AccessLevelList();
            AccessLevelListEn = accList.AccessLevelListEn;
            AccessLevelListDa = accList.AccessLevelListDa;
            AccessLevelListDe = accList.AccessLevelListDe;
        }
    }
}
