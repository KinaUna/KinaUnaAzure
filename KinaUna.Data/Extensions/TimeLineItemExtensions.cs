using KinaUna.Data.Models;
using System;

namespace KinaUna.Data.Extensions
{
    /// <summary>
    /// Extension methods for the TimeLineItem class.
    /// </summary>
    public static class TimeLineItemExtensions
    {
        /// <summary>
        /// Copies the properties needed for updating a TimeLineItem entity from one TimeLineItem object to another.
        /// </summary>
        /// <param name="currentTimeLineItem"></param>
        /// <param name="otherTimeLineItem"></param>
        public static void CopyPropertiesForUpdate(this TimeLineItem currentTimeLineItem, TimeLineItem otherTimeLineItem)
        {
            currentTimeLineItem.AccessLevel = otherTimeLineItem.AccessLevel;
            currentTimeLineItem.ProgenyTime = otherTimeLineItem.ProgenyTime;
        }

        /// <summary>
        /// Copies the properties needed for adding a TimeLineItem entity from one TimeLineItem object to another.
        /// </summary>
        /// <param name="currentTimeLineItem"></param>
        /// <param name="otherTimeLineItem"></param>
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

        /// <summary>
        /// Copies the properties needed for updating the timeline item when a calendar item has been updated.
        /// </summary>
        /// <param name="currentTimeLineItem"></param>
        /// <param name="calendarItem"></param>
        /// <returns>bool: True if the calendar item has valid data</returns>
        public static bool CopyCalendarItemPropertiesForUpdate(this TimeLineItem currentTimeLineItem, CalendarItem calendarItem )
        {
            if (currentTimeLineItem == null || !calendarItem.StartTime.HasValue || !calendarItem.EndTime.HasValue) return false;

            currentTimeLineItem.ProgenyTime = calendarItem.StartTime.Value;
            currentTimeLineItem.AccessLevel = calendarItem.AccessLevel;
            return true;

        }

        /// <summary>
        /// Copies the properties needed for adding a timeline item when a calendar item has been added.
        /// </summary>
        /// <param name="currentTimeLineItem"></param>
        /// <param name="calendarItem"></param>
        /// <param name="userEmail"></param>
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

        /// <summary>
        /// Copies the properties needed for updating the timeline item when user access has been added.
        /// This should not be used to add a timeline item to the database, but for notifications only.
        /// </summary>
        /// <param name="currentTimeLineItem"></param>
        /// <param name="userAccessItem"></param>
        public static void CopyUserAccessItemPropertiesForAdd(this TimeLineItem currentTimeLineItem, UserAccess userAccessItem)
        {
            currentTimeLineItem.ProgenyId = userAccessItem.ProgenyId;
            currentTimeLineItem.AccessLevel = userAccessItem.AccessLevel;
            currentTimeLineItem.ItemId = userAccessItem.AccessId.ToString();
            currentTimeLineItem.ItemType = (int)KinaUnaTypes.TimeLineType.UserAccess;
        }

        /// <summary>
        /// Copies the properties needed for updating the timeline item when user access has been updated.
        /// This should not be used to add a timeline item to the database, but for notifications only.
        /// </summary>
        /// <param name="currentTimeLineItem"></param>
        /// <param name="userAccessItem"></param>
        public static void CopyUserAccessItemPropertiesForUpdate(this TimeLineItem currentTimeLineItem, UserAccess userAccessItem)
        {
            currentTimeLineItem.ProgenyId = userAccessItem.ProgenyId;
            currentTimeLineItem.AccessLevel = userAccessItem.AccessLevel;
            currentTimeLineItem.ItemType = (int)KinaUnaTypes.TimeLineType.UserAccess;
            currentTimeLineItem.ItemId = userAccessItem.AccessId.ToString();

        }

        /// <summary>
        /// Copies the properties needed for updating the timeline item when a contact item has been updated.
        /// </summary>
        /// <param name="currentTimeLineItem"></param>
        /// <param name="contactItem"></param>
        /// <returns>bool: True if the contact object has valid data.</returns>
        public static bool CopyContactItemPropertiesForUpdate(this TimeLineItem currentTimeLineItem, Contact contactItem)
        {
            if (!contactItem.DateAdded.HasValue) return false;

            currentTimeLineItem.ProgenyTime = contactItem.DateAdded.Value;
            currentTimeLineItem.AccessLevel = contactItem.AccessLevel;
                
            return true;

        }

