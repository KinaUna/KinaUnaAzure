using KinaUna.Data.Models;
using System;

namespace KinaUna.Data.Extensions
{
    public static class CommentExtensions
    {
        public static void CopyPropertiesForUpdate(this Comment currentComment, Comment otherComment)
        {
            currentComment.CommentText = otherComment.CommentText;
            currentComment.Author = otherComment.Author;
            currentComment.DisplayName = otherComment.DisplayName;
            currentComment.Created = otherComment.Created;
            currentComment.ItemType = otherComment.ItemType;
            currentComment.ItemId = otherComment.ItemId;
        }

        public static void CopyPropertiesForAdd(this Comment currentComment, Comment otherComment)
        {
            currentComment.Created = DateTime.UtcNow;
            currentComment.Author = otherComment.Author;
            currentComment.CommentText = otherComment.CommentText;
            currentComment.CommentThreadNumber = otherComment.CommentThreadNumber;
            currentComment.DisplayName = otherComment.DisplayName;
            currentComment.ItemType = otherComment.ItemType;
            currentComment.ItemId = otherComment.ItemId;
        }
    }
}
