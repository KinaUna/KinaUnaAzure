using System;
using System.Collections.Generic;
using KinaUna.Data.Models;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace KinaUnaWeb.Models.ItemViewModels
{
    public class LocationViewModel: BaseItemsViewModel
    {
        public int LocationId { get; set; }
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
        public DateTime? DateAdded { get; set; }
        public string Author { get; set; }

        public List<Location> LocationsList { get; set; }
        public List<SelectListItem> ProgenyList { get; set; }
        public List<SelectListItem> AccessLevelListEn { get; set; }
        public List<SelectListItem> AccessLevelListDa { get; set; }
        public List<SelectListItem> AccessLevelListDe { get; set; }

        public string TagFilter { get; set; }
        public int? SortBy { get; set; }

        public Location Location { get; set; }

        public LocationViewModel()
        {
            LocationsList = new List<Location>();
            ProgenyList = new List<SelectListItem>();
            AccessLevelList aclList = new AccessLevelList();
            AccessLevelListEn = aclList.AccessLevelListEn;
            AccessLevelListDa = aclList.AccessLevelListDa;
            AccessLevelListDe = aclList.AccessLevelListDe;
        }

        public LocationViewModel(BaseItemsViewModel baseItemsViewModel)
        {
            SetBaseProperties(baseItemsViewModel);
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

        public void SetPropertiesFromLocation(Location location, bool isAdmin, string timeZone)
        {
            LocationId = location.LocationId;
            Latitude = location.Latitude;
            Longitude = location.Longitude;
            Name = location.Name;
            HouseNumber = location.HouseNumber;
            StreetName = location.StreetName;
            District = location.District;
            City = location.City;
            PostalCode = location.PostalCode;
            County = location.County;
            State = location.State;
            Country = location.Country;
            Notes = location.Notes;
            if (location.Date.HasValue)
            {
                Date = TimeZoneInfo.ConvertTimeFromUtc(location.Date.Value, TimeZoneInfo.FindSystemTimeZoneById(timeZone));
            }

            Tags = location.Tags;
            CurrentProgenyId = location.ProgenyId;
            DateAdded = location.DateAdded;
            Author = location.Author;
        }

        public Location CreateLocation()
        {
            Location location = new Location();
            location.Latitude = Latitude;
            location.Longitude = Longitude;
            location.Name = Name;
            location.HouseNumber = HouseNumber;
            location.StreetName = StreetName;
            location.District = District;
            location.City = City;
            location.PostalCode = PostalCode;
            location.County = County;
            location.State = State;
            location.Country = Country;
            location.Notes = Notes;
            if (Date.HasValue)
            {
                location.Date = TimeZoneInfo.ConvertTimeToUtc(Date.Value, TimeZoneInfo.FindSystemTimeZoneById(CurrentProgeny.TimeZone));
            }

            if (!string.IsNullOrEmpty(Tags))
            {
                location.Tags = Tags.Trim().TrimEnd(',', ' ').TrimStart(',', ' ');
            }

            location.ProgenyId = CurrentProgenyId;
            location.DateAdded = DateTime.UtcNow;
            location.Author = CurrentUser.UserId;
            location.AccessLevel = CurrentAccessLevel;

            return location;
        }
    }
}
