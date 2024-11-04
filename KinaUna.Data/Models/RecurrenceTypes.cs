namespace KinaUna.Data.Models
{
    /// <summary>
    /// Types of recurrence rules.
    /// </summary>
    public static class RecurrenceTypes
    {
        /// <summary>
        /// The frequency of the recurrence rule.
        /// Never= 0, daily = 1, weekly = 2, monthly by day = 3, monthly by date = 4, yearly by day = 5, yearly by date = 6
        /// </summary>
        public enum RecurrenceFrequency {Never= 0, Daily = 1, Weekly = 2, MonthlyByDay = 3, MonthlyByDate = 4, YearlyByDay = 5, YearlyByDate = 6  }
    }
}