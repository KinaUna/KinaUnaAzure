using System;
using KinaUna.Data.Models;

namespace KinaUna.Data.Extensions
{
    /// <summary>
    /// Extension methods for the CalendarItem class.
    /// </summary>
    public static class CalendarItemExtensions
    {
        /// <summary>
        /// Copies the properties needed for updating a CalendarItem entity from one CalendarItem object to another.
        /// </summary>
        /// <param name="currentCalendarItem"></param>
        /// <param name="otherCalendarItem"></param>
        public static void CopyPropertiesForRecurringEvent(this CalendarItem currentCalendarItem, CalendarItem otherCalendarItem)
        {
            currentCalendarItem.EventId = otherCalendarItem.EventId;
            currentCalendarItem.UId = otherCalendarItem.UId;
            currentCalendarItem.Author = otherCalendarItem.Author;
            currentCalendarItem.AccessLevel = otherCalendarItem.AccessLevel;
            currentCalendarItem.AllDay = otherCalendarItem.AllDay;
            currentCalendarItem.Context = otherCalendarItem.Context;
            currentCalendarItem.EndString = otherCalendarItem.EndString;
            currentCalendarItem.EndTime = otherCalendarItem.EndTime;
            currentCalendarItem.StartTime = otherCalendarItem.StartTime;
            currentCalendarItem.IsReadonly = otherCalendarItem.IsReadonly;
            currentCalendarItem.Location = otherCalendarItem.Location;
            currentCalendarItem.Notes = otherCalendarItem.Notes;
            currentCalendarItem.ProgenyId = otherCalendarItem.ProgenyId;
            currentCalendarItem.StartString = otherCalendarItem.StartString;
            currentCalendarItem.Title = otherCalendarItem.Title;
            currentCalendarItem.Progeny = otherCalendarItem.Progeny;
            currentCalendarItem.RecurrenceRuleId = otherCalendarItem.RecurrenceRuleId;
            currentCalendarItem.RecurrenceRule = otherCalendarItem.RecurrenceRule;
        }

        /// <summary>
        /// Copies the properties needed for updating a CalendarItem entity from one CalendarItem object to another.
        /// </summary>
        /// <param name="currentCalendarItem"></param>
        /// <param name="otherCalendarItem"></param>
        public static void CopyPropertiesForUpdate(this CalendarItem currentCalendarItem, CalendarItem otherCalendarItem )
        {
            currentCalendarItem.UId = otherCalendarItem.UId;
            currentCalendarItem.Author = otherCalendarItem.Author;
            currentCalendarItem.AccessLevel = otherCalendarItem.AccessLevel;
            currentCalendarItem.AllDay = otherCalendarItem.AllDay;
            currentCalendarItem.Context = otherCalendarItem.Context;
            currentCalendarItem.EndString = otherCalendarItem.EndString;
            currentCalendarItem.EndTime = otherCalendarItem.EndTime;
            currentCalendarItem.StartTime = otherCalendarItem.StartTime;
            currentCalendarItem.IsReadonly = otherCalendarItem.IsReadonly;
            currentCalendarItem.Location = otherCalendarItem.Location;
            currentCalendarItem.Notes = otherCalendarItem.Notes;
            currentCalendarItem.ProgenyId = otherCalendarItem.ProgenyId;
            currentCalendarItem.StartString = otherCalendarItem.StartString;
            currentCalendarItem.Title = otherCalendarItem.Title;
            currentCalendarItem.Progeny = otherCalendarItem.Progeny;
            currentCalendarItem.RecurrenceRuleId = otherCalendarItem.RecurrenceRuleId;
            currentCalendarItem.RecurrenceRule = otherCalendarItem.RecurrenceRule;
        }

        /// <summary>
        /// Copies the properties needed for adding a CalendarItem entity from one CalendarItem object to another.
        /// </summary>
        /// <param name="currentCalendarItem"></param>
        /// <param name="otherCalendarItem"></param>
        public static void CopyPropertiesForAdd(this CalendarItem currentCalendarItem, CalendarItem otherCalendarItem)
        {
            currentCalendarItem.UId = otherCalendarItem.UId;
            currentCalendarItem.AccessLevel = otherCalendarItem.AccessLevel;
            currentCalendarItem.Author = otherCalendarItem.Author;
            currentCalendarItem.Notes = otherCalendarItem.Notes;
            currentCalendarItem.ProgenyId = otherCalendarItem.ProgenyId;
            currentCalendarItem.AllDay = otherCalendarItem.AllDay;
            currentCalendarItem.Context = otherCalendarItem.Context;
            currentCalendarItem.Location = otherCalendarItem.Location;
            currentCalendarItem.Title = otherCalendarItem.Title;
            currentCalendarItem.StartTime = otherCalendarItem.StartTime;
            currentCalendarItem.EndTime = otherCalendarItem.EndTime;
        }

        /// <summary>
        /// Creates a new TimeLineItem from a CalendarItem.
        /// For use when a new CalendarItem is added.
        /// </summary>
        /// <param name="calendarItem"></param>
        /// <returns></returns>
        public static TimeLineItem ToNewTimeLineItem(this CalendarItem calendarItem)
        {
            TimeLineItem timeLineItem = new()
            {
                ItemId = calendarItem.EventId.ToString(),
                ProgenyId = calendarItem.ProgenyId,
                AccessLevel = calendarItem.AccessLevel,
                ItemType = (int)KinaUnaTypes.TimeLineType.Calendar,
                CreatedBy = calendarItem.Author,
                CreatedTime = DateTime.UtcNow,
                ProgenyTime = calendarItem.StartTime ?? DateTime.UtcNow
            };

            return timeLineItem;
        }

        public static bool IsBeforeEnd(this RecurrenceRule rule, DateTime date, int count)
        {
            if (rule.EndOption == 0) return true;

            if (rule.IsDateWithinRuleEnd(date) && rule.IsCountWithinRuleEnd(count)) return true;
            
            return false;
        }

        private static bool IsDateWithinRuleEnd(this RecurrenceRule rule, DateTime date)
        {
            if (rule.EndOption == 1 && rule.Until.HasValue) return rule.Until.Value >= date;
            return true;
        }

        private static bool IsCountWithinRuleEnd(this RecurrenceRule rule, int count)
        {
            if (rule.EndOption == 2 && rule.Count > 0) return rule.Count > count;
            return true;
        }
    }
}
