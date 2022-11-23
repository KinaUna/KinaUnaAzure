using System.Collections.Generic;
using System.Threading.Tasks;
using KinaUna.Data.Models;

namespace KinaUnaProgenyApi.Services
{
    public interface ITimelineService
    {
        Task<TimeLineItem> GetTimeLineItem(int id);
        Task<TimeLineItem> AddTimeLineItem(TimeLineItem timeLineItem);
        Task<TimeLineItem> UpdateTimeLineItem(TimeLineItem item);
        Task<TimeLineItem> DeleteTimeLineItem(TimeLineItem item);
        Task<TimeLineItem> GetTimeLineItemByItemId(string itemId, int itemType);
        Task<List<TimeLineItem>> GetTimeLineList(int progenyId);
    }
}
