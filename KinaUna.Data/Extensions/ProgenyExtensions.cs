using KinaUna.Data.Models;
using System;

namespace KinaUna.Data.Extensions
{
    public static class ProgenyExtensions
    {
        /// <summary>
        /// Verifies if an email address is in the admin list.
        /// </summary>
        /// <param name="progeny">Progeny</param>
        /// <param name="email">string: The email address to verify.</param>
        /// <returns>bool: True if the email address is in the admin list, otherwise false.</returns>
        public static bool IsInAdminList(this Progeny progeny, string email)
        {
            if (progeny == null || string.IsNullOrEmpty(progeny.Admins) || string.IsNullOrEmpty(email))
            {
                return false;
            }
            string[] adminList = progeny.Admins.Split(',');
            string emailTrimmed = email.Trim().ToUpper();
            foreach (string adminItem in adminList)
            {
                string itemTrimmed = adminItem.Trim().ToUpper();
                if (itemTrimmed.Equals(emailTrimmed))
                {
                    return true;
                }
            }
            return false;
        }

        public static void ApplyPlaceHolderProperties(this Progeny progeny)
        {
            progeny.Name = Constants.AppName;
            progeny.Admins = Constants.SupportEmail;
            progeny.NickName = Constants.AppName;
            progeny.BirthDay = new DateTime(2018, 2, 18, 18, 2, 0);
            progeny.Id = 0;
            progeny.TimeZone = Constants.DefaultTimezone;
        }

        public static string GetProfilePictureUrl(this Progeny progeny)
        {
            if (progeny == null || progeny.PictureLink == null)
            {
                return Constants.ProfilePictureUrl;
            }

            if (progeny.PictureLink.StartsWith("http:", StringComparison.CurrentCultureIgnoreCase))
            {
                return progeny.PictureLink;
            }

            string pictureUrl = "/Progeny/ProfilePicture/" + progeny.Id + "?imageId=" + progeny.PictureLink;

            return pictureUrl;
        }


        public static string GetPictureFileContentType(this Progeny progeny)
        {
            string contentType = FileContentTypeHelpers.GetContentTypeString(progeny.PictureLink);

            return contentType;
        }
    }
}
