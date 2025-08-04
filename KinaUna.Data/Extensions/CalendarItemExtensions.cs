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

        /// <summary>
        /// Determines whether the specified date and count are within the bounds of the recurrence rule's end
        /// conditions.
        /// </summary>
        /// <param name="rule">The recurrence rule to evaluate.</param>
        /// <param name="date">The date to check against the rule's end conditions.</param>
        /// <param name="count">The occurrence count to check against the rule's end conditions.</param>
        /// <returns><see langword="true"/> if the specified date and count are before the end of the recurrence rule; otherwise,
        /// <see langword="false"/>.</returns>
        public static bool IsBeforeEnd(this RecurrenceRule rule, DateTime date, int count)
        {
            if (rule.EndOption == 0) return true;

            if (rule.IsDateWithinRuleEnd(date) && rule.IsCountWithinRuleEnd(count)) return true;
            
            return false;
        }

        /// <summary>
        /// Determines whether the specified date falls within the end boundary of the recurrence rule.
        /// </summary>
        /// <param name="rule">The recurrence rule to evaluate.</param>
        /// <param name="date">The date to check against the rule's end boundary.</param>
        /// <returns><see langword="true"/> if the date is within the rule's end boundary or if the rule has no defined end
        /// boundary; otherwise, <see langword="false"/>.</returns>
        private static bool IsDateWithinRuleEnd(this RecurrenceRule rule, DateTime date)
        {
            if (rule.EndOption == 1 && rule.Until.HasValue) return rule.Until.Value >= date;
            return true;
        }

        /// <summary>
        /// Determines whether the specified count is within the limit defined by the recurrence rule's end option.
        /// </summary>
        /// <param name="rule">The recurrence rule that defines the end condition.</param>
        /// <param name="count">The count to evaluate against the rule's end condition.</param>
        /// <returns><see langword="true"/> if the count is within the limit defined by the rule's end option; otherwise, <see
        /// langword="false"/>. Always returns <see langword="true"/> if the rule does not specify a count limit.</returns>
        private static bool IsCountWithinRuleEnd(this RecurrenceRule rule, int count)
        {
            if (rule.EndOption == 2 && rule.Count > 0) return rule.Count > count;
            return true;
        }
    }
}
