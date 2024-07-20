using KinaUna.Data.Models;
using System;

namespace KinaUna.Data.Extensions
{
    /// <summary>
    /// Extension methods for the Comment class.
    /// </summary>
    public static class CommentExtensions
    {
        /// <summary>
        /// Copies the properties needed for updating a Comment entity from one Comment object to another.
        /// </summary>
        /// <param name="currentComment"></param>
        /// <param name="otherComment"></param>
        public static void CopyPropertiesForUpdate(this Comment currentComment, Comment otherComment)
        {
            currentComment.CommentText = otherComment.CommentText;
            currentComment.Author = otherComment.Author;
            currentComment.DisplayName = otherComment.DisplayName;
            currentComment.Created = otherComment.Created;
            currentComment.ItemType = otherComment.ItemType;
            currentComment.ItemId = otherComment.ItemId;
        }

        /// <summary>
        /// Copies the properties needed for adding a Comment entity from one Comment object to another.
        /// </summary>
        /// <param name="currentComment"></param>
        /// <param name="otherComment"></param>
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
