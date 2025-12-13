using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace KinaUnaWeb.Models.ItemViewModels
{
    public class UploadPictureViewModel: BaseItemsViewModel
    {
        [Required]
        public List<IFormFile> Files { get; init; }
        public List<string> FileNames { get; init; }
        public List<string> FileLinks { get; init; }
        [Required]
        public double Longtitude1 { get; init; } // Todo: Fix typo in property name
        public double Latitude1 { get; init; }
        public string Altitude { get; init; }
        public Picture Picture { get; set; } = new();

        /// <summary>
        /// Parameterless constructor. Needed for initialization of the view model when objects are created in Razor views/passed as parameters in POST methods.
        /// </summary>
        public UploadPictureViewModel()
        {
            ProgenyList = [];
        }

        public UploadPictureViewModel(BaseItemsViewModel baseItemsViewModel)
        {
            SetBaseProperties(baseItemsViewModel);
            
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
