using Microsoft.AspNetCore.Mvc.Rendering;
using System;
using System.Collections.Generic;
using KinaUna.Data.Models;

namespace KinaUnaWeb.Models.ItemViewModels
{
    public class VideoViewModel : BaseViewModel
    {
        public int VideoId { get; init; }
        public string VideoLink { get; init; }
        public string ThumbLink { get; init; }
        public DateTime? VideoTime { get; set; }
        public int? VideoRotation { get; init; }
        public int VideoNumber { get; init; }
        public int ProgenyId { get; init; }
        public Progeny Progeny { get; set; }
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
        public int VideoType { get; init; }
        public string Tags { get; init; }
        public string TagFilter { get; init; }
        public string TagsList { get; init; }
        public string DurationHours { get; init; }
        public string DurationMinutes { get; init; }
        public string DurationSeconds { get; init; }
        public int SortBy { get; init; }
        public string UserId { get; init; }
        public TimeSpan? Duration { get; init; }

        public string Location { get; init; }
        public string Longtitude { get; init; }
        public string Latitude { get; init; }
        public string Altitude { get; init; }
        public List<SelectListItem> LocationsList { get; init; }
        public List<Location> ProgenyLocations { get; init; }
        public int VideoCount { get; init; }
        public int PrevVideo { get; init; }
        public int NextVideo { get; init; }

        public string VidTime { get; init; }
        public bool VidTimeValid { get; init; }
        public string VidYears { get; init; }
        public string VidMonths { get; init; }
        public string[] VidWeeks { get; init; }
        public string VidDays { get; init; }
        public string VidHours { get; init; }
        public string VidMinutes { get; init; }

        public VideoViewModel()
        {
            AccessLevelList aclList = new();
            AccessLevelListEn = aclList.AccessLevelListEn;
            AccessLevelListDa = aclList.AccessLevelListDa;
            AccessLevelListDe = aclList.AccessLevelListDe;
        }
    }
}
