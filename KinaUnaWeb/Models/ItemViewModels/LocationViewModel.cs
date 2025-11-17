using KinaUna.Data.Models.DTOs;
using KinaUnaWeb.Models.TypeScriptModels.Locations;
using Microsoft.AspNetCore.Mvc.Rendering;
using System;
using System.Collections.Generic;
using System.Text.Json;

namespace KinaUnaWeb.Models.ItemViewModels
{
    public class LocationViewModel: BaseItemsViewModel
    {
        public List<Location> LocationsList { get; set; }
        
        public string TagFilter { get; set; }
        public int? SortBy { get; set; }

        public Location LocationItem { get; set; } = new();
        public string HereMapsApiKey { get; init; } = "";
        public int Sort { get; set; }
        public int SortTags { get; set; }
        public LocationsPageParameters LocationsPageParameters { get; set; }
        public int LocationId { get; set; }

        /// <summary>
        /// Parameterless constructor. Needed for initialization of the view model when objects are created in Razor views/passed as parameters in POST methods.
        /// </summary>
        public LocationViewModel()
        {
            LocationsList = [];
            ProgenyList = [];
            FamilyList = [];
        }

        public LocationViewModel(BaseItemsViewModel baseItemsViewModel)
        {
            SetBaseProperties(baseItemsViewModel);
            ProgenyList = [];
            FamilyList = [];
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

        public void SetFamilyList()
        {
            LocationItem.FamilyId = CurrentFamilyId;
            foreach (SelectListItem item in FamilyList)
            {
                if (item.Value == CurrentFamilyId.ToString())
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
            LocationItem.FamilyId = location.FamilyId;
            LocationItem.DateAdded = location.DateAdded;
            LocationItem.Author = location.Author;
            LocationItem.ItemPerMission = location.ItemPerMission;
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
                Notes = LocationItem.Notes,
                ItemPermissionsDtoList = string.IsNullOrWhiteSpace(ItemPermissionsListAsString) ? [] : JsonSerializer.Deserialize<List<ItemPermissionDto>>(ItemPermissionsListAsString, JsonSerializerOptions.Web)
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
            location.FamilyId = LocationItem.FamilyId;
            location.DateAdded = LocationItem.DateAdded;
            location.Author = location.Author;
            
            return location;
        }
    }
}
