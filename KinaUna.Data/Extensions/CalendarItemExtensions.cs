using KinaUna.Data.Models;

namespace KinaUna.Data.Extensions
{
    public static class CalendarItemExtensions
    {
        public static void CopyPropertiesForUpdate(this CalendarItem currentCalendarItem, CalendarItem otherCalendarItem )
        {
            currentCalendarItem.Author = otherCalendarItem.Author;
            currentCalendarItem.AccessLevel = otherCalendarItem.AccessLevel;
            currentCalendarItem.AllDay = otherCalendarItem.AllDay;
            currentCalendarItem.Context = otherCalendarItem.Context;
            currentCalendarItem.End = otherCalendarItem.End;
            currentCalendarItem.EndString = otherCalendarItem.EndString;
            currentCalendarItem.EndTime = otherCalendarItem.EndTime;
            currentCalendarItem.StartTime = otherCalendarItem.StartTime;
            currentCalendarItem.IsReadonly = otherCalendarItem.IsReadonly;
            currentCalendarItem.Location = otherCalendarItem.Location;
            currentCalendarItem.Notes = otherCalendarItem.Notes;
            currentCalendarItem.ProgenyId = otherCalendarItem.ProgenyId;
            currentCalendarItem.Start = otherCalendarItem.Start;
            currentCalendarItem.StartString = otherCalendarItem.StartString;
            currentCalendarItem.Title = otherCalendarItem.Title;
            currentCalendarItem.Progeny = otherCalendarItem.Progeny;
        }

        public static void CopyPropertiesForAdd(this CalendarItem currentCalendarItem, CalendarItem otherCalendarItem)
        {
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
    }
}
