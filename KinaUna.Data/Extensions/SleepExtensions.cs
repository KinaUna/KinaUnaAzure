using KinaUna.Data.Models;
using System;

namespace KinaUna.Data.Extensions
{
    /// <summary>
    /// Extension methods for the Sleep class.
    /// </summary>
    public static class SleepExtensions
    {
        /// <summary>
        /// Copies the properties needed for updating a Sleep entity from one Sleep object to another.
        /// </summary>
        /// <param name="currentSleepItem"></param>
        /// <param name="otherSleepItem"></param>
        public static void CopyPropertiesForUpdate(this Sleep currentSleepItem, Sleep otherSleepItem )
        {
            currentSleepItem.AccessLevel = otherSleepItem.AccessLevel;
            currentSleepItem.SleepDuration = otherSleepItem.SleepDuration;
            currentSleepItem.SleepEnd = otherSleepItem.SleepEnd;
            currentSleepItem.SleepNotes = otherSleepItem.SleepNotes;
            currentSleepItem.SleepStart = otherSleepItem.SleepStart;
            currentSleepItem.SleepRating = otherSleepItem.SleepRating;
            currentSleepItem.SleepNumber = otherSleepItem.SleepNumber;
            currentSleepItem.StartString = otherSleepItem.StartString;
            currentSleepItem.EndString = otherSleepItem.EndString;
        }

        /// <summary>
        /// Copies the properties needed for adding a Sleep entity from one Sleep object to another.
        /// </summary>
        /// <param name="currentSleepItem"></param>
        /// <param name="otherSleepItem"></param>
        public static void CopyPropertiesForAdd(this Sleep currentSleepItem, Sleep otherSleepItem)
        {
            currentSleepItem.AccessLevel = otherSleepItem.AccessLevel;
            currentSleepItem.Author = otherSleepItem.Author;
            currentSleepItem.SleepNotes = otherSleepItem.SleepNotes;
            currentSleepItem.SleepRating = otherSleepItem.SleepRating;
            currentSleepItem.ProgenyId = otherSleepItem.ProgenyId;
            currentSleepItem.SleepStart = otherSleepItem.SleepStart;
            currentSleepItem.SleepEnd = otherSleepItem.SleepEnd;
            currentSleepItem.CreatedDate = DateTime.UtcNow;
        }

        /// <summary>
        /// Calculates the duration of a sleep item and sets the property.
        /// </summary>
        /// <param name="currentSleepItem"></param>
        /// <param name="timezone">The users timezone</param>
        public static void CalculateDuration(this Sleep currentSleepItem, string timezone)
        {
            DateTimeOffset startOffset = new(currentSleepItem.SleepStart,
                TimeZoneInfo.FindSystemTimeZoneById(timezone).GetUtcOffset(currentSleepItem.SleepStart));
            DateTimeOffset endOffset = new(currentSleepItem.SleepEnd,
                TimeZoneInfo.FindSystemTimeZoneById(timezone).GetUtcOffset(currentSleepItem.SleepEnd));
            currentSleepItem.SleepDuration = endOffset - startOffset;
        }
    }
}
