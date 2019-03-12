using System;
using System.Collections.Generic;
using KinaUna.Data.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace KinaUnaWeb.Models.ItemViewModels
{
    public class UploadVideoViewModel
    {
        public UserInfo Userinfo { get; set; }
        public IFormFile File { get; set; }
        public List<SelectListItem> ProgenyList { get; set; }
        public int ProgenyId { get; set; }
        public string FileName { get; set; }
        public string FileLink { get; set; }
        public string ThumbLink { get; set; }
        public int AccessLevel { get; set; }
        public string Author { get; set; }
        public List<SelectListItem> AccessLevelListEn { get; set; }
        public List<SelectListItem> AccessLevelListDa { get; set; }
        public List<SelectListItem> AccessLevelListDe { get; set; }
        public string Owners { get; set; }
        public int VideoType { get; set; }
        public DateTime? VideoTime { get; set; }
        public string DurationHours { get; set; }
        public string DurationMinutes { get; set; }
        public string DurationSeconds { get; set; }

        public UploadVideoViewModel()
        {
            ProgenyList = new List<SelectListItem>();

            AccessLevelList aclList = new AccessLevelList();
            AccessLevelListEn = aclList.AccessLevelListEn;
            AccessLevelListDa = aclList.AccessLevelListDa;
            AccessLevelListDe = aclList.AccessLevelListDe;
        }
    }
}
