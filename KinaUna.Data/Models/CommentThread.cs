using System.ComponentModel.DataAnnotations;

namespace KinaUna.Data.Models
{
    public class CommentThread
    {
        [Key]
        public int Id { get; init; }
        public int CommentsCount { get; set; }
    }
}
