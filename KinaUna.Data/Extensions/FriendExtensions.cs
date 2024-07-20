using KinaUna.Data.Models;
using System;

namespace KinaUna.Data.Extensions
{
    /// <summary>
    /// Extension methods for the Friend class.
    /// </summary>
    public static class FriendExtensions
    {
        /// <summary>
        /// Copies the properties needed for updating a Friend entity from one Friend object to another.
        /// </summary>
        /// <param name="currentFriend"></param>
        /// <param name="otherFriend"></param>
        public static void CopyPropertiesForUpdate(this Friend currentFriend, Friend otherFriend )
        {
            currentFriend.AccessLevel = otherFriend.AccessLevel;
            currentFriend.Author = otherFriend.Author;
            currentFriend.Context = otherFriend.Context;
            currentFriend.Name = otherFriend.Name;
            currentFriend.Type = otherFriend.Type;
            currentFriend.ProgenyId = otherFriend.ProgenyId;
            currentFriend.Description = otherFriend.Description;
            currentFriend.FriendSince = otherFriend.FriendSince ?? DateTime.UtcNow;
            currentFriend.Notes = otherFriend.Notes;
            if (otherFriend.PictureLink != Constants.KeepExistingLink)
            {
                currentFriend.PictureLink = otherFriend.PictureLink;
            }

            currentFriend.Tags = otherFriend.Tags;
        }

        /// <summary>
        /// Copies the properties needed for adding a Friend entity from one Friend object to another.
        /// </summary>
        /// <param name="currentFriend"></param>
        /// <param name="otherFriend"></param>
        public static void CopyPropertiesForAdd(this Friend currentFriend, Friend otherFriend)
        {
            currentFriend.AccessLevel = otherFriend.AccessLevel;
            currentFriend.Author = otherFriend.Author;
            currentFriend.Context = otherFriend.Context;
            currentFriend.Name = otherFriend.Name;
            currentFriend.Type = otherFriend.Type;
            currentFriend.FriendAddedDate = DateTime.UtcNow;
            currentFriend.ProgenyId = otherFriend.ProgenyId;
            currentFriend.Description = otherFriend.Description;
            currentFriend.FriendSince = otherFriend.FriendSince;
            currentFriend.Notes = otherFriend.Notes;
            currentFriend.PictureLink = otherFriend.PictureLink;
            currentFriend.Tags = otherFriend.Tags;
        }

        /// <summary>
        /// Produces a URL for the profile picture of a Friend object.
        /// </summary>
        /// <param name="friend"></param>
        /// <returns>String with the URL</returns>
        public static string GetProfilePictureUrl(this Friend friend)
        {
            if (friend == null || friend.PictureLink == null)
            {
                return Constants.ProfilePictureUrl;
            }

            if (friend.PictureLink.StartsWith("http", StringComparison.CurrentCultureIgnoreCase))
            {
                return friend.PictureLink;
            }

            string pictureUrl = "/Friends/ProfilePicture/" + friend.FriendId + "?imageId=" + friend.PictureLink;

            return pictureUrl;
        }

        /// <summary>
        /// Generates a string with the MIME type for the profile picture of a Friend object, based on the file extension.
        /// </summary>
        /// <param name="friend"></param>
        /// <returns>String with the MIME content type</returns>
        public static string GetPictureFileContentType(this Friend friend)
        {
            string contentType = FileContentTypeHelpers.GetContentTypeString(friend.PictureLink);

            return contentType;
        }
    }
}
