using System.Collections.Generic;
using System.Linq;
using KinaUna.Data.Models;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace KinaUnaWeb.Models.ItemViewModels
{
    public class VaccinationViewModel: BaseItemsViewModel
    {
        public List<SelectListItem> ProgenyList { get; set; }
        public List<Vaccination> VaccinationList { get; set; } = [];
        public List<SelectListItem> AccessLevelListEn { get; set; }
        public List<SelectListItem> AccessLevelListDa { get; set; }
        public List<SelectListItem> AccessLevelListDe { get; set; }
        
        public Vaccination VaccinationItem { get; set; } = new();

        public int VaccinationId { get; set; }
        public VaccinationViewModel()
        {
            ProgenyList = [];
            AccessLevelList aclList = new();
            AccessLevelListEn = aclList.AccessLevelListEn;
            AccessLevelListDa = aclList.AccessLevelListDa;
            AccessLevelListDe = aclList.AccessLevelListDe;
        }

        public VaccinationViewModel(BaseItemsViewModel baseItemsViewModel)
        {
            SetBaseProperties(baseItemsViewModel);
        }

        public void SetProgenyList()
        {
            VaccinationItem.ProgenyId = CurrentProgenyId;
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

            AccessLevelListEn[VaccinationItem.AccessLevel].Selected = true;
            AccessLevelListDa[VaccinationItem.AccessLevel].Selected = true;
            AccessLevelListDe[VaccinationItem.AccessLevel].Selected = true;

            if (LanguageId == 2)
            {
                AccessLevelListEn = AccessLevelListDe;
            }

            if (LanguageId == 3)
            {
                AccessLevelListEn = AccessLevelListDa;
            }
        }

        public void SetVaccinationsList(List<Vaccination> vaccinationsList)
        {
            if (vaccinationsList.Count != 0)
            {
                foreach (Vaccination vaccination in vaccinationsList)
                {
                    if (vaccination.AccessLevel >= CurrentAccessLevel)
                    {
                        VaccinationList.Add(vaccination);
                    }
                }

                VaccinationList = [.. VaccinationList.OrderBy(v => v.VaccinationDate)];
            }
        }

        public void SetPropertiesFromVaccinationItem(Vaccination vaccination)
        {
            VaccinationItem.VaccinationId = vaccination.VaccinationId;
            VaccinationItem.ProgenyId = vaccination.ProgenyId;
            VaccinationItem.AccessLevel = vaccination.AccessLevel;
            VaccinationItem.Author = vaccination.Author;
            VaccinationItem.VaccinationName = vaccination.VaccinationName;
            VaccinationItem.VaccinationDate = vaccination.VaccinationDate;
            VaccinationItem.VaccinationDescription = vaccination.VaccinationDescription;
            VaccinationItem.Notes = vaccination.Notes;
        }
    }
}
