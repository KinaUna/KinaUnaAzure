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
            currentTimeLineItem.ProgenyTime = otherTimeLineItem.ProgenyTime;
            currentTimeLineItem.ModifiedBy = otherTimeLineItem.ModifiedBy;
            currentTimeLineItem.ModifiedTime = DateTime.UtcNow;
        }

        /// <summary>
        /// Copies the properties needed for adding a TimeLineItem entity from one TimeLineItem object to another.
        /// </summary>
        /// <param name="currentTimeLineItem"></param>
        /// <param name="otherTimeLineItem"></param>
        public static void CopyPropertiesForAdd(this TimeLineItem currentTimeLineItem, TimeLineItem otherTimeLineItem)
        {
            currentTimeLineItem.ProgenyId = otherTimeLineItem.ProgenyId;
            currentTimeLineItem.FamilyId = otherTimeLineItem.FamilyId;
            currentTimeLineItem.CreatedBy = otherTimeLineItem.CreatedBy;
            currentTimeLineItem.CreatedTime = DateTime.UtcNow;
            currentTimeLineItem.ModifiedBy = otherTimeLineItem.CreatedBy;
            currentTimeLineItem.ModifiedTime = DateTime.UtcNow;
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
            currentTimeLineItem.ModifiedBy = calendarItem.ModifiedBy;
            currentTimeLineItem.ModifiedTime = DateTime.UtcNow;
            return true;

        }

        /// <summary>
        /// Copies the properties needed for updating the timeline item when a calendar item has been updated.
        /// </summary>
        /// <param name="currentTimeLineItem"></param>
        /// <param name="calendarItem"></param>
        /// <returns>bool: True if the calendar item has valid data</returns>
        public static bool CopyCalendarItemPropertiesForRecurringEvent(this TimeLineItem currentTimeLineItem, CalendarItem calendarItem)
        {
            if (currentTimeLineItem == null || !calendarItem.StartTime.HasValue || !calendarItem.EndTime.HasValue) return false;

            currentTimeLineItem.ProgenyId = calendarItem.ProgenyId;
            currentTimeLineItem.FamilyId = calendarItem.FamilyId;
            currentTimeLineItem.ProgenyTime = calendarItem.StartTime.Value;
            currentTimeLineItem.ItemType = (int)KinaUnaTypes.TimeLineType.Calendar;
            currentTimeLineItem.ItemId = calendarItem.EventId.ToString();
            currentTimeLineItem.ItemYear = calendarItem.StartTime.Value.Year;
            currentTimeLineItem.ItemMonth = calendarItem.StartTime.Value.Month;
            currentTimeLineItem.ItemDay = calendarItem.StartTime.Value.Day;
            currentTimeLineItem.ItemPerMission = calendarItem.ItemPerMission;
            return true;

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
            currentTimeLineItem.ModifiedBy = contactItem.ModifiedBy;
            currentTimeLineItem.ModifiedTime = DateTime.UtcNow;

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
            currentTimeLineItem.FamilyId = contactItem.FamilyId;
            currentTimeLineItem.ItemType = (int)KinaUnaTypes.TimeLineType.Contact;
            currentTimeLineItem.ItemId = contactItem.ContactId.ToString();
            currentTimeLineItem.CreatedBy = contactItem.CreatedBy;
            currentTimeLineItem.CreatedTime = DateTime.UtcNow;
            currentTimeLineItem.ModifiedBy = contactItem.ModifiedBy;
            currentTimeLineItem.ModifiedTime = DateTime.UtcNow;
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
            currentTimeLineItem.ModifiedBy = friendItem.ModifiedBy;
            currentTimeLineItem.ModifiedTime = DateTime.UtcNow;

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
            currentTimeLineItem.ItemType = (int)KinaUnaTypes.TimeLineType.Friend;
            currentTimeLineItem.ItemId = friendItem.FriendId.ToString();
            currentTimeLineItem.CreatedBy = friendItem.CreatedBy;
            currentTimeLineItem.CreatedTime = DateTime.UtcNow;
            currentTimeLineItem.ModifiedBy = friendItem.CreatedBy;
            currentTimeLineItem.ModifiedTime = DateTime.UtcNow;

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

            currentTimeLineItem.ModifiedBy = location.ModifiedBy;
            currentTimeLineItem.ModifiedTime = DateTime.UtcNow;
        }

        /// <summary>
        /// Copies the properties needed for adding a timeline item when a location item has been added.
        /// </summary>
        /// <param name="currentTimeLineItem"></param>
        /// <param name="location"></param>
        public static void CopyLocationPropertiesForAdd(this TimeLineItem currentTimeLineItem, Location location)
        {
            currentTimeLineItem.ProgenyId = location.ProgenyId;
            currentTimeLineItem.FamilyId = location.FamilyId;
            currentTimeLineItem.ItemType = (int)KinaUnaTypes.TimeLineType.Location;
            currentTimeLineItem.ItemId = location.LocationId.ToString();
            currentTimeLineItem.CreatedBy = location.CreatedBy;
            currentTimeLineItem.CreatedTime = DateTime.UtcNow;
            currentTimeLineItem.ModifiedBy = location.CreatedBy;
            currentTimeLineItem.ModifiedTime = DateTime.UtcNow;
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
            currentTimeLineItem.ItemType = (int)KinaUnaTypes.TimeLineType.Measurement;
            currentTimeLineItem.ItemId = measurement.MeasurementId.ToString();
            currentTimeLineItem.CreatedBy = measurement.CreatedBy;
            currentTimeLineItem.CreatedTime = DateTime.UtcNow;
            currentTimeLineItem.ModifiedBy = measurement.CreatedBy;
            currentTimeLineItem.ModifiedTime = DateTime.UtcNow;
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
            currentTimeLineItem.ModifiedBy = measurement.ModifiedBy;
            currentTimeLineItem.ModifiedTime = DateTime.UtcNow;
        }

        /// <summary>
        /// Copies the properties needed for adding the timeline item when a note item has been added.
        /// </summary>
        /// <param name="currentTimeLineItem"></param>
        /// <param name="note"></param>
        public static void CopyNotePropertiesForAdd(this TimeLineItem currentTimeLineItem, Note note)
        {
            currentTimeLineItem.ProgenyId = note.ProgenyId;
            currentTimeLineItem.ItemType = (int)KinaUnaTypes.TimeLineType.Note;
            currentTimeLineItem.ItemId = note.NoteId.ToString();
            currentTimeLineItem.CreatedBy = note.CreatedBy;
            currentTimeLineItem.CreatedTime = note.CreatedDate;
            currentTimeLineItem.ProgenyTime = note.CreatedDate;
            currentTimeLineItem.ModifiedBy = note.CreatedBy;
            currentTimeLineItem.ModifiedTime = DateTime.UtcNow;
        }

        /// <summary>
        /// Copies the properties needed for updating the timeline item when a note item has been updated.
        /// </summary>
        /// <param name="currentTimeLineItem"></param>
        /// <param name="note"></param>
        public static void CopyNotePropertiesForUpdate(this TimeLineItem currentTimeLineItem, Note note)
        {
            currentTimeLineItem.ProgenyTime = note.CreatedDate;
            currentTimeLineItem.ModifiedBy = note.ModifiedBy;
            currentTimeLineItem.ModifiedTime = DateTime.UtcNow;
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
            if (picture.PictureTime.HasValue)
            {
                currentTimeLineItem.ProgenyTime = picture.PictureTime.Value;
            }

            currentTimeLineItem.CreatedBy = picture.CreatedBy;
            currentTimeLineItem.CreatedTime = DateTime.UtcNow;
            currentTimeLineItem.ModifiedBy = picture.CreatedBy;
            currentTimeLineItem.ModifiedTime = DateTime.UtcNow;

        }

        /// <summary>
        /// Copies the properties needed for updating the timeline item when a picture item has been updated.
        /// </summary>
        /// <param name="currentTimeLineItem"></param>
        /// <param name="picture"></param>
        public static void CopyPicturePropertiesForUpdate(this TimeLineItem currentTimeLineItem, Picture picture)
        {
            currentTimeLineItem.ModifiedBy = picture.ModifiedBy;
            currentTimeLineItem.ModifiedTime = DateTime.UtcNow;
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
            if (video.VideoTime.HasValue)
            {
                currentTimeLineItem.ProgenyTime = video.VideoTime.Value;
            }

            currentTimeLineItem.CreatedBy = video.CreatedBy;
            currentTimeLineItem.CreatedTime = DateTime.UtcNow;
            currentTimeLineItem.ModifiedBy = video.CreatedBy;
            currentTimeLineItem.ModifiedTime = DateTime.UtcNow;

        }

        /// <summary>
        /// Copies the properties needed for updating the timeline item when a video item has been updated.
        /// </summary>
        /// <param name="currentTimeLineItem"></param>
        /// <param name="video"></param>
        public static void CopyVideoPropertiesForUpdate(this TimeLineItem currentTimeLineItem, Video video)
        {
            if (video.VideoTime.HasValue)
            {
                currentTimeLineItem.ProgenyTime = video.VideoTime.Value;
            }

            currentTimeLineItem.ModifiedBy = video.ModifiedBy;
            currentTimeLineItem.ModifiedTime = DateTime.UtcNow;
        }

        /// <summary>
        /// Copies the properties needed for adding the timeline item when a skill item has been added.
        /// </summary>
        /// <param name="currentTimeLineItem"></param>
        /// <param name="skill"></param>
        public static void CopySkillPropertiesForAdd(this TimeLineItem currentTimeLineItem, Skill skill)
        {
            currentTimeLineItem.ProgenyId = skill.ProgenyId;
            currentTimeLineItem.ItemType = (int)KinaUnaTypes.TimeLineType.Skill;
            currentTimeLineItem.ItemId = skill.SkillId.ToString();
            currentTimeLineItem.CreatedBy = skill.CreatedBy;
            currentTimeLineItem.CreatedTime = DateTime.UtcNow;
            currentTimeLineItem.ModifiedBy = skill.CreatedBy;
            currentTimeLineItem.ModifiedTime = DateTime.UtcNow;
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

            currentTimeLineItem.ModifiedBy = skill.ModifiedBy;
            currentTimeLineItem.ModifiedTime = DateTime.UtcNow;
        }

        /// <summary>
        /// Copies the properties needed for updating the timeline item when a sleep item has been updated.
        /// </summary>
        /// <param name="currentTimeLineItem"></param>
        /// <param name="sleep"></param>
        public static void CopySleepPropertiesForUpdate(this TimeLineItem currentTimeLineItem, Sleep sleep)
        {
            currentTimeLineItem.ProgenyTime = sleep.SleepStart;
            currentTimeLineItem.ModifiedBy = sleep.ModifiedBy;
            currentTimeLineItem.ModifiedTime = DateTime.UtcNow;
        }

        /// <summary>
        /// Copies the properties needed for adding the timeline item when a sleep item has been added.
        /// </summary>
        /// <param name="currentTimeLineItem"></param>
        /// <param name="sleep"></param>
        public static void CopySleepPropertiesForAdd(this TimeLineItem currentTimeLineItem, Sleep sleep)
        {
            currentTimeLineItem.ProgenyId = sleep.ProgenyId;
            currentTimeLineItem.ItemType = (int)KinaUnaTypes.TimeLineType.Sleep;
            currentTimeLineItem.ItemId = sleep.SleepId.ToString();
            currentTimeLineItem.CreatedBy = sleep.CreatedBy;
            currentTimeLineItem.CreatedTime = DateTime.UtcNow;
            currentTimeLineItem.ModifiedBy = sleep.CreatedBy;
            currentTimeLineItem.ModifiedTime = DateTime.UtcNow;
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
            currentTimeLineItem.ItemType = (int)KinaUnaTypes.TimeLineType.Vaccination;
            currentTimeLineItem.ItemId = vaccination.VaccinationId.ToString();
            currentTimeLineItem.CreatedBy = vaccination.CreatedBy;
            currentTimeLineItem.CreatedTime = DateTime.UtcNow;
            currentTimeLineItem.ModifiedBy = vaccination.CreatedBy;
            currentTimeLineItem.ModifiedTime = DateTime.UtcNow;
            currentTimeLineItem.ProgenyTime = vaccination.VaccinationDate;
        }

        /// <summary>
        /// Copies the properties needed for updating the timeline item when a vaccination item has been updated.
        /// </summary>
        /// <param name="currentTimeLineItem"></param>
        /// <param name="vaccination"></param>
        public static void CopyVaccinationPropertiesForUpdate(this TimeLineItem currentTimeLineItem, Vaccination vaccination)
        {
            currentTimeLineItem.ProgenyTime = vaccination.VaccinationDate;
            currentTimeLineItem.ModifiedBy = vaccination.ModifiedBy;
            currentTimeLineItem.ModifiedTime = DateTime.UtcNow;
        }

        /// <summary>
        /// Copies the properties needed for adding the timeline item when a vocabulary item has been added.
        /// </summary>
        /// <param name="currentTimeLineItem"></param>
        /// <param name="vocabularyItem"></param>
        public static void CopyVocabularyItemPropertiesForAdd(this TimeLineItem currentTimeLineItem, VocabularyItem vocabularyItem)
        {
            currentTimeLineItem.ProgenyId = vocabularyItem.ProgenyId;
            currentTimeLineItem.ItemType = (int)KinaUnaTypes.TimeLineType.Vocabulary;
            currentTimeLineItem.ItemId = vocabularyItem.WordId.ToString();
            currentTimeLineItem.CreatedBy = vocabularyItem.CreatedBy;
            currentTimeLineItem.CreatedTime = DateTime.UtcNow;
            currentTimeLineItem.ModifiedBy = vocabularyItem.CreatedBy;
            currentTimeLineItem.ModifiedTime = DateTime.UtcNow;
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
            currentTimeLineItem.ModifiedBy = vocabularyItem.ModifiedBy;
            currentTimeLineItem.ModifiedTime = DateTime.UtcNow;
            if (vocabularyItem.Date.HasValue)
            {
                currentTimeLineItem.ProgenyTime = vocabularyItem.Date.Value;
            }
            
        }

        /// <summary>
        /// Copies the properties of a <see cref="TodoItem"/> to a <see cref="TimeLineItem"/> for update purposes.
        /// </summary>
        /// <remarks>This method updates the <paramref name="currentTimeLineItem"/> with the <see
        /// cref="TodoItem.CreatedTime"/> or  <see cref="TodoItem.StartDate"/> (if specified) as the <see
        /// cref="TimeLineItem.ProgenyTime"/>.</remarks>
        /// <param name="currentTimeLineItem">The <see cref="TimeLineItem"/> instance to update. Must not be <see langword="null"/>.</param>
        /// <param name="todoItem">The <see cref="TodoItem"/> instance whose properties will be copied. Must not be <see langword="null"/>.</param>
        /// <returns><see langword="true"/> if the properties were successfully copied; otherwise, <see langword="false"/> if
        /// <paramref name="currentTimeLineItem"/> is <see langword="null"/>.</returns>
        public static bool CopyTodoItemPropertiesForUpdate(this TimeLineItem currentTimeLineItem, TodoItem todoItem)
        {
            if (currentTimeLineItem == null) return false;
            currentTimeLineItem.ProgenyId = todoItem.ProgenyId;
            currentTimeLineItem.FamilyId = todoItem.FamilyId;
            DateTime progenyTime = todoItem.CreatedTime;
            if (todoItem.StartDate.HasValue)
            {
                progenyTime = todoItem.StartDate.Value;
            }
            currentTimeLineItem.ProgenyTime = progenyTime;
            currentTimeLineItem.ModifiedBy = todoItem.ModifiedBy;
            currentTimeLineItem.ModifiedTime = DateTime.UtcNow;
            return true;

        }
    }
}
