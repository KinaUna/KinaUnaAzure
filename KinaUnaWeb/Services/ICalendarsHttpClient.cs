using System.Collections.Generic;
using System.Threading.Tasks;
using KinaUna.Data.Models;

namespace KinaUnaWeb.Services
{
    public interface ICalendarsHttpClient
    {
                /// <summary>
        /// Gets a CalendarItem with a given EventId.
        /// </summary>
        /// <param name="eventId">int: The Id of the CalendarItem object (CalendarItem.EventId).</param>
        /// <returns>CalendarItem</returns>
        Task<CalendarItem> GetCalendarItem(int eventId);

        /// <summary>
        /// Adds a new CalendarItem object.
        /// </summary>
        /// <param name="eventItem">CalendarItem: The new CalendarItem object to be added.</param>
        /// <returns>CalendarItem: The CalendarItem object that was added.</returns>
        Task<CalendarItem> AddCalendarItem(CalendarItem eventItem);

        /// <summary>
        /// Updates a CalendarItem object. The CalendarItem with the same EventId will be updated.
        /// </summary>
        /// <param name="eventItem">CalendarItem: The CalendarItem object to be updated.</param>
        /// <returns>CalendarItem: The updated CalendarItem object.</returns>
        Task<CalendarItem> UpdateCalendarItem(CalendarItem eventItem);

        /// <summary>
        /// Removes the CalendarItem object with a given EventId.
        /// </summary>
        /// <param name="eventId">int: The Id of the CalendarItem to remove.</param>
        /// <returns>bool: True if the CalendarItem object was successfully removed.</returns>
        Task<bool> DeleteCalendarItem(int eventId);

        /// <summary>
        /// Gets the list of CalendarItem objects for a progeny that a user has access to.
        /// </summary>
        /// <param name="progenyId">int: The id of the progeny (Progeny.Id).</param>
        /// <param name="accessLevel">int: The user's access level.</param>
        /// <returns>List of CalendarItem objects.</returns>
        Task<List<CalendarItem>> GetCalendarList(int progenyId, int accessLevel);

        /// <summary>
        /// Gets the next 5 upcoming events in the progeny's calendar.
        /// </summary>
        /// <param name="progenyId">int: The Id of the progeny (Progeny.Id).</param>
        /// <param name="accessLevel">int: The user's access level.</param>
        /// <returns>List of CalendarItem objects.</returns>
        Task<List<CalendarItem>> GetUpcomingEvents(int progenyId, int accessLevel);
    }
}
