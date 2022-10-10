namespace KinaUnaWebBlazor.Models.ItemViewModels
{
    public class CommentViewModel: BaseViewModel
    {
        public int CommentId { get; set; } = 0;
        public int CommentThreadNumber { get; set; } = 0;
        public string CommentText { get; set; } = "";
        public string Author { get; set; } = "";
        public string DisplayName { get; set; } = "";
        public DateTime Created { get; set; } = DateTime.UtcNow;
        public int ItemId { get; set; } = 0;
        public int ProgenyId { get; set; } = 0;
    }
}
