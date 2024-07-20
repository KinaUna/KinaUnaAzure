using KinaUna.Data.Models;

namespace KinaUna.Data.Extensions
{
    /// <summary>
    /// Extension methods for the UserAccess class.
    /// </summary>
    public static class UserAccessExtensions
    {
        /// <summary>
        /// Copies the properties needed for updating a UserAccess entity from one UserAccess object to another.
        /// </summary>
        /// <param name="userAccess"></param>
        /// <param name="otherUserAccess"></param>
        public static void CopyForUpdate(this UserAccess userAccess, UserAccess otherUserAccess)
        {
            userAccess.UserId = otherUserAccess.UserId;
            userAccess.Progeny = otherUserAccess.Progeny;
            userAccess.AccessLevel = otherUserAccess.AccessLevel;
            userAccess.AccessLevelString = otherUserAccess.AccessLevelString;
            userAccess.CanContribute = otherUserAccess.CanContribute;
            userAccess.ProgenyId = otherUserAccess.ProgenyId;
            userAccess.User = otherUserAccess.User;
        }

    }
}
