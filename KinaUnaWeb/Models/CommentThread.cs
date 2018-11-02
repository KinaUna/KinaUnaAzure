using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace KinaUnaWeb.Models
{
    public class CommentThread
    {
        [Key]
        public int Id { get; set; }
        public int CommentThreadId { get; set; }
        public int CommentsCount { get; set; }
    }
}
