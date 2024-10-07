using System;
using System.Collections.Generic;
using KinaUna.Data.Models;
using KinaUnaWeb.Models.TypeScriptModels.Locations;
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

        public Location LocationItem { get; set; } = new();
        public string HereMapsApiKey { get; init; } = "";
        public int Sort { get; set; }
        public int SortTags { get; set; }
        public LocationsPageParameters LocationsPageParameters { get; set; }
        public int LocationId { get; set; }

        public LocationViewModel()
        {
            LocationsList = [];
            ProgenyList = [];
            AccessLevelList accessLevelList = new();
            AccessLevelListEn = accessLevelList.AccessLevelListEn;
            AccessLevelListDa = accessLevelList.AccessLevelListDa;
            AccessLevelListDe = accessLevelList.AccessLevelListDe;
        }

        public LocationViewModel(BaseItemsViewModel baseItemsViewModel)
        {
            SetBaseProperties(baseItemsViewModel);
            SetAccessLevelList();
            ProgenyList = [];
        }

        public void SetAccessLevelList()
        {
            AccessLevelList accList = new();
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

        public void SetPropertiesFromLocation(Location location, string timeZone)
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
            Location location = new()
            {
                LocationId = LocationItem.LocationId,
                Latitude = LocationItem.Latitude,
                Longitude = LocationItem.Longitude,
                Name = LocationItem.Name,
                HouseNumber = LocationItem.HouseNumber,
                StreetName = LocationItem.StreetName,
                District = LocationItem.District,
                City = LocationItem.City,
                PostalCode = LocationItem.PostalCode,
                County = LocationItem.County,
                State = LocationItem.State,
                Country = LocationItem.Country,
                Notes = LocationItem.Notes
            };
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
