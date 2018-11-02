using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace KinaUnaMediaApi.Models.DTOs
{
    public class CommentDto
    {
        public List<Comment> CommentsList { get; set; }
        public List<CommentThread> CommentThreadsList { get; set; }
        
    }
}
