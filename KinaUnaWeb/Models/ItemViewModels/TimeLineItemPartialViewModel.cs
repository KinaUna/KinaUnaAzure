namespace KinaUnaWeb.Models.ItemViewModels
{
    public class TimeLineItemPartialViewModel(string partialViewName, object timeLineItem)
    {
        public string PartialViewName { get; } = partialViewName;
        public object TimeLineItem { get; } = timeLineItem;
    }
}
