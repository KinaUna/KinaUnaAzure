using System.ComponentModel.DataAnnotations;

namespace KinaUna.Data.Models
{
    public class CommentThread
    {
        [Key]
        public int Id { get; set; }
        public int CommentThreadId { get; set; }
        public int CommentsCount { get; set; }
    }
}
