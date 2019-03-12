using System;
using System.Collections.Generic;
using KinaUna.Data.Models;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace KinaUnaWeb.Models.ItemViewModels
{
    public class VaccinationViewModel
    {
        public int VaccinationId { get; set; }
        public string VaccinationName { get; set; }
        public string VaccinationDescription { get; set; }
        public DateTime VaccinationDate { get; set; }
        public string Notes { get; set; }
        public int ProgenyId { get; set; }
        public int AccessLevel { get; set; }
        public string Author { get; set; }
        public List<SelectListItem> ProgenyList { get; set; }
        public List<Vaccination> VaccinationList { get; set; }
        public List<SelectListItem> AccessLevelListEn { get; set; }
        public List<SelectListItem> AccessLevelListDa { get; set; }
        public List<SelectListItem> AccessLevelListDe { get; set; }
        public Progeny Progeny { get; set; }
        public bool IsAdmin { get; set; }

        public VaccinationViewModel()
        {
            ProgenyList = new List<SelectListItem>();
            AccessLevelList aclList = new AccessLevelList();
            AccessLevelListEn = aclList.AccessLevelListEn;
            AccessLevelListDa = aclList.AccessLevelListDa;
            AccessLevelListDe = aclList.AccessLevelListDe;
        }
    }
}
