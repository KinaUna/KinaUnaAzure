using KinaUna.Data.Models.AccessManagement;
using Microsoft.AspNetCore.Mvc.Rendering;
using System;
using System.Collections.Generic;

namespace KinaUnaWeb.Models.ItemViewModels
{
    public class PictureViewModel: BaseViewModel
    {
        public int PictureId { get; init; }

        public string PictureLink { get; set; } = string.Empty;
        public DateTime? PictureTime { get; set; }
        public int? PictureRotation { get; init; }
        public int PictureWidth { get; init; }
        public int PictureHeight { get; init; }

        public int ProgenyId { get; init; }
        public Progeny Progeny { get; set; }
        public string Owners { get; init; } // Comma separated list of emails.
        public string Author { get; init; }
        public int CommentThreadNumber { get; init; }
        public List<Comment> CommentsList { get; init; } = [];
        public int CommentsCount { get; set; }
        public string Tags { get; init; } = string.Empty;
        public string TagFilter { get; init; } = string.Empty;
        public string TagsList { get; init; } = string.Empty;
        public string Location { get; init; } = string.Empty;
        public string Longtitude { get; init; } = string.Empty; // Todo: Fix typo in property name.
        public string Latitude { get; init; } = string.Empty;
        public string Altitude { get; init; } = string.Empty;
        public List<SelectListItem> LocationsList { get; init; } = [];
        public List<Location> ProgenyLocations { get; init; } = [];
        public int PictureNumber { get; init; }
        public int PictureCount { get; init; }
        public int PrevPicture { get; init; }
        public int NextPicture { get; init; }
        public string PicTime { get; init; } = string.Empty;
        public bool PicTimeValid { get; init; }
        public string PicYears { get; init; } = string.Empty;
        public string PicMonths { get; init; } = string.Empty;
        public string[] PicWeeks { get; init; } = [];
        public string PicDays { get; init; } = string.Empty;
        public string PicHours { get; init; } = string.Empty;
        public string PicMinutes { get; init; } = string.Empty;
        public int SortBy { get; init; }
        public string UserId { get; init; } = string.Empty;
        public TimelineItemPermission ItemPerMission { get; set; }
    }
}
