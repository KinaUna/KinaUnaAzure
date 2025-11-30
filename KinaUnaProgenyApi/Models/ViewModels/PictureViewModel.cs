using KinaUna.Data.Models;
using KinaUna.Data.Models.AccessManagement;
using System;
using System.Collections.Generic;

namespace KinaUnaProgenyApi.Models.ViewModels
{
    public class PictureViewModel
    {
        public int PictureId { get; set; }

        public string PictureLink { get; set; }
        public DateTime? PictureTime { get; set; }
        
        public int ProgenyId { get; set; }
        public string Owners { get; set; } // Comma separated list of emails.
        public string Author { get; set; }
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
        public int PictureNumber { get; set; }
        public int PictureCount { get; set; }
        public int PrevPicture { get; set; }
        public int NextPicture { get; set; }
        public TimelineItemPermission ItemPerMission { get; set; }

        public void SetPicturePropertiesFromPictureItem(Picture picture)
        {
            PictureId = picture.PictureId;
            PictureTime = picture.PictureTime;
            ProgenyId = picture.ProgenyId;
            Owners = picture.Owners;
            PictureLink = picture.PictureLink1200;
            Author = picture.Author;
            Tags = picture.Tags;
            Location = picture.Location;
            Latitude = picture.Latitude;
            Longtitude = picture.Longtitude;
            Altitude = picture.Altitude;
            CommentThreadNumber = picture.CommentThreadNumber;
            ItemPerMission = picture.ItemPerMission;
        }

        public void SetTagsList(List<string> tagsList)
        {
            string tagItems = "[";

            if (tagsList.Count != 0)
            {
                foreach (string tagString in tagsList)
                {
                    tagItems = tagItems + "'" + tagString + "',";
                }

                tagItems = tagItems[..^1];

            }

            tagItems += "]";

            TagsList = tagItems;
        }
    }
}

