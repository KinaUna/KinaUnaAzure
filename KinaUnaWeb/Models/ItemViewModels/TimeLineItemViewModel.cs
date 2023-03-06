namespace KinaUnaWeb.Models.ItemViewModels
{
    public class TimeLineItemViewModel: BaseItemsViewModel
    {
        public int TypeId { get; set; }
        public int ItemId { get; set; }
        public string TagFilter { get; set; } = string.Empty;
    }
}
