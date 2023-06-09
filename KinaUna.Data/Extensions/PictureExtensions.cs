﻿using System;
using KinaUna.Data.Models;

namespace KinaUna.Data.Extensions
{
    /// <summary>
    /// Extensions for Picture data.
    /// </summary>
    public static class PictureExtensions
    {
        /// <summary>
        /// Replaces null strings with empty string.
        /// </summary>
        /// <param name="picture">Picture: The picture object to update.</param>
        
        public static void RemoveNullStrings(this Picture picture)
        {
            if (picture != null)
            {
                picture.Altitude ??= "";

                picture.Tags ??= "";

                picture.Author ??= "";

                picture.Latitude ??= "";

                picture.Location ??= "";

                picture.Longtitude ??= "";

                picture.Owners ??= "";

                picture.PictureLink1200 ??= "";

                picture.PictureLink ??= "";

                picture.PictureLink600 ??= "";

                picture.TimeZone ??= "";
            }
        }

        public static void CopyPropertiesForUpdate(this Picture currentPicture, Picture otherPicture)
        {
            currentPicture.AccessLevel = otherPicture.AccessLevel;
            currentPicture.CommentThreadNumber = otherPicture.CommentThreadNumber;
            currentPicture.TimeZone = otherPicture.TimeZone;
            currentPicture.PictureTime = otherPicture.PictureTime;
            currentPicture.Owners = otherPicture.Owners;
            currentPicture.PictureHeight = otherPicture.PictureHeight;
            currentPicture.PictureWidth = otherPicture.PictureWidth;
            currentPicture.PictureRotation = otherPicture.PictureRotation;
            currentPicture.PictureNumber = otherPicture.PictureNumber;
            
            if (!string.IsNullOrEmpty(otherPicture.Tags))
            {
                currentPicture.Tags = otherPicture.Tags.TrimEnd(',', ' ').TrimStart(',', ' ');
            }

            if (!string.IsNullOrEmpty(otherPicture.Location))
            {
                currentPicture.Location = otherPicture.Location;
            }

            if (!string.IsNullOrEmpty(otherPicture.Longtitude))
            {
                currentPicture.Longtitude = otherPicture.Longtitude.Replace(',', '.');
            }

            if (!string.IsNullOrEmpty(otherPicture.Latitude))
            {
                currentPicture.Latitude = otherPicture.Latitude.Replace(',', '.');
            }

            if (!string.IsNullOrEmpty(otherPicture.Altitude))
            {
                currentPicture.Altitude = otherPicture.Altitude.Replace(',', '.');
            }
        }

        public static void CopyPropertiesForAdd(this Picture currentPicture, Picture otherPicture)
        {
            currentPicture.AccessLevel = otherPicture.AccessLevel;
            currentPicture.CommentThreadNumber = otherPicture.CommentThreadNumber;
            currentPicture.ProgenyId = otherPicture.ProgenyId;
            currentPicture.PictureLink = otherPicture.PictureLink;
            currentPicture.PictureLink600 = otherPicture.PictureLink600;
            currentPicture.PictureLink1200 = otherPicture.PictureLink1200;
            currentPicture.Author = otherPicture.Author;
            currentPicture.TimeZone = otherPicture.TimeZone;
            currentPicture.PictureTime = otherPicture.PictureTime;
            currentPicture.Owners = otherPicture.Owners;
            currentPicture.PictureHeight = otherPicture.PictureHeight;
            currentPicture.PictureWidth = otherPicture.PictureWidth;
            currentPicture.PictureRotation = otherPicture.PictureRotation;
            currentPicture.PictureNumber = otherPicture.PictureNumber;
            currentPicture.Progeny = otherPicture.Progeny;
            currentPicture.Comments = otherPicture.Comments;

            if (!string.IsNullOrEmpty(otherPicture.Tags))
            {
                currentPicture.Tags = otherPicture.Tags.TrimEnd(',', ' ').TrimStart(',', ' ');
            }

            if (!string.IsNullOrEmpty(otherPicture.Location))
            {
                currentPicture.Location = otherPicture.Location;
            }

            if (!string.IsNullOrEmpty(otherPicture.Longtitude))
            {
                currentPicture.Longtitude = otherPicture.Longtitude.Replace(',', '.');
            }

            if (!string.IsNullOrEmpty(otherPicture.Latitude))
            {
                currentPicture.Latitude = otherPicture.Latitude.Replace(',', '.');
            }

            if (!string.IsNullOrEmpty(otherPicture.Altitude))
            {
                currentPicture.Altitude = otherPicture.Altitude.Replace(',', '.');
            }
        }

        public static void ApplyPlacholderProperties(this Picture picture)
        {
            Progeny progeny = new();
            progeny.ApplyPlaceHolderProperties();

            picture.ProgenyId = 0;
            picture.Progeny = progeny;
            picture.AccessLevel = 5;
            picture.PictureLink = Constants.WebAppUrl + "/photodb/0/default_temp.jpg";
            picture.PictureLink600 = Constants.WebAppUrl + "/photodb/0/default_temp.jpg";
            picture.PictureLink1200 = Constants.WebAppUrl + "/photodb/0/default_temp.jpg";
            picture.PictureTime = new DateTime(2018, 9, 1, 12, 00, 00);
        }
    }
}
