using System;

namespace KinaUna.Data.Models
{
    /// <summary>
    /// CalendarReminder entity. Used to store reminders for calendar events.
    /// </summary>
    public class CalendarReminder
    {
        public int CalendarReminderId { get; set; }
        /// <summary>
        /// The EventId of the event the reminder is for.
        /// </summary>
        public int EventId { get; set; }
        /// <summary>
        /// The UserId of the user the reminder is for.
        /// </summary>
        public string UserId { get; set; }
        /// <summary>
        /// The time to send a notification to remind the user.
        /// </summary>
        public DateTime NotifyTime { get; set; }
        /// <summary>
        /// Has the reminder been sent.
        /// </summary>
        public bool Notified { get; set; }
    }
}
