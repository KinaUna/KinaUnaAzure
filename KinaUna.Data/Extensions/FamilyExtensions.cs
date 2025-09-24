using System.Collections.Generic;
using KinaUna.Data.Models.Family;

namespace KinaUna.Data.Extensions
{
    public static class FamilyExtensions
    {
        /// <summary>
        /// Verifies if an email address is in the admin list.
        /// </summary>
        /// <param name="family">The Family object.</param>
        /// <param name="email">string: The email address to verify.</param>
        /// <returns>bool: True if the email address is in the admin list, otherwise false.</returns>
        public static bool IsInAdminList(this Family family, string email)
        {
            if (family == null || string.IsNullOrEmpty(family.Admins) || string.IsNullOrEmpty(email))
            {
                return false;
            }
            string[] adminList = family.Admins.Split(',');
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
        /// <param name="family">The Family object.</param>
        /// <returns>List of strings: The list of admin email addresses.</returns>
        public static List<string> GetAdminsList(this Family family)
        {
            string[] adminList = family?.Admins?.Split(',') ?? [];
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
        /// <param name="family">The Family object.</param>
        /// <param name="email">The email address to add.</param>
        /// <returns></returns>
        public static bool AddToAdminList(this Family family, string email)
        {
            if (family == null || string.IsNullOrEmpty(email))
            {
                return false;
            }
            if (string.IsNullOrEmpty(family.Admins))
            {
                family.Admins = email.Trim();
                return true;
            }
            if (family.IsInAdminList(email))
            {
                return false;
            }
            family.Admins = family.Admins + "," + email.Trim();
            return true;
        }

        /// <summary>
        /// Removes the specified email address from the admin list of the given family.
        /// </summary>
        /// <remarks>The comparison of email addresses is case-insensitive and ignores leading or trailing
        /// whitespace. If the admin list becomes empty after removal, the <see cref="Family.Admins"/> property will be
        /// set to an empty string.</remarks>
        /// <param name="family">The <see cref="Family"/> instance whose admin list will be modified. Cannot be <see langword="null"/>.</param>
        /// <param name="email">The email address to remove from the admin list. Cannot be <see langword="null"/> or empty.</param>
        /// <returns><see langword="true"/> if the email address was successfully removed from the admin list;  otherwise, <see
        /// langword="false"/> if the email address was not found in the admin list,  the admin list is empty, or the
        /// input parameters are invalid.</returns>
        public static bool RemoveFromAdminList(this Family family, string email)
        {
            if (family == null || string.IsNullOrEmpty(family.Admins) || string.IsNullOrEmpty(email))
            {
                return false;
            }
            if (!family.IsInAdminList(email))
            {
                return false;
            }
            string[] adminList = family.Admins.Split(',');
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
            family.Admins = newAdminList;
            return true;
        }
    }
}
