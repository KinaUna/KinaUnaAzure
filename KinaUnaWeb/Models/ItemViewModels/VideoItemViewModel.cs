using Microsoft.AspNetCore.Mvc.Rendering;
using System;
using System.Collections.Generic;

namespace KinaUnaWeb.Models.ItemViewModels
{
    public class VideoItemViewModel : BaseItemsViewModel
    {
        public Video Video { get; set; } = new();
        public int CommentThreadNumber { get; set; }
        public List<Comment> CommentsList { get; set; }
        public int CommentsCount { get; set; }
        public string TagFilter { get; set; }
        public int SortBy { get; set; }
        
        public List<SelectListItem> LocationsList { get; set; }
        public List<Location> ProgenyLocations { get; set; }
        public int VideoCount { get; set; }
        public int PrevVideo { get; set; }
        public int NextVideo { get; set; }

        public string VidTime { get; set; }
        public bool VidTimeValid { get; set; }
        public List<string> VidYearsDataList { get; set; }
        public string VidMonths { get; set; }
        public string[] VidWeeks { get; set; }
        public string VidDays { get; set; }
        public string VidHours { get; set; }
        public string VidMinutes { get; set; }
        public bool PartialView { get; set; }
        public string HereMapsApiKey { get; init; } = "";
        public int VideoNumber { get; internal set; }

        /// <summary>
        /// Parameterless constructor. Needed for initialization of the view model when objects are created in Razor views/passed as parameters in POST methods.
        /// </summary>
        public VideoItemViewModel()
        {
            ProgenyList = [];
        }

        public VideoItemViewModel(BaseItemsViewModel baseItemsViewModel)
        {
            SetBaseProperties(baseItemsViewModel);
        }

        public void SetPropertiesFromVideoViewModel(VideoViewModel videoViewModel)
        {
            Video.VideoId = videoViewModel.VideoId;
            Video.VideoType = videoViewModel.VideoType;
            Video.VideoTime = videoViewModel.VideoTime;
            Video.ProgenyId = videoViewModel.ProgenyId;
            Video.Progeny = CurrentProgeny;
            Video.Owners = videoViewModel.Owners;
            Video.VideoLink = videoViewModel.VideoLink;
            Video.ThumbLink = videoViewModel.ThumbLink;
            Video.Author = videoViewModel.Author;
            CommentThreadNumber = Video.CommentThreadNumber = videoViewModel.CommentThreadNumber;
            CommentsList = Video.Comments = videoViewModel.CommentsList ?? [];
            Tags = Video.Tags = videoViewModel.Tags;
            TagsList = videoViewModel.TagsList;
            Video.Location = videoViewModel.Location;
            Video.Latitude = videoViewModel.Latitude;
            Video.Longtitude = videoViewModel.Longtitude;
            Video.Altitude = videoViewModel.Altitude;
            Video.VideoNumber = videoViewModel.VideoNumber;
            VideoCount = videoViewModel.VideoCount;
            PrevVideo = videoViewModel.PrevVideo;
            NextVideo = videoViewModel.NextVideo;
            Video.ItemPerMission = videoViewModel.ItemPerMission;
            CommentsList = videoViewModel.CommentsList;
            CommentsCount = CommentsList?.Count ?? 0;
            TagFilter = videoViewModel.TagFilter;
            SortBy = videoViewModel.SortBy;
            
            if (videoViewModel.Duration != null)
            {
                Video.DurationHours = videoViewModel.Duration.Value.Hours.ToString();
                Video.DurationMinutes = videoViewModel.Duration.Value.Minutes.ToString();
                Video.DurationSeconds = videoViewModel.Duration.Value.Seconds.ToString();
            }
            if (Video.VideoTime != null && CurrentProgeny.BirthDay.HasValue)
            {
                PictureTime picTime = new(CurrentProgeny.BirthDay.Value,
                    TimeZoneInfo.ConvertTimeToUtc(Video.VideoTime.Value, TimeZoneInfo.FindSystemTimeZoneById(CurrentUser.Timezone)),
                    TimeZoneInfo.FindSystemTimeZoneById(CurrentProgeny.TimeZone));
                VidTimeValid = true;
                VidTime = Video.VideoTime.Value.ToString("dd MMMM yyyy HH:mm"); // Todo: Replace string format with global constant or user defined value
                VidYearsDataList = picTime.CalcYears();
                VidMonths = picTime.CalcMonths();
                VidWeeks = picTime.CalcWeeks();
                VidDays = picTime.CalcDays();
                VidHours = picTime.CalcHours();
                VidMinutes = picTime.CalcMinutes();
            }
            else
            {
                VidTimeValid = false;
                VidTime = "";
            }
        }
        
        public void SetPropertiesFromVideoItem(Video video)
        {
            Video.ProgenyId = video.ProgenyId;
            Video.VideoId = video.VideoId;
            Video.ThumbLink = video.ThumbLink;
            Video.VideoTime = video.VideoTime;
            Video.ItemPerMission = video.ItemPerMission;
            if (video.VideoTime.HasValue)
            {
                Video.VideoTime = TimeZoneInfo.ConvertTimeFromUtc(video.VideoTime.Value, TimeZoneInfo.FindSystemTimeZoneById(CurrentUser.Timezone));
            }
        }

        public void SetProgenyList()
        {
            Video.ProgenyId = CurrentProgenyId;
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
