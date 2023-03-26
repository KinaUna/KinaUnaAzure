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
        public List<IFormFile> Files { get; set; }
        public List<SelectListItem> ProgenyList { get; set; }
        public List<string> FileNames { get; set; }
        public List<string> FileLinks { get; set; }
        [Required]
        public List<SelectListItem> AccessLevelListEn { get; set; }
        public List<SelectListItem> AccessLevelListDa { get; set; }
        public List<SelectListItem> AccessLevelListDe { get; set; }
        public double Longtitude1 { get; set; }
        public double Latitude1 { get; set; }
        public string Altitude { get; set; }
        public Picture Picture { get; set; } = new();

        public UploadPictureViewModel()
        {
            ProgenyList = new List<SelectListItem>();
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
