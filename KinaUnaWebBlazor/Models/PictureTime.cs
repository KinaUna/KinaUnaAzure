namespace KinaUnaWebBlazor.Models
{
    public class PictureTime(DateTime birthday, DateTime? pictureTaken, TimeZoneInfo birthdayTimezone)
    {
        private readonly DateTime _pictureUtcTime = pictureTaken ?? TimeZoneInfo.ConvertTimeToUtc(birthday, birthdayTimezone);
        private readonly DateTime _birthDayUtc = TimeZoneInfo.ConvertTimeToUtc(birthday, birthdayTimezone);

        public string PictureDateTime => _pictureUtcTime.ToString("dd MMMM yyyy HH:mm");


        public string CalcYears()
        {

            TimeSpan age = _pictureUtcTime - _birthDayUtc;
            double ageYears = age.TotalSeconds / (365.0 * 24.0 * 60.0 * 60.0);

            return ageYears.ToString("F6");
        }

        public string CalcMonths()
        {
            int ageMonths = GetTotalMonthsBetweenDates(_pictureUtcTime, _birthDayUtc);

            return ageMonths.ToString();
        }

        public string[] CalcWeeks()
        {
            int ageWeeks = (new DateTime(_pictureUtcTime.Year, _pictureUtcTime.Month, _pictureUtcTime.Day) - new DateTime(_birthDayUtc.Year, _birthDayUtc.Month, _birthDayUtc.Day)).Days / 7;
            int ageWeeksDays = (new DateTime(_pictureUtcTime.Year, _pictureUtcTime.Month, _pictureUtcTime.Day) - new DateTime(_birthDayUtc.Year, _birthDayUtc.Month, _birthDayUtc.Day)).Days % 7;
            string[] ageWeeksResult = [ageWeeks.ToString(), ageWeeksDays.ToString()];
            return ageWeeksResult;
        }

        public string CalcDays()
        {
            double ageDays = (_pictureUtcTime - _birthDayUtc).TotalDays;
            return ageDays.ToString("F2");
        }

        public string CalcHours()
        {
            double ageHours = (_pictureUtcTime - _birthDayUtc).TotalHours;
            return ageHours.ToString("F4");
        }

        public string CalcMinutes()
        {
            double ageMinutes = (_pictureUtcTime - _birthDayUtc).TotalMinutes;
            return ageMinutes.ToString("F2");
        }


        private static int GetTotalMonthsBetweenDates(DateTime firstDateTime, DateTime secondDateTime)
        {
            DateTime earlyDate = (firstDateTime > secondDateTime) ? secondDateTime.Date : firstDateTime.Date;
            DateTime lateDate = (firstDateTime > secondDateTime) ? firstDateTime.Date : secondDateTime.Date;

            // Start with 1 month's difference and keep incrementing
            // until we overshoot the late date
            int monthsDiff = 1;
            while (earlyDate.AddMonths(monthsDiff) <= lateDate)
            {
                monthsDiff++;
            }

            return monthsDiff - 1;
        }
    }
}
