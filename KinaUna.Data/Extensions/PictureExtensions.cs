using System;
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
                if (picture.Altitude == null)
                {
                    picture.Altitude = "";
                }

                if (picture.Tags == null)
                {
                    picture.Tags = "";
                }

                if (picture.Author == null)
                {
                    picture.Author = "";
                }

                if (picture.Latitude == null)
                {
                    picture.Latitude = "";
                }

                if (picture.Location == null)
                {
                    picture.Location = "";
                }

                if (picture.Longtitude == null)
                {
                    picture.Longtitude = "";
                }

                if (picture.Owners == null)
                {
                    picture.Owners = "";
                }

                if (picture.PictureLink1200 == null)
                {
                    picture.PictureLink1200 = "";
                }

                if (picture.PictureLink == null)
                {
                    picture.PictureLink = "";
                }

                if (picture.PictureLink600 == null)
                {
                    picture.PictureLink600 = "";
                }

                if (picture.TimeZone == null)
                {
                    picture.TimeZone = "";
                }
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
            Progeny progeny = new Progeny();
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
