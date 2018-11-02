﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace KinaUnaWeb.Models
{
    public class Comment
    {
        public int CommentId { get; set; }
        public int CommentThreadNumber { get; set; }
        public string CommentText { get; set; }
        public string Author { get; set; }
        public string DisplayName { get; set; }
        public DateTime Created { get; set; }
    }
}
