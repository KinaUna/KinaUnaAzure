using System;

namespace KinaUna.Data.Extensions
{
    // Original Source: https://learn.microsoft.com/en-us/answers/questions/479549/age-calculator-c
    public static class DateTimeExtensions
    {
        /// <summary>  
        /// Get elapsed time in years, months, days, hours, seconds  
        /// </summary>  
        /// <param name="startDate">Date in past</param>  
        /// <param name="endDate">Date pass startDate</param>  
        /// <param name="years"></param>  
        /// <param name="months"></param>  
        /// <param name="days"></param>  
        /// <param name="hours"></param>  
        /// <param name="minutes"></param>  
        /// <param name="seconds"></param>  
        public static void GetElapsedTime(this DateTime startDate, DateTime endDate, out int years, out int months, out int days, out int hours, out int minutes, out int seconds)
        {
            // If from_date > to_date, switch them around.  
            if (startDate > endDate)
            {
                GetElapsedTime(
                    endDate,
                    startDate,
                    out years,
                    out months,
                    out days,
                    out hours,
                    out minutes,
                    out seconds);

                years = -years;
                months = -months;
                days = -days;
                hours = -hours;
                minutes = -minutes;
                seconds = -seconds;
            }
            else
            {
                // Handle the years.  
                years = endDate.Year - startDate.Year;

                // See if we went too far.  
                DateTime testDate = startDate.AddMonths(12 * years);
                if (testDate > endDate)
                {
                    years--;
                    testDate = startDate.AddMonths(12 * years);
                }

                // Add months until we go too far.  
                months = 0;
                while (testDate <= endDate)
                {
                    months++;
                    testDate = startDate.AddMonths(12 * years + months);
                }

                months--;

                // Subtract to see how many more days,  
                // hours, minutes, etc. we need.  
                startDate = startDate.AddMonths(12 * years + months);
                TimeSpan remainder = endDate - startDate;
                days = remainder.Days;
                hours = remainder.Hours;
                minutes = remainder.Minutes;
                seconds = remainder.Seconds;
            }
        }
    }
}
