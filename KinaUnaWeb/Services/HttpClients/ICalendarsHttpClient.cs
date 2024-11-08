using System.Collections.Generic;
using System.Threading.Tasks;
using KinaUna.Data.Models;

namespace KinaUnaWeb.Services.HttpClients
{
    /// <summary>
    /// Provides methods for interacting with the Calendar API.
    /// </summary>
    public interface ICalendarsHttpClient
    {
        /// <summary>
        /// Gets a CalendarItem with a given EventId.
        /// </summary>
        /// <param name="eventId">The EventId of the CalendarItem object.</param>
        /// <returns>CalendarItem. Start and end times are in UTC timezone.</returns>
        Task<CalendarItem> GetCalendarItem(int eventId);

        /// <summary>
        /// Adds a new CalendarItem object.
        /// </summary>
        /// <param name="eventItem">The CalendarItem object to be added. Start and end times should be in UTC timezone.</param>
        /// <returns>The CalendarItem object that was added. Start and end times are in UTC timezone.</returns>
        Task<CalendarItem> AddCalendarItem(CalendarItem eventItem);

        /// <summary>
        /// Updates a CalendarItem object.
        /// </summary>
        /// <param name="eventItem">The CalendarItem object to be updated. Start and end times should be in UTC timezone.</param>
        /// <returns>The updated CalendarItem object. Start and end times are in UTC timezone.</returns>
        Task<CalendarItem> UpdateCalendarItem(CalendarItem eventItem);

        /// <summary>
        /// Removes the CalendarItem object with a given EventId.
        /// </summary>
        /// <param name="eventId">The EventId of the CalendarItem to remove.</param>
        /// <returns>bool: True if the CalendarItem object was successfully removed.</returns>
        Task<bool> DeleteCalendarItem(int eventId);
        
        /// <summary>
        /// Gets the list of CalendarItem objects for a progeny that a user has access to.
        /// </summary>
        /// <param name="progenyIds">The list of Ids of the Progenies to get the list of CalendarItems for.</param>
        /// <returns>List of CalendarItem objects. Start and end times are in UTC timezone.</returns>
        Task<List<CalendarItem>> GetProgeniesCalendarList(List<int> progenyIds);
    }
}
