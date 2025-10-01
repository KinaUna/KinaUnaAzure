using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using KinaUna.Data.Models;

namespace KinaUnaProgenyApi.Services.CalendarServices;

public interface ICalendarRecurrencesService
{
    /// <summary>
    /// Gets a list of CalendarItems generated from recurring events for a Progeny.
    /// </summary>
    /// <param name="progenyId"></param>
    /// <param name="familyId"></param>
    /// <param name="start">DateTime with the start date. Results include this day.</param>
    /// <param name="end">DateTime with the end date. Results include this day.</param>
    /// <param name="includeOriginal">Include the original event in the list.</param>
    /// <param name="currentUserInfo"></param>
    /// <returns>List of CalendarItems</returns>
    Task<List<CalendarItem>> GetRecurringEventsForProgenyOrFamily(int progenyId, int familyId, DateTime start, DateTime end, bool includeOriginal, UserInfo currentUserInfo);

    Task<List<CalendarItem>> GetCalendarItemsForRecurrenceRule(RecurrenceRule recurrenceRule, DateTime start, DateTime end, bool includeOriginal, UserInfo currentUserInfo);
}