        /// <summary>
        /// Copies the properties needed for adding a timeline item when a contact item has been added.
        /// </summary>
        /// <param name="currentTimeLineItem"></param>
        /// <param name="contactItem"></param>
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

        /// <summary>
        /// Copies the properties needed for updating the timeline item when a friend item has been updated.
        /// </summary>
        /// <param name="currentTimeLineItem"></param>
        /// <param name="friendItem"></param>
        /// <returns>bool: True if the friend object has valid data.</returns>
        public static bool CopyFriendItemPropertiesForUpdate(this TimeLineItem currentTimeLineItem, Friend friendItem)
        {
            if (!friendItem.FriendSince.HasValue) return false;

            currentTimeLineItem.ProgenyTime = friendItem.FriendSince.Value;
            currentTimeLineItem.AccessLevel = friendItem.AccessLevel;

            return true;

        }

        /// <summary>
        /// Copies the properties needed for adding a timeline item when a friend item has been added.
        /// </summary>
        /// <param name="currentTimeLineItem"></param>
        /// <param name="friendItem"></param>
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

        /// <summary>
        /// Copies the properties needed for updating the timeline item when a location item has been updated.
        /// </summary>
        /// <param name="currentTimeLineItem"></param>
        /// <param name="location"></param>
        public static void CopyLocationPropertiesForUpdate(this TimeLineItem currentTimeLineItem, Location location)
        {
            if (location.Date.HasValue)
            {
                currentTimeLineItem.ProgenyTime = location.Date.Value;
            }

            currentTimeLineItem.AccessLevel = location.AccessLevel;
        }

        /// <summary>
        /// Copies the properties needed for adding a timeline item when a location item has been added.
        /// </summary>
        /// <param name="currentTimeLineItem"></param>
        /// <param name="location"></param>
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

        /// <summary>
        /// Copies the properties needed for adding the timeline item when a measurement item has been added.
        /// </summary>
        /// <param name="currentTimeLineItem"></param>
        /// <param name="measurement"></param>
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

        /// <summary>
        /// Copies the properties needed for updating the timeline item when a measurement item has been updated.
        /// </summary>
        /// <param name="currentTimeLineItem"></param>
        /// <param name="measurement"></param>
        public static void CopyMeasurementPropertiesForUpdate(this TimeLineItem currentTimeLineItem, Measurement measurement)
        {
            currentTimeLineItem.ProgenyTime = measurement.Date;
            currentTimeLineItem.AccessLevel = measurement.AccessLevel;
        }

        /// <summary>
        /// Copies the properties needed for adding the timeline item when a note item has been added.
        /// </summary>
        /// <param name="currentTimeLineItem"></param>
        /// <param name="note"></param>
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

        /// <summary>
        /// Copies the properties needed for updating the timeline item when a note item has been updated.
        /// </summary>
        /// <param name="currentTimeLineItem"></param>
        /// <param name="note"></param>
        public static void CopyNotePropertiesForUpdate(this TimeLineItem currentTimeLineItem, Note note)
        {
            currentTimeLineItem.ProgenyTime = note.CreatedDate;
            currentTimeLineItem.AccessLevel = note.AccessLevel;
        }

        /// <summary>
        /// Copies the properties needed for adding the timeline item when a picture item has been added.
        /// </summary>
        /// <param name="currentTimeLineItem"></param>
        /// <param name="picture"></param>
        public static void CopyPicturePropertiesForAdd(this TimeLineItem currentTimeLineItem, Picture picture)
        {
            currentTimeLineItem.ProgenyId = picture.ProgenyId;
            currentTimeLineItem.ItemId = picture.PictureId.ToString();
            currentTimeLineItem.ItemType = (int)KinaUnaTypes.TimeLineType.Photo;
            currentTimeLineItem.AccessLevel = picture.AccessLevel;
            if (picture.PictureTime.HasValue)
            {
                currentTimeLineItem.ProgenyTime = picture.PictureTime.Value;
            }

            currentTimeLineItem.CreatedBy = picture.Author;
            currentTimeLineItem.CreatedTime = DateTime.UtcNow;

        }

