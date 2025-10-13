using Microsoft.AspNetCore.Mvc.Rendering;
using System;
using System.Collections.Generic;

namespace KinaUnaWeb.Models.ItemViewModels
{
    public class VideoViewModel : BaseViewModel
    {
        public int VideoId { get; init; }
        public string VideoLink { get; init; } = string.Empty;  
        public string ThumbLink { get; init; } = string.Empty;
        public DateTime? VideoTime { get; set; }
        public int? VideoRotation { get; init; }
        public int VideoNumber { get; init; }
        public int ProgenyId { get; init; }
        public Progeny Progeny { get; set; }
        public string Owners { get; init; } = string.Empty; // Comma separated list of emails.
        public string Author { get; init; } = string.Empty;
        public int CommentThreadNumber { get; init; }
        public List<Comment> CommentsList { get; init; } = [];
        public int CommentsCount { get; set; }
        public int VideoType { get; init; }
        public string Tags { get; init; } = string.Empty;
        public string TagFilter { get; init; } = string.Empty;
        public string TagsList { get; init; } = string.Empty;
        public string DurationHours { get; init; } = string.Empty;
        public string DurationMinutes { get; init; } = string.Empty;
        public string DurationSeconds { get; init; } = string.Empty;
        public int SortBy { get; init; }
        public string UserId { get; init; } = string.Empty;
        public TimeSpan? Duration { get; init; }

        public string Location { get; init; } = string.Empty;
        public string Longtitude { get; init; } = string.Empty;
        public string Latitude { get; init; } = string.Empty;
        public string Altitude { get; init; } = string.Empty;
        public List<SelectListItem> LocationsList { get; init; } = [];
        public List<Location> ProgenyLocations { get; init; } = [];
        public int VideoCount { get; init; }
        public int PrevVideo { get; init; }
        public int NextVideo { get; init; }

        public string VidTime { get; init; } = string.Empty;
        public bool VidTimeValid { get; init; }
        public string VidYears { get; init; } = string.Empty;
        public string VidMonths { get; init; } = string.Empty;
        public string[] VidWeeks { get; init; } = [];
        public string VidDays { get; init; } = string.Empty;
        public string VidHours { get; init; } = string.Empty;
        public string VidMinutes { get; init; } = string.Empty;
    }
}
