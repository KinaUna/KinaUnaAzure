using KinaUna.Data.Models;
using System;

namespace KinaUna.Data.Extensions
{
    public static class TimeLineItemExtensions
    {
        public static bool CopyCalendarItemPropertiesForUpdate(this TimeLineItem currentTimeLineItem, CalendarItem calendarItem )
        {
            if (currentTimeLineItem != null && calendarItem.StartTime.HasValue && calendarItem.EndTime.HasValue)
            {
                currentTimeLineItem.ProgenyTime = calendarItem.StartTime.Value;
                currentTimeLineItem.AccessLevel = calendarItem.AccessLevel;
                return true;
            }

            return false;
        }

        public static void CopyCalendarItemPropertiesForAdd(this TimeLineItem currentTimeLineItem, CalendarItem calendarItem, string userEmail)
        {
            currentTimeLineItem.ProgenyId = calendarItem.ProgenyId;
            currentTimeLineItem.AccessLevel = calendarItem.AccessLevel;
            currentTimeLineItem.ItemType = (int)KinaUnaTypes.TimeLineType.Calendar;
            currentTimeLineItem.ItemId = calendarItem.EventId.ToString();
            currentTimeLineItem.CreatedBy = userEmail;
            currentTimeLineItem.CreatedTime = DateTime.UtcNow;
            if (calendarItem.StartTime != null)
            {
                currentTimeLineItem.ProgenyTime = calendarItem.StartTime.Value;
            }
            else
            {
                currentTimeLineItem.ProgenyTime = DateTime.UtcNow;
            }
        }

        public static void CopyUserAccessItemPropertiesForAdd(this TimeLineItem currentTimeLineItem, UserAccess userAccessItem)
        {
            currentTimeLineItem.ProgenyId = userAccessItem.ProgenyId;
            currentTimeLineItem.AccessLevel = 0;
            currentTimeLineItem.ItemId = userAccessItem.AccessId.ToString();
            currentTimeLineItem.ItemType = (int)KinaUnaTypes.TimeLineType.UserAccess;
        }

        public static void CopyUserAccessItemPropertiesForUpdate(this TimeLineItem currentTimeLineItem, UserAccess userAccessItem)
        {
            currentTimeLineItem.ProgenyId = userAccessItem.ProgenyId;
            currentTimeLineItem.AccessLevel = 0;
            currentTimeLineItem.ItemId = userAccessItem.AccessId.ToString();
            currentTimeLineItem.ItemType = (int)KinaUnaTypes.TimeLineType.UserAccess;
        }
    }
}
