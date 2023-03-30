using System;
using System.Collections.Generic;
using KinaUna.Data.Models;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace KinaUnaWeb.Models.ItemViewModels
{
    public class PictureItemViewModel: BaseItemsViewModel
    {
        public Picture Picture { get; set; } = new();
        
        public List<SelectListItem> AccessLevelListEn { get; set; }
        public List<SelectListItem> AccessLevelListDa { get; set; }
        public List<SelectListItem> AccessLevelListDe { get; set; }
        public int CommentThreadNumber { get; set; }
        public List<Comment> CommentsList { get; set; }
        public int CommentsCount { get; set; }
        public string TagFilter { get; set; }
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

        public string HereMapsApiKey { get; set; } = "";

        public PictureItemViewModel()
        {
            AccessLevelList aclList = new();
            AccessLevelListEn = aclList.AccessLevelListEn;
            AccessLevelListDa = aclList.AccessLevelListDa;
            AccessLevelListDe = aclList.AccessLevelListDe;

        }

        public PictureItemViewModel(BaseItemsViewModel baseItemsViewModel)
        {
            SetBaseProperties(baseItemsViewModel);
        }

        public void SetPropertiesFromPictureViewModel(PictureViewModel pictureViewModel)
        {
            Picture.PictureId = pictureViewModel.PictureId;
            Picture.ProgenyId = pictureViewModel.ProgenyId;
            Picture.Progeny = CurrentProgeny;
            Picture.AccessLevel = pictureViewModel.AccessLevel;
            Picture.Author = pictureViewModel.Author;
            CommentThreadNumber = Picture.CommentThreadNumber = pictureViewModel.CommentThreadNumber;
            CommentsList = Picture.Comments = pictureViewModel.CommentsList ?? new List<Comment>();
            Picture.Location = pictureViewModel.Location;
            Picture.Latitude = pictureViewModel.Latitude;
            Picture.Longtitude = pictureViewModel.Longtitude;
            Picture.Altitude = pictureViewModel.Altitude;
            Picture.Owners = pictureViewModel.Owners;
            Picture.PictureHeight = pictureViewModel.PictureHeight;
            Picture.PictureWidth = pictureViewModel.PictureWidth;
            Picture.PictureLink = pictureViewModel.PictureLink;
            Picture.PictureRotation = pictureViewModel.PictureRotation;
            PictureNumber = Picture.PictureNumber = pictureViewModel.PictureNumber;
            Picture.PictureTime = pictureViewModel.PictureTime;
            Tags = Picture.Tags = pictureViewModel.Tags;
            
            CommentsCount = CommentsList?.Count ?? 0;
            TagFilter = pictureViewModel.TagFilter;
            TagsList = pictureViewModel.TagsList;
            PictureCount = pictureViewModel.PictureCount;
            PrevPicture = pictureViewModel.PrevPicture;
            NextPicture = pictureViewModel.NextPicture;

            if (Picture.PictureTime != null && CurrentProgeny.BirthDay.HasValue)
            {
                PictureTime picTime = new(CurrentProgeny.BirthDay.Value,
                    TimeZoneInfo.ConvertTimeToUtc(Picture.PictureTime.Value, TimeZoneInfo.FindSystemTimeZoneById(CurrentUser.Timezone)),
                    TimeZoneInfo.FindSystemTimeZoneById(CurrentProgeny.TimeZone));
                PicTimeValid = true;
                PicTime = Picture.PictureTime.Value.ToString("dd MMMM yyyy HH:mm"); // Todo: Replace format string with global constant or user defined value
                PicYears = picTime.CalcYears();
                PicMonths = picTime.CalcMonths();
                PicWeeks = picTime.CalcWeeks();
                PicDays = picTime.CalcDays();
                PicHours = picTime.CalcHours();
                PicMinutes = picTime.CalcMinutes();
            }
            else
            {
                PicTimeValid = false;
                PicTime = "";
            }
        }

        public void SetPropertiesFromPictureItem(Picture picture)
        {
            Picture.ProgenyId = picture.ProgenyId;
            Picture.PictureId = picture.PictureId;
            Picture.PictureLink = picture.PictureLink600;
            Picture.PictureTime = picture.PictureTime;
            if (Picture.PictureTime.HasValue)
            {
                Picture.PictureTime = TimeZoneInfo.ConvertTimeFromUtc(Picture.PictureTime.Value, TimeZoneInfo.FindSystemTimeZoneById(CurrentUser.Timezone));
            }
        }
        public void SetAccessLevelList()
        {
            AccessLevelList accessLevelList = new();
            AccessLevelListEn = accessLevelList.AccessLevelListEn;
            AccessLevelListDa = accessLevelList.AccessLevelListDa;
            AccessLevelListDe = accessLevelList.AccessLevelListDe;

            AccessLevelListEn[Picture.AccessLevel].Selected = true;
            AccessLevelListDa[Picture.AccessLevel].Selected = true;
            AccessLevelListDe[Picture.AccessLevel].Selected = true;

            if (LanguageId == 2)
            {
                AccessLevelListEn = AccessLevelListDe;
            }

            if (LanguageId == 3)
            {
                AccessLevelListEn = AccessLevelListDa;
            }
        }
    }
}
