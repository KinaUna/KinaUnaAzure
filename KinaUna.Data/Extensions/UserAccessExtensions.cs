using KinaUna.Data.Models;

namespace KinaUna.Data.Extensions
{
    public static class UserAccessExtensions
    {
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
