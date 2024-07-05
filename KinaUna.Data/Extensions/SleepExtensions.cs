using KinaUna.Data.Models;
using System;

namespace KinaUna.Data.Extensions
{
    public static class SleepExtensions
    {
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
