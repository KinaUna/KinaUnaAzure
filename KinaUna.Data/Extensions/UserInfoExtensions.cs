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
    }
}
