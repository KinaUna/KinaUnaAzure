using KinaUna.Data.Models;
using System;
using System.Collections.Generic;

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

        /// <summary>
        /// Converts the comma-separated admin list into a List of strings.
        /// </summary>
        /// <param name="progeny">The progeny object.</param>
        /// <returns>List of strings: The list of admin email addresses.</returns>
        public static List<string> GetAdminsList(this Progeny progeny)
        {
            string[] adminList = progeny?.Admins?.Split(',') ?? [];
            List<string> admins = new List<string>();
            foreach (string adminItem in adminList)
            {
                string itemTrimmed = adminItem.Trim();
                if (!string.IsNullOrEmpty(itemTrimmed))
                {
                    admins.Add(itemTrimmed);
                }
            }

            return admins;
        }

        /// <summary>
        /// Adds an email address to the admin list if it is not already present.
        /// </summary>
        /// <param name="progeny">The progeny object.</param>
        /// <param name="email">The email address to add.</param>
        /// <returns></returns>
        public static bool AddToAdminList(this Progeny progeny, string email)
        {
            if (progeny == null || string.IsNullOrEmpty(email))
            {
                return false;
            }
            if (string.IsNullOrEmpty(progeny.Admins))
            {
                progeny.Admins = email.Trim();
                return true;
            }
            if (progeny.IsInAdminList(email))
            {
                return false;
            }

            progeny.Admins = progeny.Admins + "," + email.Trim();
            return true;
        }

        /// <summary>
        /// Removes the specified email address from the admin list of the given progeny.
        /// </summary>
        /// <remarks>The comparison of email addresses is case-insensitive and ignores leading or trailing
        /// whitespace. If the admin list becomes empty after removal, the <see cref="Progeny.Admins"/> property will be
        /// set to an empty string.</remarks>
        /// <param name="progeny">The <see cref="Progeny"/> instance whose admin list will be modified. Cannot be <see langword="null"/>.</param>
        /// <param name="email">The email address to remove from the admin list. Cannot be <see langword="null"/> or empty.</param>
        /// <returns><see langword="true"/> if the email address was successfully removed from the admin list;  otherwise, <see
        /// langword="false"/> if the email address was not found in the admin list,  the admin list is empty, or the
        /// input parameters are invalid.</returns>
        public static bool RemoveFromAdminList(this Progeny progeny, string email)
        {
            if (progeny == null || string.IsNullOrEmpty(progeny.Admins) || string.IsNullOrEmpty(email))
            {
                return false;
            }
            if (!progeny.IsInAdminList(email))
            {
                return false;
            }
            string[] adminList = progeny.Admins.Split(',');
            string emailTrimmed = email.Trim().ToUpper();
            string newAdminList = string.Empty;
            foreach (string adminItem in adminList)
            {
                string itemTrimmed = adminItem.Trim();
                if (!itemTrimmed.ToUpper().Equals(emailTrimmed))
                {
                    if (string.IsNullOrEmpty(newAdminList))
                    {
                        newAdminList = itemTrimmed;
                    }
                    else
                    {
                        newAdminList = newAdminList + "," + itemTrimmed;
                    }
                }
            }

            progeny.Admins = newAdminList;
            return true;
        }

        /// <summary>
        /// Applies placeholder properties to a Progeny object. Used when a progeny cannot be found.
        /// </summary>
        /// <param name="progeny"></param>
        public static void ApplyPlaceHolderProperties(this Progeny progeny)
        {
            progeny.Name = Constants.AppName;
            progeny.Admins = Constants.SupportEmail;
            progeny.NickName = Constants.AppName;
            progeny.BirthDay = new DateTime(2018, 2, 18, 18, 2, 0);
            progeny.Id = 0;
            progeny.TimeZone = Constants.DefaultTimezone;
        }

        /// <summary>
        /// Produces a URL for the profile picture of a Progeny object.
        /// </summary>
        /// <param name="progeny"></param>
        /// <returns>string with the URL.</returns>
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


        /// <summary>
        /// Produces a string with the MIME type for a Progeny's profile picture, based on the file extension.
        /// </summary>
        /// <param name="progeny"></param>
        /// <returns>string with the MIME type.</returns>
        public static string GetPictureFileContentType(this Progeny progeny)
        {
            string contentType = FileContentTypeHelpers.GetContentTypeString(progeny.PictureLink);

            return contentType;
        }
    }
}
