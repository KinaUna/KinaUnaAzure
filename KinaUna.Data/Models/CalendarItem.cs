using KinaUna.Data.Models.ItemInterfaces;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace KinaUna.Data.Models
{
    /// <summary>
    /// Entity Framework Entity for calendar event data.
    /// </summary>
    public class CalendarItem: IContexted
    {
        /// <summary>
        /// Primary key for the calendar event.
        /// </summary>
        [Key]
        public int EventId { get; set; }
        /// <summary>
        /// The Id of the Progeny the event belongs to.
        /// </summary>
        public int ProgenyId { get; set; }
        /// <summary>
        /// The event title.
        /// </summary>
        public string Title { get; set; }
        /// <summary>
        /// Notes and detailed description of the event.
        /// </summary>
        public string Notes { get; set; }
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
        public string Location { get; set; }
        /// <summary>
        /// The context of the event.
        /// </summary>
        public string Context { get; set; }
        /// <summary>
        /// All day event. I.e. birthday, holiday etc.
        /// </summary>
        public bool AllDay { get; set; }
        /// <summary>
        /// The required access level to view the event.
        /// </summary>
        public int AccessLevel { get; set; }
        /// <summary>
        /// String representation of the start time.
        /// </summary>
        public string StartString { get; set; }
        /// <summary>
        /// String representation of the end time.
        /// </summary>
        public string EndString { get; set; }
        /// <summary>
        /// The User Id of the user who created the event.
        /// </summary>
        public string Author { get; set; }

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
    }
}
