using System;
using KinaUna.Data.Extensions;

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
        public bool PartialView { get; set; }

        public CommentViewModel()
        {
            
        }

        public CommentViewModel(BaseItemsViewModel baseItemsViewModel)
        {
            SetBaseProperties(baseItemsViewModel);
        }

        public Comment CreateComment(int commentType)
        {
            Comment comment = new()
            {
                CommentThreadNumber = CommentThreadNumber,
                CommentText = CommentText,
                Author = CurrentUser.UserId,
                DisplayName = CurrentUser.FullName(),
                Created = DateTime.UtcNow,
                ItemType = commentType,
                ItemId = ItemId.ToString(),
                Progeny = CurrentProgeny
            };

            return comment;
        }
    }
}
