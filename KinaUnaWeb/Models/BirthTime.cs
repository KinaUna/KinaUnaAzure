using System;
using System.Globalization;

namespace KinaUnaWeb.Models
{
    public class BirthTime(DateTime birthday, TimeZoneInfo birthdayTimezone)
    {
        private readonly DateTime _birthDay = TimeZoneInfo.ConvertTimeToUtc(birthday, birthdayTimezone);
        private readonly DateTime _currentDateTime = DateTime.UtcNow;

        public string CurrentTime => TimeZoneInfo.ConvertTimeFromUtc(_currentDateTime, birthdayTimezone).ToString("dd MMMM yyyy HH:mm:ss"); // Todo: Custom format


        public string CalcYears()
        {

            TimeSpan age = _currentDateTime - _birthDay;
            double ageYears = age.TotalSeconds / (365.0 * 24.0 * 60.0 * 60.0);

            return ageYears.ToString("F6");
        }

        public string CalcMonths()
        {
            int ageMonths = GetTotalMonthsBetweenDates(_currentDateTime, _birthDay);

            return ageMonths.ToString();
        }

        public string[] CalcWeeks()
        {
            int ageWeeks = (DateTime.Today.ToUniversalTime() - new DateTime(_birthDay.Year, _birthDay.Month, _birthDay.Day)).Days / 7;
            int ageWeeksDays = (DateTime.Today.ToUniversalTime() - new DateTime(_birthDay.Year, _birthDay.Month, _birthDay.Day)).Days % 7;
            string[] ageWeeksResult = [ageWeeks.ToString(), ageWeeksDays.ToString()];
            return ageWeeksResult;
        }

        public string CalcDays()
        {
            double ageDays = (_currentDateTime - _birthDay).TotalDays;
            return ageDays.ToString("F2");
        }

        public string CalcHours()
        {
            double ageHours = (_currentDateTime - _birthDay).TotalHours;
            return ageHours.ToString("F4");
        }

        public string CalcMinutes()
        {
            double ageMinutes = (_currentDateTime - _birthDay).TotalMinutes;
            return ageMinutes.ToString("F2");
        }

        public string CalcNextBirthday()
        {
            int daysToNextBirthday;

            if (DateTime.Today.ToUniversalTime() < new DateTime(_currentDateTime.Year, _birthDay.Month, _birthDay.Day))
            {
                daysToNextBirthday = (new DateTime(_currentDateTime.Year, _birthDay.Month, _birthDay.Day, 0, 0, 0, DateTimeKind.Utc) -
                            new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, DateTime.UtcNow.Day, 0, 0, 0, DateTimeKind.Utc)).Days;
            }
            else
            {
                daysToNextBirthday = (new DateTime(_currentDateTime.Year + 1, _birthDay.Month, _birthDay.Day, 0, 0, 0, DateTimeKind.Utc) -
                            new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, DateTime.UtcNow.Day, 0, 0, 0, DateTimeKind.Utc)).Days;
            }

            return daysToNextBirthday.ToString();
        }

        public string[] CalcMileStoneWeeks()
        {
            double ageWeeks = Math.Floor((_currentDateTime - _birthDay).TotalDays / 7);
            double milestone = Math.Pow(10, Math.Ceiling(Math.Log10(ageWeeks)));
            DateTime milestoneDate = _birthDay + TimeSpan.FromDays(milestone * 7);
            string[] mileStoneWeeks =
            [
                milestone.ToString(CultureInfo.InvariantCulture),
                milestoneDate.ToString("dddd, dd MMMM yyyy"),
            ];
            return mileStoneWeeks;
        }

        public string[] CalcMileStoneDays()
        {
            double ageDays = (_currentDateTime - _birthDay).TotalDays;
            double milestone = Math.Pow(10, Math.Ceiling(Math.Log10(ageDays)));
            DateTime milestoneDate = _birthDay + TimeSpan.FromDays(milestone);
            string[] mileStoneDays =
            [
                milestone.ToString(CultureInfo.InvariantCulture),
                milestoneDate.ToString("dddd, dd MMMM yyyy"),
            ];
            return mileStoneDays;
        }

        public string[] CalcMileStoneHours()
        {
            double ageHours = (_currentDateTime - _birthDay).TotalHours;
            double milestone = Math.Pow(10, Math.Ceiling(Math.Log10(ageHours)));
            DateTime milestoneDate = _birthDay + TimeSpan.FromHours(milestone);
            string[] mileStoneHours =
            [
                milestone.ToString(CultureInfo.InvariantCulture),
                milestoneDate.ToString("dddd, dd MMMM yyyy HH:mm"),
            ];
            return mileStoneHours;

        }

        public string[] CalcMileStoneMinutes()
        {
            double ageMinutes = (_currentDateTime - _birthDay).TotalMinutes;
            double milestone = Math.Pow(10, Math.Ceiling(Math.Log10(ageMinutes)));
            DateTime milestoneDate = _birthDay + TimeSpan.FromMinutes(milestone);
            string[] mileStoneMinutes =
            [
                milestone.ToString(CultureInfo.InvariantCulture),
                milestoneDate.ToString("dddd, dd MMMM yyyy HH:mm"),
            ];
            return mileStoneMinutes;

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
