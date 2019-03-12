using System;
using System.Collections.Generic;
using KinaUna.Data.Models;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace KinaUnaWeb.Models.ItemViewModels
{
    public class LocationViewModel
    {
        public int LocationId { get; set; }
        public int ProgenyId { get; set; }
        public string Name { get; set; }
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public string StreetName { get; set; }
        public string HouseNumber { get; set; }
        public string City { get; set; }
        public string District { get; set; }
        public string County { get; set; }
        public string State { get; set; }
        public string Country { get; set; }
        public string PostalCode { get; set; }
        public string Notes { get; set; }
        public DateTime? Date { get; set; }
        public int AccessLevel { get; set; }
        public string Tags { get; set; }
        public DateTime? DateAdded { get; set; }
        public string Author { get; set; }

        public List<Location> LocationsList { get; set; }
        public string TagsList { get; set; }
        public Progeny Progeny { get; set; }
        public List<SelectListItem> ProgenyList { get; set; }
        public bool IsAdmin { get; set; }
        public List<SelectListItem> AccessLevelListEn { get; set; }
        public List<SelectListItem> AccessLevelListDa { get; set; }
        public List<SelectListItem> AccessLevelListDe { get; set; }

        public string TagFilter { get; set; }
        public int? SortBy { get; set; }

        public LocationViewModel()
        {
            LocationsList = new List<Location>();
            ProgenyList = new List<SelectListItem>();
            AccessLevelList aclList = new AccessLevelList();
            AccessLevelListEn = aclList.AccessLevelListEn;
            AccessLevelListDa = aclList.AccessLevelListDa;
            AccessLevelListDe = aclList.AccessLevelListDe;
        }
    }
}
