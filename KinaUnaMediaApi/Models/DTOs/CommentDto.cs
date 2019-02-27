using System.Collections.Generic;

namespace KinaUnaMediaApi.Models.DTOs
{
    public class CommentDto
    {
        public List<Comment> CommentsList { get; set; }
        public List<CommentThread> CommentThreadsList { get; set; }
        
    }
}
