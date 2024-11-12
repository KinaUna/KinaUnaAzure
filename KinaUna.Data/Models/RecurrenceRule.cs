using System;
using System.ComponentModel.DataAnnotations;

namespace KinaUna.Data.Models
{
    /// <summary>
    /// Entity Framework Entity for recurrence rule data.
    /// Based on the iCalendar RFC 5545 standard.
    /// </summary>
    public class RecurrenceRule
    {
        public int RecurrenceRuleId { get; set; }

        /// <summary>
        /// The Id of the Progeny the event belongs to. Makes it easier to determine if a progeny has any recurring events.
        /// </summary>
        public int ProgenyId { get; set; }

        /// <summary>
        /// The frequency of the recurrence rule.
        /// Never= 0, daily = 1, weekly = 2, monthly by day = 3, monthly by date = 4, yearly by day = 5, yearly by date = 6
        /// </summary>
        public int Frequency { get; set; } = 0;

        /// <summary>
        /// The interval of the recurrence rule. Default is 1.
        /// I.e. every 2nd day, every 3rd week, every 4th month etc.
        /// </summary>
        public int Interval { get; set; } = 1;
        
        /// <summary>
        /// The count of occurrences of the recurrence rule.
        /// 0 = infinite, or until the Until date is reached.
        /// </summary>
        public int Count { get; set; } = 0;

        /// <summary>
        /// The first date of the recurrence rule. 
        /// </summary>
        public DateTime Start { get; set; } = DateTime.UtcNow;
        /// <summary>
        /// End date of the recurrence rule, if null the rule is infinite or until count is reached.
        /// </summary>
        public DateTime? Until{ get; set; }
        
        /// <summary>
        /// The day of the week for weekly recurrence.
        /// Day of the month for monthly by day and yearly by day.
        /// </summary>
        [MaxLength(1024)]
        public string ByDay { get; set; } = string.Empty;
        
        /// <summary>
        /// Day of the month for monthly by date and yearly by date.
        /// </summary>
        [MaxLength(1024)]
        public string ByMonthDay { get; set; } = string.Empty;

        /// <summary>
        /// The month of the year for yearly recurrence.
        /// </summary>
        [MaxLength(1024)]
        public string ByMonth { get; set; } = string.Empty;

        /// <summary>
        /// Repeat end condition.
        /// 0 = Never, 1 = On date, 2 = After count
        /// </summary>
        public int EndOption { get; set; } = 0;

        public void EnsureStringsAreNotNull()
        {
            if (ByDay == null)
            {
                ByDay = string.Empty;
            }

            if (ByMonthDay == null)
            {
                ByMonthDay = string.Empty;
            }

            if (ByMonth == null)
            {
                ByMonth = string.Empty;
            }
        }
    }
}
