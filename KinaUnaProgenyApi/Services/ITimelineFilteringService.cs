using KinaUna.Data.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace KinaUnaProgenyApi.Services
{
    /// <summary>
    /// Filters TimeLineItems based on tags, categories, contexts and keywords.
    /// </summary>
    public interface ITimelineFilteringService
    {
        /// <summary>
        /// Filters TimeLineItems based on tags.
        /// </summary>
        /// <param name="timeLineItems">The list of items to filter.</param>
        /// <param name="tags">Comma separated list of tags.</param>
        /// <returns>List of TimeLineItems that contain any of the tags.</returns>
        Task<List<TimeLineItem>> GetTimeLineItemsWithTags(List<TimeLineItem> timeLineItems, string tags);
        
        /// <summary>
        /// Filters TimeLineItems based on categories.
        /// </summary>
        /// <param name="timeLineItems">The list of items to filter.</param>
        /// <param name="categories">Comma separated list of categories.</param>
        /// <returns>List of TimeLineItems that contain any of the categories</returns>
        Task<List<TimeLineItem>> GetTimeLineItemsWithCategories(List<TimeLineItem> timeLineItems, string categories);

        /// <summary>
        /// Filters TimeLineItems based on contexts.
        /// </summary>
        /// <param name="timeLineItems">The list of items to filter.</param>
        /// <param name="contexts">Comma separated list of contexts</param>
        /// <returns>List of TimeLineItems that contain any of the contexts.</returns>
        Task<List<TimeLineItem>> GetTimeLineItemsWithContexts(List<TimeLineItem> timeLineItems, string contexts);

        /// <summary>
        /// Filters TimeLineItems based on keywords.
        /// </summary>
        /// <param name="timeLineItems">The list of items to filter.</param>
        /// <param name="keywords">Comma separated list of keywords.</param>
        /// <returns>List of TimeLineItems that contain any of the keywords.</returns>
        Task<List<TimeLineItem>> GetTimeLineItemsWithKeyword(List<TimeLineItem> timeLineItems, string keywords);
    }
}