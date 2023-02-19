namespace KinaUnaWeb.Models.ItemViewModels
{
    public class TimeLineItemPartialViewModel
    {
        public string PartialViewName { get; set; }
        public object TimeLineItem { get; set; }

        public TimeLineItemPartialViewModel(string partialViewName, object timeLineItem)
        {
            PartialViewName = partialViewName;
            TimeLineItem = timeLineItem;
        }
    }
}
