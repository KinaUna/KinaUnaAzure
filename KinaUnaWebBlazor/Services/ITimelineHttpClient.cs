using KinaUna.Data.Models;

namespace KinaUnaWebBlazor.Services
{
    public interface ITimelineHttpClient
    {
        /// <summary>
        /// Gets the TimeLineItem with the given type and the Id (Not the TimeLineItem.TimeLineId, but the type's own Id property).
        /// </summary>
        /// <param name="itemId">int: The item's Id. That is PictureId, VideoId, NoteId, WordId, etc.</param>
        /// <param name="itemType">int: The type of the item. Defined in the KinaUnaTypes.TimeLineType enum.</param>
        /// <returns>TimeLineItem</returns>
        Task<TimeLineItem?> GetTimeLineItem(string itemId, int itemType);

        /// <summary>
        /// Adds a new TimeLineItem.
        /// </summary>
        /// <param name="timeLineItem">TimeLineItem: The new TimeLineItem to add.</param>
        /// <returns>TimeLineItem</returns>
        Task<TimeLineItem?> AddTimeLineItem(TimeLineItem? timeLineItem);

        /// <summary>
        /// Updates a TimeLineItem. The TimeLineItem with the same TimeLineId will be updated.
        /// </summary>
        /// <param name="timeLineItem">TimeLineItem: The TimeLineItem to update.</param>
        /// <returns>TimeLineItem: The updated TimeLineItem.</returns>
        Task<TimeLineItem?> UpdateTimeLineItem(TimeLineItem? timeLineItem);

        /// <summary>
        /// Removes the TimeLineItem with the given TimeLineId
        /// </summary>
        /// <param name="timeLineItemId">int: The TimeLineId of the TimeLineItem to remove (TimeLineItem.TimeLineId).</param>
        /// <returns>bool: True if the TimeLineItem was successfully removed.</returns>
        Task<bool> DeleteTimeLineItem(int timeLineItemId);

        /// <summary>
        /// Gets a progeny's list of TimeLineItems that a user has access to.
        /// </summary>
        /// <param name="progenyId">int: The Id of the progeny (Progeny.Id).</param>
        /// <param name="accessLevel">int: The user's access level.</param>
        /// <returns></returns>
        Task<List<TimeLineItem>?> GetTimeline(int progenyId, int accessLevel);
    }
}
