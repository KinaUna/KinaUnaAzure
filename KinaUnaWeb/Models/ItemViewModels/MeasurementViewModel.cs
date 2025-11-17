using KinaUna.Data.Models.DTOs;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Collections.Generic;
using System.Text.Json;

namespace KinaUnaWeb.Models.ItemViewModels
{
    public class MeasurementViewModel: BaseItemsViewModel
    {
        public List<Measurement> MeasurementsList { get; set; }
        public Measurement MeasurementItem { get; set; } = new();
        public int MeasurementId { get; set; }

        /// <summary>
        /// Parameterless constructor. Needed for initialization of the view model when objects are created in Razor views/passed as parameters in POST methods.
        /// </summary>
        public MeasurementViewModel()
        {
            ProgenyList = [];
        }

        public MeasurementViewModel(BaseItemsViewModel baseItemsViewModel)
        {
            ProgenyList = [];
            SetBaseProperties(baseItemsViewModel);
        }

        public void SetProgenyList()
        {
            MeasurementItem.ProgenyId = CurrentProgenyId;
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
        
        public Measurement CreateMeasurement()
        {
            Measurement measurement = new()
            {
                MeasurementId = MeasurementItem.MeasurementId,
                ProgenyId = CurrentProgenyId,
                CreatedDate = MeasurementItem.CreatedDate,
                Date = MeasurementItem.Date,
                Height = MeasurementItem.Height,
                Weight = MeasurementItem.Weight,
                Circumference = MeasurementItem.Circumference,
                HairColor = MeasurementItem.HairColor,
                EyeColor = MeasurementItem.EyeColor,
                Author = MeasurementItem.Author,
                ItemPermissionsDtoList = string.IsNullOrWhiteSpace(ItemPermissionsListAsString) ? [] : JsonSerializer.Deserialize<List<ItemPermissionDto>>(ItemPermissionsListAsString, JsonSerializerOptions.Web)
            };

            return measurement;
        }

        public void SetPropertiesFromMeasurement(Measurement measurement)
        {
            MeasurementItem.ProgenyId = measurement.ProgenyId;
            MeasurementItem.MeasurementId = measurement.MeasurementId;
            MeasurementItem.Author = measurement.Author;
            MeasurementItem.CreatedDate = measurement.CreatedDate;
            MeasurementItem.Date = measurement.Date;
            MeasurementItem.Height = measurement.Height;
            MeasurementItem.Weight = measurement.Weight;
            MeasurementItem.Circumference = measurement.Circumference;
            MeasurementItem.HairColor = measurement.HairColor;
            MeasurementItem.EyeColor = measurement.EyeColor;
            MeasurementItem.ItemPerMission = measurement.ItemPerMission;
        }
    }
}
