using System;
using System.Collections.Generic;
using KinaUna.Data.Models;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace KinaUnaWeb.Models.ItemViewModels
{
    public class LocationViewModel: BaseItemsViewModel
    {
        public List<Location> LocationsList { get; set; }
        public List<SelectListItem> ProgenyList { get; set; }
        public List<SelectListItem> AccessLevelListEn { get; set; }
        public List<SelectListItem> AccessLevelListDa { get; set; }
        public List<SelectListItem> AccessLevelListDe { get; set; }

        public string TagFilter { get; set; }
        public int? SortBy { get; set; }

        public Location LocationItem { get; set; } = new Location();

        public LocationViewModel()
        {
            LocationsList = new List<Location>();
            ProgenyList = new List<SelectListItem>();
            AccessLevelList accessLevelList = new AccessLevelList();
            AccessLevelListEn = accessLevelList.AccessLevelListEn;
            AccessLevelListDa = accessLevelList.AccessLevelListDa;
            AccessLevelListDe = accessLevelList.AccessLevelListDe;
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

            AccessLevelListEn[LocationItem.AccessLevel].Selected = true;
            AccessLevelListDa[LocationItem.AccessLevel].Selected = true;
            AccessLevelListDe[LocationItem.AccessLevel].Selected = true;

            if (LanguageId == 2)
            {
                AccessLevelListEn = AccessLevelListDe;
            }

            if (LanguageId == 3)
            {
                AccessLevelListEn = AccessLevelListDa;
            }
        }

        public void SetProgenyList()
        {
            LocationItem.ProgenyId = CurrentProgenyId;
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

        public void SetPropertiesFromLocation(Location location, bool isAdmin, string timeZone)
        {
            LocationItem.LocationId = location.LocationId;
            LocationItem.Latitude = location.Latitude;
            LocationItem.Longitude = location.Longitude;
            LocationItem.Name = location.Name;
            LocationItem.HouseNumber = location.HouseNumber;
            LocationItem.StreetName = location.StreetName;
            LocationItem.District = location.District;
            LocationItem.City = location.City;
            LocationItem.PostalCode = location.PostalCode;
            LocationItem.County = location.County;
            LocationItem.State = location.State;
            LocationItem.Country = location.Country;
            LocationItem.Notes = location.Notes;
            if (location.Date.HasValue)
            {
                LocationItem.Date = TimeZoneInfo.ConvertTimeFromUtc(location.Date.Value, TimeZoneInfo.FindSystemTimeZoneById(timeZone));
            }

            LocationItem.Tags = location.Tags;
            LocationItem.ProgenyId = location.ProgenyId;
            LocationItem.DateAdded = location.DateAdded;
            LocationItem.Author = location.Author;
        }

        public Location CreateLocation()
        {
            Location location = new Location();
            location.LocationId = LocationItem.LocationId;
            location.Latitude = LocationItem.Latitude;
            location.Longitude = LocationItem.Longitude;
            location.Name = LocationItem.Name;
            location.HouseNumber = LocationItem.HouseNumber;
            location.StreetName = LocationItem.StreetName;
            location.District = LocationItem.District;
            location.City = LocationItem.City;
            location.PostalCode = LocationItem.PostalCode;
            location.County = LocationItem.County;
            location.State = LocationItem.State;
            location.Country = LocationItem.Country;
            location.Notes = LocationItem.Notes;
            if (LocationItem.Date.HasValue)
            {
                location.Date = TimeZoneInfo.ConvertTimeToUtc(LocationItem.Date.Value, TimeZoneInfo.FindSystemTimeZoneById(CurrentUser.Timezone));
            }

            if (!string.IsNullOrEmpty(LocationItem.Tags))
            {
                location.Tags = LocationItem.Tags.Trim().TrimEnd(',', ' ').TrimStart(',', ' ');
            }

            location.ProgenyId = LocationItem.ProgenyId;
            location.DateAdded = LocationItem.DateAdded;
            location.Author = location.Author;
            location.AccessLevel = LocationItem.AccessLevel;

            return location;
        }
    }
}
