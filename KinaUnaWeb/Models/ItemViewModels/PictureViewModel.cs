using System;
using System.Collections.Generic;
using KinaUna.Data.Models;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace KinaUnaWeb.Models.ItemViewModels
{
    public class PictureViewModel: BaseViewModel
    {
        public int PictureId { get; init; }

        public string PictureLink { get; set; }
        public DateTime? PictureTime { get; set; }
        public int? PictureRotation { get; init; }
        public int PictureWidth { get; init; }
        public int PictureHeight { get; init; }

        public int ProgenyId { get; init; }
        public Progeny Progeny { get; init; }
        public string Owners { get; init; } // Comma separated list of emails.
        public int AccessLevel { get; init; } // 0 = Hidden/Parents only, 1=Family, 2= Friends, 3=DefaultUSers, 4= public.
        public string Author { get; init; }
        public List<SelectListItem> AccessLevelListEn { get; init; }
        public List<SelectListItem> AccessLevelListDa { get; init; }
        public List<SelectListItem> AccessLevelListDe { get; init; }
        public bool IsAdmin { get; init; }
        public int CommentThreadNumber { get; init; }
        public List<Comment> CommentsList { get; init; }
        public int CommentsCount { get; set; }
        public string Tags { get; init; }
        public string TagFilter { get; init; }
        public string TagsList { get; init; }
        public string Location { get; init; }
        public string Longtitude { get; init; } // Todo: Fix typo in property name.
        public string Latitude { get; init; }
        public string Altitude { get; init; }
        public List<SelectListItem> LocationsList { get; init; }
        public List<Location> ProgenyLocations { get; init; }
        public int PictureNumber { get; init; }
        public int PictureCount { get; init; }
        public int PrevPicture { get; init; }
        public int NextPicture { get; init; }
        public string PicTime { get; init; }
        public bool PicTimeValid { get; init; }
        public string PicYears { get; init; }
        public string PicMonths { get; init; }
        public string[] PicWeeks { get; init; }
        public string PicDays { get; init; }
        public string PicHours { get; init; }
        public string PicMinutes { get; init; }
        public int SortBy { get; init; }
        public string UserId { get; init; }
        public PictureViewModel()
        {
            AccessLevelList aclList = new();
            AccessLevelListEn = aclList.AccessLevelListEn;
            AccessLevelListDa = aclList.AccessLevelListDa;
            AccessLevelListDe = aclList.AccessLevelListDe;

        }
    }
}
