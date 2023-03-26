using System;
using System.Collections.Generic;
using System.Linq;
using KinaUna.Data;
using KinaUna.Data.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace KinaUnaWeb.Models.ItemViewModels
{
    public class UploadVideoViewModel: BaseItemsViewModel
    {
        public Video Video { get; set; } = new();
        public IFormFile File { get; set; }
        public List<SelectListItem> ProgenyList { get; set; }
        public string FileName { get; set; }
        public string FileLink { get; set; }
        public List<SelectListItem> AccessLevelListEn { get; set; }
        public List<SelectListItem> AccessLevelListDa { get; set; }
        public List<SelectListItem> AccessLevelListDe { get; set; }
        
        public UploadVideoViewModel()
        {
            ProgenyList = new List<SelectListItem>();
            
            AccessLevelList aclList = new();
            AccessLevelListEn = aclList.AccessLevelListEn;
            AccessLevelListDa = aclList.AccessLevelListDa;
            AccessLevelListDe = aclList.AccessLevelListDe;
        }

        public UploadVideoViewModel(BaseItemsViewModel baseItemsViewModel)
        {
            Video = new Video();
            SetBaseProperties(baseItemsViewModel);
        }

        public void SetAccessLevelList()
        {
            AccessLevelList accessLevelList = new();
            AccessLevelListEn = accessLevelList.AccessLevelListEn;
            AccessLevelListDa = accessLevelList.AccessLevelListDa;
            AccessLevelListDe = accessLevelList.AccessLevelListDe;

            AccessLevelListEn[Video.AccessLevel].Selected = true;
            AccessLevelListDa[Video.AccessLevel].Selected = true;
            AccessLevelListDe[Video.AccessLevel].Selected = true;

            if (LanguageId == 2)
            {
                AccessLevelListEn = AccessLevelListDe;
            }

            if (LanguageId == 3)
            {
                AccessLevelListEn = AccessLevelListDa;
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

        public Video CreateVideo(bool parseDuration)
        {
            Video video = new()
            {
                ProgenyId = Video.ProgenyId,
                AccessLevel = Video.AccessLevel,
                Author = CurrentUser.UserId,
                Owners = CurrentUser.UserEmail,
                ThumbLink = Constants.WebAppUrl + "/videodb/moviethumb.png",
                VideoTime = DateTime.UtcNow
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

            video.ThumbLink = "https://i.ytimg.com/vi/" + video.VideoLink.Split("/").Last() + "/hqdefault.jpg";

            return video;
        }
    }
}
