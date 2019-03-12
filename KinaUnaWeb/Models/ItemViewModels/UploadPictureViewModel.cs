using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using KinaUna.Data.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace KinaUnaWeb.Models.ItemViewModels
{
    public class UploadPictureViewModel
    {
        [Required]
        public List<IFormFile> Files { get; set; }
        public UserInfo Userinfo { get; set; }
        public List<SelectListItem> ProgenyList { get; set; }
        [Required]
        public int ProgenyId { get; set; }
        public List<string> FileNames { get; set; }
        public List<string> FileLinks { get; set; }
        [Required]
        public int AccessLevel { get; set; }
        public string Author { get; set; }
        public List<SelectListItem> AccessLevelListEn { get; set; }
        public List<SelectListItem> AccessLevelListDa { get; set; }
        public List<SelectListItem> AccessLevelListDe { get; set; }
        public string Owners { get; set; }
        public string Tags { get; set; }
        public double Longtitude1 { get; set; }
        public double Latitude1 { get; set; }
        public string Altitude { get; set; }
        public string Longtitude { get; set; }
        public string Latitude { get; set; }
        public string Location { get; set; }
        public UploadPictureViewModel()
        {
            // List<IFormFile> files = new List<IFormFile>();

            ProgenyList = new List<SelectListItem>();

            AccessLevelList aclList = new AccessLevelList();
            AccessLevelListEn = aclList.AccessLevelListEn;
            AccessLevelListDa = aclList.AccessLevelListDa;
            AccessLevelListDe = aclList.AccessLevelListDe;
        }
    }
}
