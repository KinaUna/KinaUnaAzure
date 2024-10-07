using System.Collections.Generic;
using KinaUna.Data.Models;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace KinaUnaWeb.Models.ItemViewModels
{
    public class MeasurementViewModel: BaseItemsViewModel
    {
        public List<SelectListItem> ProgenyList { get; set; }
        public List<Measurement> MeasurementsList { get; set; }
        public List<SelectListItem> AccessLevelListEn { get; set; }
        public List<SelectListItem> AccessLevelListDa { get; set; }
        public List<SelectListItem> AccessLevelListDe { get; set; }
        public Measurement MeasurementItem { get; set; } = new();
        public int MeasurementId { get; set; }

        public MeasurementViewModel()
        {
            ProgenyList = [];
            SetAccessLevelList();
        }

        public MeasurementViewModel(BaseItemsViewModel baseItemsViewModel)
        {
            ProgenyList = [];
            SetBaseProperties(baseItemsViewModel);
            SetAccessLevelList();
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

        public void SetAccessLevelList()
        {
            AccessLevelList accessLevelList = new();
            AccessLevelListEn = accessLevelList.AccessLevelListEn;
            AccessLevelListDa = accessLevelList.AccessLevelListDa;
            AccessLevelListDe = accessLevelList.AccessLevelListDe;

            AccessLevelListEn[MeasurementItem.AccessLevel].Selected = true;
            AccessLevelListDa[MeasurementItem.AccessLevel].Selected = true;
            AccessLevelListDe[MeasurementItem.AccessLevel].Selected = true;

            if (LanguageId == 2)
            {
                AccessLevelListEn = AccessLevelListDe;
            }

            if (LanguageId == 3)
            {
                AccessLevelListEn = AccessLevelListDa;
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
                AccessLevel = MeasurementItem.AccessLevel,
                Author = MeasurementItem.Author
            };

            return measurement;
        }

        public void SetPropertiesFromMeasurement(Measurement measurement, bool isAdmin)
        {
            MeasurementItem.ProgenyId = measurement.ProgenyId;
            MeasurementItem.MeasurementId = measurement.MeasurementId;
            MeasurementItem.AccessLevel = measurement.AccessLevel;
            MeasurementItem.Author = measurement.Author;
            MeasurementItem.CreatedDate = measurement.CreatedDate;
            MeasurementItem.Date = measurement.Date;
            MeasurementItem.Height = measurement.Height;
            MeasurementItem.Weight = measurement.Weight;
            MeasurementItem.Circumference = measurement.Circumference;
            MeasurementItem.HairColor = measurement.HairColor;
            MeasurementItem.EyeColor = measurement.EyeColor;

            IsCurrentUserProgenyAdmin = isAdmin;
        }
    }
}
