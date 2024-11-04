namespace KinaUna.Data.Models
{
    /// <summary>
    /// Types of recurrence rules.
    /// </summary>
    public static class RecurrenceTypes
    {
        /// <summary>
        /// The frequency of the recurrence rule.
        /// daily = 0, weekly = 1, monthly by day = 2, monthly by date = 3, yearly by day = 4, yearly by date = 5
        /// </summary>
        public enum RecurrenceFrequency { Daily = 0, Weekly = 1, MonthlyByDay = 2, MonthlyByDate = 3, YearlyByDay = 4, YearlyByDate = 5  }
    }
}