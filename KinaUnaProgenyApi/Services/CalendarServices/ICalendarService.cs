using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using KinaUna.Data.Models;

namespace KinaUnaProgenyApi.Services.CalendarServices
{
    public interface ICalendarService
    {
        /// <summary>
        /// Get a CalendarItem from the cache.
        /// If it isn't in the cache gets it from the database.
        /// If it doesn't exist in the database, returns null.
        /// </summary>
        /// <param name="id">The CalendarItem's EventId</param>
        /// <returns>CalendarItem if it exists, null if it doesn't exist.</returns>
        Task<CalendarItem> GetCalendarItem(int id);

        /// <summary>
        /// Add a new CalendarItem to the database.
        /// </summary>
        /// <param name="item">The CalendarItem to add.</param>
        /// <returns>The added CalendarItem.</returns>
        Task<CalendarItem> AddCalendarItem(CalendarItem item);

        /// <summary>
        /// Updates a CalendarItem in the database and sets the updated item in the cache.
        /// </summary>
        /// <param name="item">The CalendarItem with the updated properties.</param>
        /// <returns>The updated CalendarItem.</returns>
        Task<CalendarItem> UpdateCalendarItem(CalendarItem item);

        /// <summary>
        /// Deletes a CalendarItem from the database and removes it from the cache.
        /// </summary>
        /// <param name="item">The CalendarItem to delete.</param>
        /// <returns>The deleted CalendarItem.</returns>
        Task<CalendarItem> DeleteCalendarItem(CalendarItem item);

        /// <summary>
        /// Gets a List of all CalendarItems for a Progeny from the cache.
        /// If the list isn't found in the cache, gets it from the database and sets it in the cache.
        /// </summary>
        /// <param name="progenyId">The ProgenyId of the Progeny to get all CalendarItems for.</param>
        /// <param name="accessLevel">The required access level to view the event.</param>
        /// <param name="start">Optional start date for the list.</param>
        /// <param name="end">Optional end date for the list.</param>
        /// <returns>List of CalendarItems.</returns>
        Task<List<CalendarItem>> GetCalendarList(int progenyId, int accessLevel, DateTime? start = null, DateTime? end = null);

        /// <summary>
        /// Gets the list of CalendarItems for a Progeny that are recurring events on this day.
        /// Only includes items after 1900.
        /// </summary>
        /// <param name="progenyId">The id of the Progeny to get items for.</param>
        /// <returns>List of CalendarItems.</returns>
        Task<List<CalendarItem>> GetRecurringCalendarItemsOnThisDay(int progenyId);

        /// <summary>
        /// Gets the list of CalendarItems for a Progeny that are recurring events for the latest posts list.
        /// Only includes items after 1900.
        /// </summary>
        /// <param name="progenyId">The id of the Progeny to get items for.</param>
        /// <returns>List of CalendarItems.</returns>
        Task<List<CalendarItem>> GetRecurringCalendarItemsLatestPosts(int progenyId);
        
        /// <summary>
        /// Gets a list of CalendarItems for a Progeny with a specific context.
        /// </summary>
        /// <param name="progenyId">The id of the Progeny.</param>
        /// <param name="context">String with the context to look for, any item containing the string in the context property, not case-sensitive.</param>
        /// <param name="accessLevel">The required access level to view the event.</param>
        /// <returns>List of CalendarItems.</returns>
        Task<List<CalendarItem>> GetCalendarItemsWithContext(int progenyId, string context, int accessLevel);

        /// <summary>
        /// Assigns UIds to all CalendarItems that don't have one.
        /// </summary>
        /// <returns></returns>
        Task CheckCalendarItemsForUId();
    }
}
