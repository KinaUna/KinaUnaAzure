using System;
using KinaUna.Data.Models;

namespace KinaUna.Data.Extensions
{
    /// <summary>
    /// Extensions for Video data.
    /// </summary>
    public static class VideoExtensions
    {
        /// <summary>
        /// Replaces null strings with empty string.
        /// </summary>
        /// <param name="video">Video: The video object to update.</param>
        
        public static void RemoveNullStrings(this Video video)
        {
            if (video == null) return;

            video.Altitude ??= "";

            video.Tags ??= "";

            video.Author ??= "";

            video.Latitude ??= "";

            video.Location ??= "";

            video.Longtitude ??= "";

            video.Owners ??= "";

            video.ThumbLink ??= "";

            video.VideoLink ??= "";
                
            video.TimeZone ??= "";
        }

        public static void CopyPropertiesForUpdate(this Video currentVideo, Video otherVideo, bool parseDuration = false)
        {
            currentVideo.AccessLevel = otherVideo.AccessLevel;
            currentVideo.ProgenyId = otherVideo.ProgenyId;
            currentVideo.CommentThreadNumber = otherVideo.CommentThreadNumber;
            currentVideo.Duration = otherVideo.Duration;
            currentVideo.DurationHours = otherVideo.DurationHours;
            currentVideo.DurationMinutes = otherVideo.DurationMinutes;
            currentVideo.DurationSeconds = otherVideo.DurationSeconds;
            currentVideo.Author = otherVideo.Author;
            currentVideo.Owners = otherVideo.Owners;
            currentVideo.VideoLink = otherVideo.VideoLink;
            currentVideo.VideoNumber = otherVideo.VideoNumber;
            currentVideo.VideoTime = otherVideo.VideoTime;
            currentVideo.ThumbLink = otherVideo.ThumbLink;
            currentVideo.TimeZone = otherVideo.TimeZone;
            currentVideo.VideoType = otherVideo.VideoType;
            currentVideo.Progeny = otherVideo.Progeny;
            currentVideo.Comments = otherVideo.Comments;

            if (parseDuration)
            {
                bool durationHoursParsed = int.TryParse(otherVideo.DurationHours, out int durHours);
                bool durationMinutesParsed = int.TryParse(otherVideo.DurationMinutes, out int durMins);
                bool durationSecondsParsed = int.TryParse(otherVideo.DurationSeconds, out int durSecs);
                if (durHours + durMins + durSecs != 0 && durationHoursParsed && durationMinutesParsed && durationSecondsParsed)
                {
                    currentVideo.Duration = new TimeSpan(durHours, durMins, durSecs);
                }
            }
            

            if (!string.IsNullOrEmpty(otherVideo.Tags))
            {
                currentVideo.Tags = otherVideo.Tags.TrimEnd(',', ' ').TrimStart(',', ' ');
            }

            if (!string.IsNullOrEmpty(otherVideo.Location))
            {
                currentVideo.Location = otherVideo.Location;
            }

            if (!string.IsNullOrEmpty(otherVideo.Longtitude))
            {
                currentVideo.Longtitude = otherVideo.Longtitude;
            }

            if (!string.IsNullOrEmpty(otherVideo.Latitude))
            {
                currentVideo.Latitude = otherVideo.Latitude;
            }

            if (!string.IsNullOrEmpty(otherVideo.Altitude))
            {
                currentVideo.Altitude = otherVideo.Altitude;
            }
        }

    }
}