        /// <summary>
        /// Copies the properties needed for updating the timeline item when a picture item has been updated.
        /// </summary>
        /// <param name="currentTimeLineItem"></param>
        /// <param name="picture"></param>
        public static void CopyPicturePropertiesForUpdate(this TimeLineItem currentTimeLineItem, Picture picture)
        {
            currentTimeLineItem.AccessLevel = picture.AccessLevel;
            if (picture.PictureTime.HasValue)
            {
                currentTimeLineItem.ProgenyTime = picture.PictureTime.Value;
            }
        }

        /// <summary>
        /// Copies the properties needed for adding the timeline item when a video item has been added.
        /// </summary>
        /// <param name="currentTimeLineItem"></param>
        /// <param name="video"></param>
        public static void CopyVideoPropertiesForAdd(this TimeLineItem currentTimeLineItem, Video video)
        {
            currentTimeLineItem.ProgenyId = video.ProgenyId;
            currentTimeLineItem.ItemId = video.VideoId.ToString();
            currentTimeLineItem.ItemType = (int)KinaUnaTypes.TimeLineType.Video;
            currentTimeLineItem.AccessLevel = video.AccessLevel;
            if (video.VideoTime.HasValue)
            {
                currentTimeLineItem.ProgenyTime = video.VideoTime.Value;
            }

            currentTimeLineItem.CreatedBy = video.Author;
            currentTimeLineItem.CreatedTime = DateTime.UtcNow;

        }

        /// <summary>
        /// Copies the properties needed for updating the timeline item when a video item has been updated.
        /// </summary>
        /// <param name="currentTimeLineItem"></param>
        /// <param name="video"></param>
        public static void CopyVideoPropertiesForUpdate(this TimeLineItem currentTimeLineItem, Video video)
        {
            currentTimeLineItem.AccessLevel = video.AccessLevel;
            if (video.VideoTime.HasValue)
            {
                currentTimeLineItem.ProgenyTime = video.VideoTime.Value;
            }
        }

        /// <summary>
        /// Copies the properties needed for adding the timeline item when a skill item has been added.
        /// </summary>
        /// <param name="currentTimeLineItem"></param>
        /// <param name="skill"></param>
        public static void CopySkillPropertiesForAdd(this TimeLineItem currentTimeLineItem, Skill skill)
        {
            currentTimeLineItem.ProgenyId = skill.ProgenyId;
            currentTimeLineItem.AccessLevel = skill.AccessLevel;
            currentTimeLineItem.ItemType = (int)KinaUnaTypes.TimeLineType.Skill;
            currentTimeLineItem.ItemId = skill.SkillId.ToString();
            currentTimeLineItem.CreatedBy = skill.Author;
            currentTimeLineItem.CreatedTime = DateTime.UtcNow;
            if (skill.SkillFirstObservation != null)
            {
                currentTimeLineItem.ProgenyTime = skill.SkillFirstObservation.Value;
            }
            else
            {
                currentTimeLineItem.ProgenyTime = DateTime.UtcNow;
            }
        }

        /// <summary>
        /// Copies the properties needed for updating the timeline item when a skill item has been updated.
        /// </summary>
        /// <param name="currentTimeLineItem"></param>
        /// <param name="skill"></param>
        public static void CopySkillPropertiesForUpdate(this TimeLineItem currentTimeLineItem, Skill skill)
        {
            if (skill.SkillFirstObservation != null)
            {
                currentTimeLineItem.ProgenyTime = skill.SkillFirstObservation.Value;
            }
            else
            {
                currentTimeLineItem.ProgenyTime = DateTime.UtcNow;
            }

            currentTimeLineItem.AccessLevel = skill.AccessLevel;
        }

        /// <summary>
        /// Copies the properties needed for updating the timeline item when a sleep item has been updated.
        /// </summary>
        /// <param name="currentTimeLineItem"></param>
        /// <param name="sleep"></param>
        public static void CopySleepPropertiesForUpdate(this TimeLineItem currentTimeLineItem, Sleep sleep)
        {
            currentTimeLineItem.ProgenyTime = sleep.SleepStart;
            currentTimeLineItem.AccessLevel = sleep.AccessLevel;
        }

