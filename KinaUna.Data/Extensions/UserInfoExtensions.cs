using KinaUna.Data.Models;

namespace KinaUna.Data.Extensions
{
    public static class UserInfoExtensions
    {
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
        

        public static string GetPictureFileContentType(this UserInfo userInfo)
        {
            string contentType = FileContentTypeHelpers.GetContentTypeString(userInfo.ProfilePicture);
            
            return contentType;
        }
    }
}
