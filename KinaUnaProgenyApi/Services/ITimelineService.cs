using System.Collections.Generic;
using System.Threading.Tasks;
using KinaUna.Data.Models;
using KinaUna.Data.Models.DTOs;

namespace KinaUnaProgenyApi.Services
{
    public interface ITimelineService
    {
        /// <summary>
        /// Gets the TimeLineItem with the specified TimeLineId.
        /// </summary>
        /// <param name="id">The TimeLineId of the TimeLineItem to get.</param>
        /// <param name="currentUserInfo">The UserInfo object for the current user, to check permissions.</param>
        /// <returns>The TimeLineItem with the given TimeLineId. Null if the TimeLineItem doesn't exist.</returns>
        Task<TimeLineItem> GetTimeLineItem(int id, UserInfo currentUserInfo);

        /// <summary>
        /// Adds a new TimeLineItem to the database and adds it to the cache.
        /// </summary>
        /// <param name="timeLineItem">The TimeLineItem to add.</param>
        /// <param name="currentUserInfo"></param>
        /// <returns>The added TimeLineItem.</returns>
        Task<TimeLineItem> AddTimeLineItem(TimeLineItem timeLineItem, UserInfo currentUserInfo);

        /// <summary>
        /// Updates a TimeLineItem in the database and the cache.
        /// </summary>
        /// <param name="item">The TimeLineItem with the updated properties.</param>
        /// <param name="currentUserInfo"></param>
        /// <returns>The updated TimeLineItem. Null if a TimeLineItem with the TimeLineId doesn't already exist.</returns>
        Task<TimeLineItem> UpdateTimeLineItem(TimeLineItem item, UserInfo currentUserInfo);

        /// <summary>
        /// Deletes a TimeLineItem from the database and the cache.
        /// </summary>
        /// <param name="item">The TimeLineItem to delete.</param>
        /// <param name="currentUserInfo"></param>
        /// <returns>The deleted TimeLineItem. Null if a TimeLineItem with the TimeLineId doesn't exist.</returns>
        Task<TimeLineItem> DeleteTimeLineItem(TimeLineItem item, UserInfo currentUserInfo);

        /// <summary>
        /// Gets the TimeLineItem with the specified ItemId and ItemType.
        /// First checks the cache, if not found, gets the TimeLineItem from the database and adds it to the cache.
        /// </summary>
        /// <param name="itemId">The ItemId of the TimeLineItem.</param>
        /// <param name="itemType">The ItemType (see KinaUnaTypes.TimeLineTypes enum) of the TimeLineItem.</param>
        /// <param name="currentUserInfo">The UserInfo object for the current user, to check permissions.</param>
        /// <returns>The TimeLineItem with the given ItemId and ItemType. Null if the TimeLineItem doesn't exist.</returns>
        Task<TimeLineItem> GetTimeLineItemByItemId(string itemId, int itemType, UserInfo currentUserInfo);

        /// <summary>
        /// Gets a list of all TimeLineItems for a Progeny.
        /// First checks the cache, if not found, gets the list from the database and adds it to the cache.
        /// </summary>
        /// <param name="progenyId">The ProgenyId of the Progeny to get TimeLineItems for.</param>
        /// <param name="currentUserInfo">The UserInfo object for the current user, to check permissions.</param>
        /// <returns>List of TimeLineItem objects.</returns>
        Task<List<TimeLineItem>> GetTimeLineList(int progenyId, UserInfo currentUserInfo);

        /// <summary>
        /// Creates a OnThisDayResponse for displaying TimeLineItems on the OnThisDay page.
        /// </summary>
        /// <param name="onThisDayRequest">The OnThisDayRequest object with the parameters.</param>
        /// <param name="currentUserInfo">The current users UserInfo.</param>
        /// <returns>OnThisDayResponse object.</returns>
        Task<OnThisDayResponse> GetOnThisDayData(OnThisDayRequest onThisDayRequest, UserInfo currentUserInfo);

        /// <summary>
        /// Gets a TimelineResponse for displaying TimeLineItems on the Timeline page.
        /// </summary>
        /// <param name="timelineRequest">The TimelineRequest object with the parameters.</param>
        /// <param name="currentUserInfo">The current users UserInfo.</param>
        /// <returns>TimelineResponse with the filtered list of Timeline items.</returns>
        Task<TimelineResponse> GetTimelineData(TimelineRequest timelineRequest, UserInfo currentUserInfo);
    }
}
