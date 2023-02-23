using System;
using KinaUna.Data.Extensions;
using KinaUna.Data.Models;

namespace KinaUnaWeb.Models.ItemViewModels
{
    public class CommentViewModel: BaseItemsViewModel
    {
        public int CommentId { get; set; }
        public int CommentThreadNumber { get; set; }
        public string CommentText { get; set; }
        public string Author { get; set; }
        public string DisplayName { get; set; }
        public DateTime Created { get; set; }
        public int SortBy { get; set; }

        public int ItemId { get; set; }
        
        public CommentViewModel()
        {
            
        }

        public CommentViewModel(BaseItemsViewModel baseItemsViewModel)
        {
            SetBaseProperties(baseItemsViewModel);
        }

        public Comment CreateComment(int commentType)
        {
            Comment comment = new Comment();
            comment.CommentThreadNumber = CommentThreadNumber;
            comment.CommentText = CommentText;
            comment.Author = CurrentUser.UserId;
            comment.DisplayName = CurrentUser.FullName();
            comment.Created = DateTime.UtcNow;
            comment.ItemType = commentType;
            comment.ItemId = ItemId.ToString();
            comment.Progeny = CurrentProgeny;

            return comment;
        }
    }
}
