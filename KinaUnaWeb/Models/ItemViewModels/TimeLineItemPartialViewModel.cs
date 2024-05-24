namespace KinaUnaWeb.Models.ItemViewModels
{
    public class TimeLineItemPartialViewModel(string partialViewName, object timeLineItem)
    {
        public string PartialViewName { get; set; } = partialViewName;
        public object TimeLineItem { get; set; } = timeLineItem;
    }
}
