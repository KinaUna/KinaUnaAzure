using KinaUna.Data.Models.ItemInterfaces;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace KinaUna.Data.Models
{
    /// <summary>
    /// Entity Framework Entity for calendar event data.
    /// </summary>
    public class CalendarItem: IContexted, ILocatable
    {
        /// <summary>
        /// Primary key for the calendar event.
        /// </summary>
        [Key]
        public int EventId { get; set; }
        /// <summary>
        /// Unique Id for the calendar event.
        /// </summary>
        [MaxLength(128)]
        public string UId { get; set; } = string.Empty;
        /// <summary>
        /// The Id of the Progeny the event belongs to.
        /// </summary>
        public int ProgenyId { get; set; }
        /// <summary>
        /// The event title.
        /// </summary>
        [MaxLength(256)]
        public string Title { get; set; } = string.Empty;
        /// <summary>
        /// Notes and detailed description of the event.
        /// </summary>
        public string Notes { get; set; } = string.Empty;
        /// <summary>
        /// The start time and date of the event.
        /// </summary>
        public DateTime? StartTime { get; set; }
        /// <summary>
        /// The end time and date of the event.
        /// </summary>
        public DateTime? EndTime { get; set; }

        /// <summary>
        /// The location of the event.
        /// </summary>
        [MaxLength(256)]
        public string Location { get; set; } = string.Empty;
        /// <summary>
        /// The context of the event.
        /// </summary>
        [MaxLength(256)]
        public string Context { get; set; } = string.Empty;
        /// <summary>
        /// All day event. I.e. birthday, holiday etc.
        /// </summary>
        public bool AllDay { get; set; }
        /// <summary>
        /// The required access level to view the event.
        /// </summary>
        public int AccessLevel { get; set; }

        /// <summary>
        /// The User Id of the user who created the event.
        /// </summary>
        [MaxLength(256)]
        public string Author { get; set; } = string.Empty;

        /// <summary>
        /// The Id of the RecurrenceRule for recurring events.
        /// 0 = No recurrence.
        /// </summary>
        public int RecurrenceRuleId { get; set; }

        /// <summary>
        /// String representation of the start time.
        /// </summary>
        [NotMapped]
        public string StartString { get; set; } = string.Empty;
        /// <summary>
        /// String representation of the end time.
        /// </summary>
        [NotMapped]
        public string EndString { get; set; } = string.Empty;

        

        /// <summary>
        /// Progeny data for the progeny the event belongs to.
        /// </summary>
        [NotMapped]
        public Progeny Progeny { get; set; }
        /// <summary>
        /// Read only flag. Used to determine if the event can be edited or deleted in Calendar views.
        /// </summary>
        [NotMapped]
        public bool IsReadonly { get; set; }

        public string GetLocationString()
        {
            return Location;
        }
    }
}
