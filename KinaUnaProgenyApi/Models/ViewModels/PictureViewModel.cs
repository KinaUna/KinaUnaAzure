using System;
using System.Collections.Generic;
using KinaUna.Data.Models;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace KinaUnaProgenyApi.Models.ViewModels
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
        public string TagsList { get; set; }
        public string TagFilter { get; set; }
        public string Location { get; set; }
        public string Longtitude { get; set; } // Todo: Fix typo in variable name.
        public string Latitude { get; set; }
        public string Altitude { get; set; }
        public List<SelectListItem> LocationsList { get; set; }
        public List<Location> ProgenyLocations { get; set; }
        public int PictureNumber { get; set; }
        public int PictureCount { get; set; }
        public int PrevPicture { get; set; }
        public int NextPicture { get; set; }
        public PictureViewModel()
        {
            AccessLevelList aclList = new();
            AccessLevelListEn = aclList.AccessLevelListEn;
            AccessLevelListDa = aclList.AccessLevelListDa;
            AccessLevelListDe = aclList.AccessLevelListDe;
        }

        public void SetPicturePropertiesFromPictureItem(Picture picture)
        {
            PictureId = picture.PictureId;
            PictureTime = picture.PictureTime;
            ProgenyId = picture.ProgenyId;
            Owners = picture.Owners;
            PictureLink = picture.PictureLink1200;
            AccessLevel = picture.AccessLevel;
            Author = picture.Author;
            AccessLevelListEn[picture.AccessLevel].Selected = true;
            AccessLevelListDa[picture.AccessLevel].Selected = true;
            AccessLevelListDe[picture.AccessLevel].Selected = true;
            Tags = picture.Tags;
            Location = picture.Location;
            Latitude = picture.Latitude;
            Longtitude = picture.Longtitude;
            Altitude = picture.Altitude;
            CommentThreadNumber = picture.CommentThreadNumber;
        }

        public void SetTagsList(List<string> tagsList)
        {
            string tagItems = "[";

            if (tagsList.Count != 0)
            {
                foreach (string tagstring in tagsList)
                {
                    tagItems = tagItems + "'" + tagstring + "',";
                }

                tagItems = tagItems[..^1];

            }

            tagItems += "]";

            TagsList = tagItems;
        }
    }
}

