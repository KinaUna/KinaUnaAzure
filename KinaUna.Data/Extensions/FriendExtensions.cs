﻿using KinaUna.Data.Models;
using System;

namespace KinaUna.Data.Extensions
{
    public static class FriendExtensions
    {
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


        public static string GetPictureFileContentType(this Friend friend)
        {
            string contentType = FileContentTypeHelpers.GetContentTypeString(friend.PictureLink);

            return contentType;
        }
    }
}
