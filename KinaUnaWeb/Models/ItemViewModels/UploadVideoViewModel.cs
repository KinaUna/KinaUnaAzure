using KinaUna.Data;
using KinaUna.Data.Models.DTOs;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Rendering;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;

namespace KinaUnaWeb.Models.ItemViewModels
{
    public class UploadVideoViewModel: BaseItemsViewModel
    {
        public Video Video { get; set; } = new();
        public IFormFile File { get; init; }
        public string FileName { get; init; }
        public string FileLink { get; init; }
        public List<SelectListItem> LocationsList { get; set; } = [];
        public List<Location> ProgenyLocations { get; set; } = [];

        /// <summary>
        /// Parameterless constructor. Needed for initialization of the view model when objects are created in Razor views/passed as parameters in POST methods.
        /// </summary>
        public UploadVideoViewModel()
        {
            ProgenyList = [];
            
        }

        public UploadVideoViewModel(BaseItemsViewModel baseItemsViewModel)
        {
            Video = new Video();
            SetBaseProperties(baseItemsViewModel);
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

        public Video CreateVideo(bool parseDuration)
        {
            Video video = new()
            {
                ProgenyId = Video.ProgenyId,
                Author = CurrentUser.UserId,
                Owners = CurrentUser.UserEmail,
                ThumbLink = Constants.WebAppUrl + "/videodb/moviethumb.png",
                VideoTime = DateTime.UtcNow,
                ItemPermissionsDtoList = string.IsNullOrWhiteSpace(ItemPermissionsListAsString) ? [] : JsonSerializer.Deserialize<List<ItemPermissionDto>>(ItemPermissionsListAsString, JsonSerializerOptions.Web)
            };
            if (Video.VideoTime != null)
            {
                video.VideoTime = TimeZoneInfo.ConvertTimeToUtc(Video.VideoTime.Value, TimeZoneInfo.FindSystemTimeZoneById(CurrentUser.Timezone));
            }
            video.VideoType = 2; // Todo: Replace with Enum or constant
            video.Duration = Video.Duration;

            if (parseDuration)
            {
                bool durationHoursParsed = int.TryParse(Video.DurationHours, out int durHours);
                bool durationMinutesParsed = int.TryParse(Video.DurationMinutes, out int durMins);
                bool durationSecondsParsed = int.TryParse(Video.DurationSeconds, out int durSecs);
                if (durationHoursParsed && durationMinutesParsed && durationSecondsParsed)
                {
                    video.Duration = new TimeSpan(durHours, durMins, durSecs);
                }
            }
            
            if (FileLink.Contains("<iframe"))
            {
                string[] vLink1 = FileLink.Split('"');
                foreach (string str in vLink1)
                {
                    if (str.Contains("://"))
                    {
                        video.VideoLink = str;
                    }
                }
            }

            if (FileLink.Contains("watch?v"))
            {
                string str = FileLink.Split('=').Last();
                video.VideoLink = "https://www.youtube.com/embed/" + str;
            }

            if (FileLink.StartsWith("https://youtu.be"))
            {
                string str = FileLink.Split('/').Last();
                video.VideoLink = "https://www.youtube.com/embed/" + str;
            }

            if (FileLink.Contains("youtube.com/shorts/") || FileLink.Contains("youtu.be/shorts/"))
            {
                string str = FileLink.Split('/').Last();
                video.VideoLink = "https://www.youtube.com/embed/" + str;
            }

            video.ThumbLink = "https://i.ytimg.com/vi/" + video.VideoLink.Split("/").Last() + "/hqdefault.jpg";

            video.Location = Video.Location;
            video.Latitude = Video.Latitude;
            video.Longtitude = Video.Longtitude;
            video.Altitude = Video.Altitude;
            video.Tags = Video.Tags;

            return video;
        }
    }
}
