using System;

namespace KinaUna.Data.Models
{
    public class RecurrenceRule
    {
        public int RecurrenceRuleId { get; set; }
        /// <summary>
        /// The frequency of the recurrence rule.
        /// daily = 0, weekly = 1, monthly by day = 2, monthly by date = 3, yearly by day = 4, yearly by date = 5
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
        /// End date of the recurrence rule, if null the rule is infinite or until count is reached.
        /// </summary>
        public DateTime? Until{ get; set; }
        /// <summary>
        /// The day of the week for weekly recurrence.
        /// Day of the month for monthly by day and yearly by day.
        /// </summary>
        public string ByDay { get; set; } = string.Empty;
        /// <summary>
        /// Day of the month for monthly by date and yearly by date.
        /// </summary>
        public string ByMonthDay { get; set; } = string.Empty;
        /// <summary>
        /// The month of the year for yearly recurrence.
        /// </summary>
        public string ByMonth { get; set; } = string.Empty;
    }
}