        /// <summary>
        /// Copies the properties needed for adding the timeline item when a sleep item has been added.
        /// </summary>
        /// <param name="currentTimeLineItem"></param>
        /// <param name="sleep"></param>
        public static void CopySleepPropertiesForAdd(this TimeLineItem currentTimeLineItem, Sleep sleep)
        {
            currentTimeLineItem.ProgenyId = sleep.ProgenyId;
            currentTimeLineItem.AccessLevel = sleep.AccessLevel;
            currentTimeLineItem.ItemType = (int)KinaUnaTypes.TimeLineType.Sleep;
            currentTimeLineItem.ItemId = sleep.SleepId.ToString();
            currentTimeLineItem.CreatedBy = sleep.Author;
            currentTimeLineItem.CreatedTime = DateTime.UtcNow;
            currentTimeLineItem.ProgenyTime = sleep.SleepStart;
        }

        /// <summary>
        /// Copies the properties needed for adding the timeline item when a vaccination item has been added.
        /// </summary>
        /// <param name="currentTimeLineItem"></param>
        /// <param name="vaccination"></param>
        public static void CopyVaccinationPropertiesForAdd(this TimeLineItem currentTimeLineItem, Vaccination vaccination)
        {
            currentTimeLineItem.ProgenyId = vaccination.ProgenyId;
            currentTimeLineItem.AccessLevel = vaccination.AccessLevel;
            currentTimeLineItem.ItemType = (int)KinaUnaTypes.TimeLineType.Vaccination;
            currentTimeLineItem.ItemId = vaccination.VaccinationId.ToString();
            currentTimeLineItem.CreatedBy = vaccination.Author;
            currentTimeLineItem.CreatedTime = DateTime.UtcNow;
            currentTimeLineItem.ProgenyTime = vaccination.VaccinationDate;
        }

        /// <summary>
        /// Copies the properties needed for updating the timeline item when a vaccination item has been updated.
        /// </summary>
        /// <param name="currentTimeLineItem"></param>
        /// <param name="vaccination"></param>
        public static void CopyVaccinationPropertiesForUpdate(this TimeLineItem currentTimeLineItem, Vaccination vaccination)
        {
            currentTimeLineItem.AccessLevel = vaccination.AccessLevel;
            currentTimeLineItem.ProgenyTime = vaccination.VaccinationDate;
        }

        /// <summary>
        /// Copies the properties needed for adding the timeline item when a vocabulary item has been added.
        /// </summary>
        /// <param name="currentTimeLineItem"></param>
        /// <param name="vocabularyItem"></param>
        public static void CopyVocabularyItemPropertiesForAdd(this TimeLineItem currentTimeLineItem, VocabularyItem vocabularyItem)
        {
            currentTimeLineItem.ProgenyId = vocabularyItem.ProgenyId;
            currentTimeLineItem.AccessLevel = vocabularyItem.AccessLevel;
            currentTimeLineItem.ItemType = (int)KinaUnaTypes.TimeLineType.Vocabulary;
            currentTimeLineItem.ItemId = vocabularyItem.WordId.ToString();
            currentTimeLineItem.CreatedBy = vocabularyItem.Author;
            currentTimeLineItem.CreatedTime = DateTime.UtcNow;
            if (vocabularyItem.Date != null)
            {
                currentTimeLineItem.ProgenyTime = vocabularyItem.Date.Value;
            }
            else
            {
                currentTimeLineItem.ProgenyTime = DateTime.UtcNow;
            }
        }

        /// <summary>
        /// Copies the properties needed for updating the timeline item when a vocabulary item has been updated.
        /// </summary>
        /// <param name="currentTimeLineItem"></param>
        /// <param name="vocabularyItem"></param>
        public static void CopyVocabularyItemPropertiesForUpdate(this TimeLineItem currentTimeLineItem, VocabularyItem vocabularyItem)
        {
            currentTimeLineItem.AccessLevel = vocabularyItem.AccessLevel;
            if (vocabularyItem.Date.HasValue)
            {
                currentTimeLineItem.ProgenyTime = vocabularyItem.Date.Value;
            }
            
        }
    }
}
