using System;
using System.Collections.Generic;
using KinaUna.Data.Models;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace KinaUnaWeb.Models.ItemViewModels
{
    public class PictureViewModel
    {
        public int PictureId { get; set; }

        public string PictureLink { get; set; }
        public DateTime? PictureTime { get; set; }
        public int? PictureRotation { get; set; }
        public int PictureWidth { get; set; }
        public int PictureHeight { get; set; }

        public int ProgenyId { get; set; }
        public Progeny Progeny { get; set; }
        public string Owners { get; set; } // Comma separated list of emails.
        public int AccessLevel { get; set; } // 0 = Hidden/Parents only, 1=Family, 2= Friends, 3=DefaultUSers, 4= public.
        public string Author { get; set; }
        public List<SelectListItem> AccessLevelListEn { get; set; }
        public List<SelectListItem> AccessLevelListDa { get; set; }
        public List<SelectListItem> AccessLevelListDe { get; set; }
        public bool IsAdmin { get; set; }
        public int CommentThreadNumber { get; set; }
        public List<Comment> CommentsList { get; set; }
        public int CommentsCount { get; set; }
        public string Tags { get; set; }
        public string TagFilter { get; set; }
        public string TagsList { get; set; }
        public string Location { get; set; }
        public string Longtitude { get; set; }
        public string Latitude { get; set; }
        public string Altitude { get; set; }
        public List<SelectListItem> LocationsList { get; set; }
        public List<Location> ProgenyLocations { get; set; }
        public int PictureNumber { get; set; }
        public int PictureCount { get; set; }
        public int PrevPicture { get; set; }
        public int NextPicture { get; set; }
        public string PicTime { get; set; }
        public bool PicTimeValid { get; set; }
        public string PicYears { get; set; }
        public string PicMonths { get; set; }
        public string[] PicWeeks { get; set; }
        public string PicDays { get; set; }
        public string PicHours { get; set; }
        public string PicMinutes { get; set; }
        public int SortBy { get; set; }
        public string UserId { get; set; }
        public PictureViewModel()
        {
            AccessLevelList aclList = new AccessLevelList();
            AccessLevelListEn = aclList.AccessLevelListEn;
            AccessLevelListDa = aclList.AccessLevelListDa;
            AccessLevelListDe = aclList.AccessLevelListDe;

        }
    }
}
