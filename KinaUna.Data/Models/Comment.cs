using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace KinaUna.Data.Models
{
    public class Comment
    {
        public int CommentId { get; set; }
        public int CommentThreadNumber { get; set; }
        [MaxLength(4096)] 
        public string CommentText { get; set; }

        [MaxLength(256)]
        public string Author { get; set; }

        [MaxLength(256)]
        public string DisplayName { get; set; }
        public DateTime Created { get; set; }

        [NotMapped]
        public string AuthorImage { get; set; }
        [NotMapped]
        public Progeny Progeny { get; set; }
        [NotMapped]
        public int ItemType { get; set; }
        [NotMapped]
        public string ItemId { get; set; }
        [NotMapped]
        public int AccessLevel { get; set; }
    }
}
