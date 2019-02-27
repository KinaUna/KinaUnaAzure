using System;

namespace KinaUnaWeb.Models.ItemViewModels
{
    public class CommentViewModel
    {
        public int CommentId { get; set; }
        public int CommentThreadNumber { get; set; }
        public string CommentText { get; set; }
        public string Author { get; set; }
        public string DisplayName { get; set; }
        public DateTime Created { get; set; }

        public int ItemId { get; set; }
        public int ProgenyId { get; set; }
    }
}
