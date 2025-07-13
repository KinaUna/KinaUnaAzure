using System;
using System.ComponentModel.DataAnnotations;

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
        [MaxLength(256)]
        public string UserId { get; set; }

        /// <summary>
        /// How many minutes before the event the reminder should be sent. 0 = custom user defined time.
        /// </summary>
        public int NotifyTimeOffsetType { get; set; }

        /// <summary>
        /// The time to send a notification to remind the user.
        /// </summary>
        public DateTime NotifyTime { get; set; }

        /// <summary>
        /// The RecurrenceRuleId of the event the reminder is for.
        /// </summary>
        public int RecurrenceRuleId { get; set; }

        /// <summary>
        /// Has the reminder been sent.
        /// </summary>
        public bool Notified { get; set; }

        /// <summary>
        /// The date the reminder was last sent.
        /// </summary>
        public DateTime NotifiedDate { get; set; } = DateTime.UtcNow.AddYears(-1);
    }
}
