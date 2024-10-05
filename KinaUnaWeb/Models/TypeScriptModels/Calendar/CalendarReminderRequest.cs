namespace KinaUnaWeb.Models.TypeScriptModels.Calendar
{
    public class CalendarReminderRequest
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
        public string NotifyTimeString { get; set; }

        public int NotifyTimeOffsetType { get; set; }

        /// <summary>
        /// Has the reminder been sent.
        /// </summary>
        public bool Notified { get; set; }
    }
}
