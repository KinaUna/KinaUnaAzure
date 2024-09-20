using KinaUna.Data.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace KinaUnaProgenyApi.Services
{
    public interface ITimelineFilteringService
    {
        Task<List<TimeLineItem>> GetTimeLineItemsWithKeyword(List<TimeLineItem> timeLineItems, string keyword);
        Task<List<TimeLineItem>> GetTimeLineItemsWithTags(List<TimeLineItem> timeLineItems, string tags);
    }
}