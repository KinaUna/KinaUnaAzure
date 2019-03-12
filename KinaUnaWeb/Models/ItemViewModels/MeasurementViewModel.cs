using System;
using System.Collections.Generic;
using KinaUna.Data.Models;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace KinaUnaWeb.Models.ItemViewModels
{
    public class MeasurementViewModel
    {
        public List<SelectListItem> ProgenyList { get; set; }
        public List<Measurement> MeasurementsList { get; set; }
        public List<SelectListItem> AccessLevelListEn { get; set; }
        public List<SelectListItem> AccessLevelListDa { get; set; }
        public List<SelectListItem> AccessLevelListDe { get; set; }
        public int MeasurementId { get; set; }
        public int ProgenyId { get; set; }
        public double Weight { get; set; }
        public double Height { get; set; }
        public double Circumference { get; set; }
        public string EyeColor { get; set; }
        public string HairColor { get; set; }
        public DateTime Date { get; set; }
        public DateTime CreatedDate { get; set; }
        public int AccessLevel { get; set; }
        public string Author { get; set; }
        public Progeny Progeny { get; set; }
        public bool IsAdmin { get; set; }

        public MeasurementViewModel()
        {
            ProgenyList = new List<SelectListItem>();
            AccessLevelList aclList = new AccessLevelList();
            AccessLevelListEn = aclList.AccessLevelListEn;
            AccessLevelListDa = aclList.AccessLevelListDa;
            AccessLevelListDe = aclList.AccessLevelListDe;
        }
    }
}
