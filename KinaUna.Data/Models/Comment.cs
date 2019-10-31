using System;
using System.ComponentModel.DataAnnotations.Schema;
using Newtonsoft.Json;

namespace KinaUna.Data.Models
{
    public class Comment
    {
        public int CommentId { get; set; }
        public int CommentThreadNumber { get; set; }
        public string CommentText { get; set; }
        public string Author { get; set; }
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
