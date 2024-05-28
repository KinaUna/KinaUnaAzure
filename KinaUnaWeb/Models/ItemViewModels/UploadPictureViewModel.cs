using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using KinaUna.Data.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace KinaUnaWeb.Models.ItemViewModels
{
    public class UploadPictureViewModel: BaseItemsViewModel
    {
        [Required]
        public List<IFormFile> Files { get; init; }
        public List<SelectListItem> ProgenyList { get; set; }
        public List<string> FileNames { get; init; }
        public List<string> FileLinks { get; init; }
        [Required]
        public List<SelectListItem> AccessLevelListEn { get; set; }
        public List<SelectListItem> AccessLevelListDa { get; set; }
        public List<SelectListItem> AccessLevelListDe { get; set; }
        public double Longtitude1 { get; init; } // Todo: Fix typo in property name
        public double Latitude1 { get; init; }
        public string Altitude { get; init; }
        public Picture Picture { get; init; } = new();

        public UploadPictureViewModel()
        {
            ProgenyList = [];
            AccessLevelList aclList = new();
            AccessLevelListEn = aclList.AccessLevelListEn;
            AccessLevelListDa = aclList.AccessLevelListDa;
            AccessLevelListDe = aclList.AccessLevelListDe;
        }

        public UploadPictureViewModel(BaseItemsViewModel baseItemsViewModel)
        {
            SetBaseProperties(baseItemsViewModel);
            
        }

        public void SetAccessLevelList()
        {
            AccessLevelList accessLevelList = new();
            AccessLevelListEn = accessLevelList.AccessLevelListEn;
            AccessLevelListDa = accessLevelList.AccessLevelListDa;
            AccessLevelListDe = accessLevelList.AccessLevelListDe;

            AccessLevelListEn[Picture.AccessLevel].Selected = true;
            AccessLevelListDa[Picture.AccessLevel].Selected = true;
            AccessLevelListDe[Picture.AccessLevel].Selected = true;

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
            Picture.ProgenyId = CurrentProgenyId;
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
    }
}
