using System.ComponentModel.DataAnnotations;

namespace KinaUna.Data.Models
{
    /// <summary>
    /// Entity Framework Entity for comment thread data.
    /// </summary>
    public class CommentThread
    {
        [Key]
        public int Id { get; init; }
        public int CommentsCount { get; set; }
    }
}
