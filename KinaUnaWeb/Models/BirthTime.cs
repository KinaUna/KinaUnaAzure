using System;
using System.Collections.Generic;
using System.Globalization;
using KinaUna.Data.Extensions;

namespace KinaUnaWeb.Models
{
    public class BirthTime(DateTime birthday, TimeZoneInfo birthdayTimezone)
    {
        private readonly DateTime _currentDateTime = DateTime.UtcNow;

        public string CurrentTime => TimeZoneInfo.ConvertTimeFromUtc(_currentDateTime, birthdayTimezone).ToString("dd MMMM yyyy HH:mm:ss"); // Todo: Custom format
        
        public List<string> CalcYears()
        {
            List<string> ageYearsList = [];

            birthday.GetElapsedTime(_currentDateTime, out int ageYears, out int ageMonths, out int ageDays, out int ageHours, out int ageMinutes, out int ageSeconds);

            ageYearsList.Add(ageYears.ToString());
            ageYearsList.Add(ageMonths.ToString());
            ageYearsList.Add(ageDays.ToString());
            ageYearsList.Add(ageHours.ToString());
            ageYearsList.Add(ageMinutes.ToString());
            ageYearsList.Add(ageSeconds.ToString());

            return ageYearsList;
        }

        public string CalcMonths()
        {
            int ageMonths = GetTotalMonthsBetweenDates(_currentDateTime, birthday);

            return ageMonths.ToString();
        }

        public string[] CalcWeeks()
        {
            int ageWeeks = (DateTime.Today.ToUniversalTime() - new DateTime(birthday.Year, birthday.Month, birthday.Day)).Days / 7;
            int ageWeeksDays = (DateTime.Today.ToUniversalTime() - new DateTime(birthday.Year, birthday.Month, birthday.Day)).Days % 7;
            string[] ageWeeksResult = [ageWeeks.ToString(), ageWeeksDays.ToString()];
            return ageWeeksResult;
        }

        public string CalcDays()
        {
            double ageDays = (_currentDateTime - birthday).TotalDays;
            return ageDays.ToString("F2");
        }

        public string CalcHours()
        {
            double ageHours = (_currentDateTime - birthday).TotalHours;
            return ageHours.ToString("F4");
        }

        public string CalcMinutes()
        {
            double ageMinutes = (_currentDateTime - birthday).TotalMinutes;
            return ageMinutes.ToString("F2");
        }

        public string CalcNextBirthday()
        {
            int daysToNextBirthday;

            if (DateTime.Today.ToUniversalTime() < new DateTime(_currentDateTime.Year, birthday.Month, birthday.Day))
            {
                daysToNextBirthday = (new DateTime(_currentDateTime.Year, birthday.Month, birthday.Day, 0, 0, 0, DateTimeKind.Utc) -
                            new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, DateTime.UtcNow.Day, 0, 0, 0, DateTimeKind.Utc)).Days;
            }
            else
            {
                daysToNextBirthday = (new DateTime(_currentDateTime.Year + 1, birthday.Month, birthday.Day, 0, 0, 0, DateTimeKind.Utc) -
                            new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, DateTime.UtcNow.Day, 0, 0, 0, DateTimeKind.Utc)).Days;
            }

            return daysToNextBirthday.ToString();
        }

        public string[] CalcMileStoneWeeks()
        {
            double ageWeeks = Math.Floor((_currentDateTime - birthday).TotalDays / 7);
            double milestone = Math.Pow(10, Math.Ceiling(Math.Log10(ageWeeks)));
            DateTime milestoneDate = birthday + TimeSpan.FromDays(milestone * 7);
            string[] mileStoneWeeks =
            [
                milestone.ToString(CultureInfo.InvariantCulture),
                milestoneDate.ToString("dddd, dd MMMM yyyy"),
            ];
            return mileStoneWeeks;
        }

        public string[] CalcMileStoneDays()
        {
            double ageDays = (_currentDateTime - birthday).TotalDays;
            double milestone = Math.Pow(10, Math.Ceiling(Math.Log10(ageDays)));
            DateTime milestoneDate = birthday + TimeSpan.FromDays(milestone);
            string[] mileStoneDays =
            [
                milestone.ToString(CultureInfo.InvariantCulture),
                milestoneDate.ToString("dddd, dd MMMM yyyy"),
            ];
            return mileStoneDays;
        }

        public string[] CalcMileStoneHours()
        {
            double ageHours = (_currentDateTime - birthday).TotalHours;
            double milestone = Math.Pow(10, Math.Ceiling(Math.Log10(ageHours)));
            DateTime milestoneDate = birthday + TimeSpan.FromHours(milestone);
            string[] mileStoneHours =
            [
                milestone.ToString(CultureInfo.InvariantCulture),
                milestoneDate.ToString("dddd, dd MMMM yyyy HH:mm"),
            ];
            return mileStoneHours;

        }

        public string[] CalcMileStoneMinutes()
        {
            double ageMinutes = (_currentDateTime - birthday).TotalMinutes;
            double milestone = Math.Pow(10, Math.Ceiling(Math.Log10(ageMinutes)));
            DateTime milestoneDate = birthday + TimeSpan.FromMinutes(milestone);
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
