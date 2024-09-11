using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace KinaUna.Data.Models
{
    /// <summary>
    /// Entity Framework Entity for Comment data.
    /// Used for comments on other entities, such as Pictures and Videos.
    /// CommentThreadNumber is used to group comments on the same item.
    /// </summary>
    public class Comment
    {
        /// <summary>
        /// Primary key for the comment.
        /// </summary>
        public int CommentId { get; init; }
        /// <summary>
        /// The Id of the thread the comment belongs to.
        /// </summary>
        public int CommentThreadNumber { get; set; }
        /// <summary>
        /// The main text of the comment.
        /// </summary>
        [MaxLength(4096)] 
        public string CommentText { get; set; }
        /// <summary>
        /// The Id of the user who created the comment.
        /// </summary>
        [MaxLength(256)]
        public string Author { get; set; }
        /// <summary>
        /// The display name of the user who created the comment.
        /// </summary>
        [MaxLength(256)]
        public string DisplayName { get; set; }
        /// <summary>
        /// The date and time the comment was created.
        /// </summary>
        public DateTime Created { get; set; }

        /// <summary>
        /// The profile picture of the user who created the comment.
        /// </summary>
        [NotMapped]
        public string AuthorImage { get; set; }
        /// <summary>
        /// Progeny associated with the comment.
        /// </summary>
        [NotMapped]
        public Progeny Progeny { get; set; }
        /// <summary>
        /// The type of item the comment is associated with.
        /// </summary>
        [NotMapped]
        public int ItemType { get; set; }
        /// <summary>
        /// The Id (PictureId, EventId, etc.) of the item the comment is associated with.
        /// </summary>
        [NotMapped]
        public string ItemId { get; set; }
        /// <summary>
        /// The access level required to view the comment and the item the comment is associated with.
        /// </summary>
        [NotMapped]
        public int AccessLevel { get; set; }
    }
}
