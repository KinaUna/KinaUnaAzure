using System.Collections.Generic;
using System.Threading.Tasks;
using KinaUna.Data.Models;
using KinaUna.Data.Models.DTOs;

namespace KinaUnaWeb.Services.HttpClients
{
    /// <summary>
    /// Provides methods to interact with the TimeLine API.
    /// </summary>
    public interface ITimelineHttpClient
    {
        /// <summary>
        /// Gets the TimeLineItem with the given ItemId and ItemType.
        /// </summary>
        /// <param name="itemId">The ItemId (i.e. the PictureId, VideoId, ContactId, etc. that the TimeLineItem belongs to).</param>
        /// <param name="itemType">The ItemType of the TimeLineItem. Defined in the KinaUnaTypes.TimeLineType enum.</param>
        /// <returns>TimeLineItem object. If it cannot be found a new TimeLineItem with ItemId=0 is returned.</returns>
        Task<TimeLineItem> GetTimeLineItem(string itemId, int itemType);

        /// <summary>
        /// Adds a new TimeLineItem.
        /// </summary>
        /// <param name="timeLineItem">The TimeLineItem to add.</param>
        /// <returns>The added TimeLineItem object.</returns>
        Task<TimeLineItem> AddTimeLineItem(TimeLineItem timeLineItem);

        /// <summary>
        /// Updates a TimeLineItem. The TimeLineItem with the same TimeLineId will be updated.
        /// </summary>
        /// <param name="timeLineItem">The TimeLineItem with the updated properties.</param>
        /// <returns>The updated TimeLineItem object.</returns>
        Task<TimeLineItem> UpdateTimeLineItem(TimeLineItem timeLineItem);

        /// <summary>
        /// Deletes the TimeLineItem with the given TimeLineId
        /// </summary>
        /// <param name="timeLineItemId">The TimeLineId of the TimeLineItem to delete.</param>
        /// <returns>bool: True if the TimeLineItem was successfully removed.</returns>
        Task<bool> DeleteTimeLineItem(int timeLineItemId);

        /// <summary>
        /// Gets a list of all TimeLineItems for a Progeny that a user has access to.
        /// </summary>
        /// <param name="progenyId">The Id of the progeny to get TimeLineItems for.</param>
        /// <param name="accessLevel">The user's access level for the Progeny.</param>
        /// <param name="order">Sort order: 0 for ascending, 1 for descending</param>
        /// <returns>List of TimeLineItem objects.</returns>
        Task<List<TimeLineItem>> GetTimeline(int progenyId, int accessLevel, int order);

        /// <summary>
        /// Gets data for the OnThisDay page.
        /// </summary>
        /// <param name="onThisDayRequest">OnThisDayRequest object with the parameters for the OnThisDay Page.</param>
        /// <returns>OnThisDayResponse object.</returns>
        Task<OnThisDayResponse> GetOnThisDayTimeLineItems(OnThisDayRequest onThisDayRequest);
    }
}
