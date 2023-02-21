﻿using KinaUna.Data.Models;
using System;

namespace KinaUna.Data.Extensions
{
    public static class TimeLineItemExtensions
    {
        public static void CopyPropertiesForUpdate(this TimeLineItem currentTimeLineItem, TimeLineItem otherTimeLineItem)
        {
            currentTimeLineItem.ProgenyId = otherTimeLineItem.ProgenyId;
            currentTimeLineItem.AccessLevel = otherTimeLineItem.AccessLevel;
            currentTimeLineItem.CreatedBy = otherTimeLineItem.CreatedBy;
            currentTimeLineItem.CreatedTime = otherTimeLineItem.CreatedTime;
            currentTimeLineItem.ItemId = otherTimeLineItem.ItemId;
            currentTimeLineItem.ItemType = otherTimeLineItem.ItemType;
            currentTimeLineItem.ProgenyTime = otherTimeLineItem.ProgenyTime;
        }

        public static void CopyPropertiesForAdd(this TimeLineItem currentTimeLineItem, TimeLineItem otherTimeLineItem)
        {
            currentTimeLineItem.ProgenyId = otherTimeLineItem.ProgenyId;
            currentTimeLineItem.AccessLevel = otherTimeLineItem.AccessLevel;
            currentTimeLineItem.CreatedBy = otherTimeLineItem.CreatedBy;
            currentTimeLineItem.CreatedTime = otherTimeLineItem.CreatedTime;
            currentTimeLineItem.ItemId = otherTimeLineItem.ItemId;
            currentTimeLineItem.ItemType = otherTimeLineItem.ItemType;
            currentTimeLineItem.ProgenyTime = otherTimeLineItem.ProgenyTime;
        }

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

        public static bool CopyContactItemPropertiesForUpdate(this TimeLineItem currentTimeLineItem, Contact contactItem)
        {
            if (contactItem.DateAdded.HasValue)
            {
                currentTimeLineItem.ProgenyTime = contactItem.DateAdded.Value;
                currentTimeLineItem.AccessLevel = contactItem.AccessLevel;
                
                return true;
            }

            return false;
        }

        public static void CopyContactPropertiesForAdd(this TimeLineItem currentTimeLineItem, Contact contactItem)
        {
            currentTimeLineItem.ProgenyId = contactItem.ProgenyId;
            currentTimeLineItem.AccessLevel = contactItem.AccessLevel;
            currentTimeLineItem.ItemType = (int)KinaUnaTypes.TimeLineType.Contact;
            currentTimeLineItem.ItemId = contactItem.ContactId.ToString();
            currentTimeLineItem.CreatedBy = contactItem.Author;
            currentTimeLineItem.CreatedTime = DateTime.UtcNow;
            if (contactItem.DateAdded.HasValue)
            {
                currentTimeLineItem.ProgenyTime = contactItem.DateAdded.Value;
            }
            else
            {
                currentTimeLineItem.ProgenyTime = DateTime.UtcNow;
            }
        }

        public static bool CopyFriendItemPropertiesForUpdate(this TimeLineItem currentTimeLineItem, Friend friendItem)
        {
            if (friendItem.FriendSince.HasValue)
            {
                currentTimeLineItem.ProgenyTime = friendItem.FriendSince.Value;
                currentTimeLineItem.AccessLevel = friendItem.AccessLevel;

                return true;
            }

            return false;
        }

        public static void CopyFriendPropertiesForAdd(this TimeLineItem currentTimeLineItem, Friend friendItem)
        {
            currentTimeLineItem.ProgenyId = friendItem.ProgenyId;
            currentTimeLineItem.AccessLevel = friendItem.AccessLevel;
            currentTimeLineItem.ItemType = (int)KinaUnaTypes.TimeLineType.Friend;
            currentTimeLineItem.ItemId = friendItem.FriendId.ToString();
            currentTimeLineItem.CreatedBy = friendItem.Author;
            currentTimeLineItem.CreatedTime = DateTime.UtcNow;
            
            if (friendItem.FriendSince != null)
            {
                currentTimeLineItem.ProgenyTime = friendItem.FriendSince.Value;
            }
            else
            {
                currentTimeLineItem.ProgenyTime = DateTime.UtcNow;
            }
        }

        public static void CopyLocationPropertiesForUpdate(this TimeLineItem currentTimeLineItem, Location location)
        {
            if (location.Date.HasValue)
            {
                currentTimeLineItem.ProgenyTime = location.Date.Value;
            }

            currentTimeLineItem.AccessLevel = location.AccessLevel;
        }

        public static void CopyLocationPropertiesForAdd(this TimeLineItem currentTimeLineItem, Location location)
        {
            currentTimeLineItem.ProgenyId = location.ProgenyId;
            currentTimeLineItem.AccessLevel = location.AccessLevel;
            currentTimeLineItem.ItemType = (int)KinaUnaTypes.TimeLineType.Location;
            currentTimeLineItem.ItemId = location.LocationId.ToString();
            currentTimeLineItem.CreatedBy = location.Author;
            currentTimeLineItem.CreatedTime = DateTime.UtcNow;
            if (location.Date.HasValue)
            {
                currentTimeLineItem.ProgenyTime = location.Date.Value;
            }
            else
            {
                currentTimeLineItem.ProgenyTime = DateTime.UtcNow;
            }
        }

        public static void CopyMeasurementPropertiesForAdd(this TimeLineItem currentTimeLineItem, Measurement measurement)
        {
            currentTimeLineItem.ProgenyId = measurement.ProgenyId;
            currentTimeLineItem.AccessLevel = measurement.AccessLevel;
            currentTimeLineItem.ItemType = (int)KinaUnaTypes.TimeLineType.Measurement;
            currentTimeLineItem.ItemId = measurement.MeasurementId.ToString();
            currentTimeLineItem.CreatedBy = measurement.Author;
            currentTimeLineItem.CreatedTime = DateTime.UtcNow;
            currentTimeLineItem.ProgenyTime = measurement.Date;
        }

        public static void CopyMeasurementPropertiesForUpdate(this TimeLineItem currentTimeLineItem, Measurement measurement)
        {
            currentTimeLineItem.ProgenyTime = measurement.Date;
            currentTimeLineItem.AccessLevel = measurement.AccessLevel;
        }

        public static void CopyNotePropertiesForAdd(this TimeLineItem currentTimeLineItem, Note note)
        {
            currentTimeLineItem.ProgenyId = note.ProgenyId;
            currentTimeLineItem.AccessLevel = note.AccessLevel;
            currentTimeLineItem.ItemType = (int)KinaUnaTypes.TimeLineType.Note;
            currentTimeLineItem.ItemId = note.NoteId.ToString();
            currentTimeLineItem.CreatedBy = note.Owner;
            currentTimeLineItem.CreatedTime = note.CreatedDate;
            currentTimeLineItem.ProgenyTime = note.CreatedDate;
        }

        public static void CopyNotePropertiesForUpdate(this TimeLineItem currentTimeLineItem, Note note)
        {
            currentTimeLineItem.ProgenyTime = note.CreatedDate;
            currentTimeLineItem.AccessLevel = note.AccessLevel;
        }
    }
}
