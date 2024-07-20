using KinaUna.Data.Models;

namespace KinaUna.Data.Extensions
{
    /// <summary>
    /// Extension methods for the UserInfo class.
    /// </summary>
    public static class UserInfoExtensions
    {
        /// <summary>
        /// Produces the user's full name from the first name, middle name and last name properties.
        /// </summary>
        /// <param name="userInfo"></param>
        /// <returns>string: The full name as a single string</returns>
        public static string FullName(this UserInfo userInfo)
        {
            string fullName = "";
            if (!string.IsNullOrEmpty(userInfo.FirstName))
            {
                fullName = userInfo.FirstName;
            }

            if (!string.IsNullOrEmpty(userInfo.MiddleName))
            {
                fullName = fullName + " " + userInfo.MiddleName;
            }

            if (!string.IsNullOrEmpty(userInfo.LastName))
            {
                fullName = fullName + " " + userInfo.LastName;
            }

            fullName = fullName.Trim();
            if (string.IsNullOrEmpty(fullName))
            {
                fullName = userInfo.UserName;
            }

            return fullName;
        }

        /// <summary>
        /// Produces the URL for the user's profile picture.
        /// </summary>
        /// <param name="userInfo"></param>
        /// <returns>string: The url for the profile picture.</returns>
        public static string GetProfilePictureUrl(this UserInfo userInfo)
        {
            if (userInfo == null || userInfo.ProfilePicture == null)
            {
                return Constants.ProfilePictureUrl;
            }

            if (userInfo.ProfilePicture.StartsWith("http", System.StringComparison.CurrentCultureIgnoreCase))
            {
                return userInfo.ProfilePicture;
            }

            string pictureUrl = "/Account/ProfilePicture/" + userInfo.UserId + "?imageId=" + userInfo.ProfilePicture;

            return pictureUrl;
        }
        
        /// <summary>
        /// Obtains the MIME type for the user's profile picture. Based on the file extension.
        /// </summary>
        /// <param name="userInfo"></param>
        /// <returns>string: The MIME type of the picture file.</returns>
        public static string GetPictureFileContentType(this UserInfo userInfo)
        {
            string contentType = FileContentTypeHelpers.GetContentTypeString(userInfo.ProfilePicture);
            
            return contentType;
        }
    }
}
