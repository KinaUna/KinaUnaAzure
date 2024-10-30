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
            if (picture == null) return;

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
            picture.PictureTime ??= DateTime.MinValue;
        }

        /// <summary>
        /// Copies the properties needed when users edits a Picture.
        /// </summary>
        /// <param name="currentPicture"></param>
        /// <param name="otherPicture"></param>
        public static void CopyPropertiesForUserUpdate(this Picture currentPicture, Picture otherPicture)
        {
            otherPicture.RemoveNullStrings();

            currentPicture.AccessLevel = otherPicture.AccessLevel;
            currentPicture.PictureTime = otherPicture.PictureTime;
            currentPicture.Tags = otherPicture.Tags.TrimEnd(',', ' ').TrimStart(',', ' ');
            currentPicture.Location = otherPicture.Location.TrimEnd(',', ' ');
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

        /// <summary>
        /// Copies the properties needed for updating a Picture entity from one Picture object to another.
        /// </summary>
        /// <param name="currentPicture"></param>
        /// <param name="otherPicture"></param>
        public static void CopyPropertiesForUpdate(this Picture currentPicture, Picture otherPicture)
        {
            otherPicture.RemoveNullStrings();

            currentPicture.AccessLevel = otherPicture.AccessLevel;
            currentPicture.CommentThreadNumber = otherPicture.CommentThreadNumber;
            currentPicture.TimeZone = otherPicture.TimeZone;
            currentPicture.Owners = otherPicture.Owners;
            currentPicture.PictureRotation = otherPicture.PictureRotation;
            currentPicture.PictureTime = otherPicture.PictureTime;
            currentPicture.PictureNumber = otherPicture.PictureNumber;
            currentPicture.PictureLink = otherPicture.PictureLink;
            currentPicture.PictureLink600 = otherPicture.PictureLink600;
            currentPicture.PictureLink1200 = otherPicture.PictureLink1200;
            currentPicture.Tags = otherPicture.Tags?.TrimEnd(',', ' ').TrimStart(',', ' ') ?? "";
            currentPicture.Location = otherPicture.Location?.TrimEnd(',', ' ') ?? "";
            currentPicture.Longtitude = otherPicture.Longtitude?.Replace(',', '.') ?? "";
            currentPicture.Latitude = otherPicture.Latitude?.Replace(',', '.') ?? "";
            currentPicture.Altitude = otherPicture.Altitude?.Replace(',', '.') ?? "";
        }

        /// <summary>
        /// Copies the properties needed for adding a Picture entity from one Picture object to another.
        /// </summary>
        /// <param name="currentPicture"></param>
        /// <param name="otherPicture"></param>
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
            else
            {
                currentPicture.Tags = "";
            }

            if (!string.IsNullOrEmpty(otherPicture.Location))
            {
                currentPicture.Location = otherPicture.Location;
            }
            else
            {
                currentPicture.Location = "";
            }

            if (!string.IsNullOrEmpty(otherPicture.Longtitude))
            {
                currentPicture.Longtitude = otherPicture.Longtitude.Replace(',', '.');
            }
            else
            {
                currentPicture.Longtitude = "";
            }

            if (!string.IsNullOrEmpty(otherPicture.Latitude))
            {
                currentPicture.Latitude = otherPicture.Latitude.Replace(',', '.');
            }
            else
            {
                currentPicture.Latitude = "";
            }

            if (!string.IsNullOrEmpty(otherPicture.Altitude))
            {
                currentPicture.Altitude = otherPicture.Altitude.Replace(',', '.');
            }
            else
            {
                currentPicture.Altitude = "";
            }
        }

        /// <summary>
        /// Copies the properties needed when users edits a Picture.
        /// </summary>
        /// <param name="currentPicture"></param>
        /// <param name="otherPicture"></param>
        /// <param name="ownerEmail"></param>
        /// <param name="progeny"></param>
        public static void CopyPropertiesForCopy(this Picture currentPicture, Picture otherPicture, string ownerEmail, Progeny progeny)
        {
            otherPicture.RemoveNullStrings();

            currentPicture.PictureId = 0;
            currentPicture.ProgenyId = otherPicture.ProgenyId;
            currentPicture.Progeny = progeny;
            currentPicture.Owners = ownerEmail;
            currentPicture.AccessLevel = otherPicture.AccessLevel;
            currentPicture.PictureTime = otherPicture.PictureTime;
            currentPicture.Tags = otherPicture.Tags.TrimEnd(',', ' ').TrimStart(',', ' ');
            currentPicture.Location = otherPicture.Location.TrimEnd(',', ' ');
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

        /// <summary>
        /// Applies placeholder properties to a Picture object, used when no picture is found.
        /// </summary>
        /// <param name="picture"></param>
        public static void ApplyPlaceholderProperties(this Picture picture)
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

        /// <summary>
        /// Produces a string with the MIME type for a Picture object, based on the file extension.
        /// </summary>
        /// <param name="picture"></param>
        /// <returns>string with the MIME type.</returns>
        public static string GetPictureFileContentType(this Picture picture)
        {
            string contentType = FileContentTypeHelpers.GetContentTypeString(picture.PictureLink);

            return contentType;
        }

        /// <summary>
        /// Produces the URL for a Picture object.
        /// </summary>
        /// <param name="picture"></param>
        /// <param name="size"></param>
        /// <returns>string with the URL.</returns>
        public static string GetPictureUrl(this Picture picture, int size)
        {
            if (picture == null)
            {
                return "";
            }

            if (!string.IsNullOrEmpty(picture.PictureLink) && picture.PictureLink.StartsWith("http:", StringComparison.CurrentCultureIgnoreCase))
            {
                return picture.PictureLink;
            }

            string pictureUrl = "/Pictures/File?id=" + picture.PictureId + "&size=" + size;

            return pictureUrl;
        }
    }
}
