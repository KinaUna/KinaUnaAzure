using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace KinaUnaWeb.Models.ItemViewModels
{
    public class VaccinationViewModel: BaseItemsViewModel
    {
        public List<Vaccination> VaccinationList { get; set; } = [];
        public Vaccination VaccinationItem { get; set; } = new();

        public int VaccinationId { get; set; }
        public VaccinationViewModel()
        {
            ProgenyList = [];
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
        
        public void SetVaccinationsList(List<Vaccination> vaccinationsList)
        {
            if (vaccinationsList.Count != 0)
            {
                VaccinationList = [.. VaccinationList.OrderBy(v => v.VaccinationDate)];
            }
        }

        public void SetPropertiesFromVaccinationItem(Vaccination vaccination)
        {
            VaccinationItem.VaccinationId = vaccination.VaccinationId;
            VaccinationItem.ProgenyId = vaccination.ProgenyId;
            VaccinationItem.Author = vaccination.Author;
            VaccinationItem.VaccinationName = vaccination.VaccinationName;
            VaccinationItem.VaccinationDate = vaccination.VaccinationDate;
            VaccinationItem.VaccinationDescription = vaccination.VaccinationDescription;
            VaccinationItem.Notes = vaccination.Notes;
            VaccinationItem.ItemPerMission = vaccination.ItemPerMission;
        }
    }
}
