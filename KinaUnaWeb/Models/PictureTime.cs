using System;
using System.Collections.Generic;
using KinaUna.Data.Extensions;

namespace KinaUnaWeb.Models
{
    public class PictureTime
    {
        private readonly DateTime _pictureUtcTime;
        private readonly DateTime _birthDayUtc;

        public string PictureDateTime => _pictureUtcTime.ToString("dd MMMM yyyy HH:mm");

        public PictureTime(DateTime birthday, DateTime? pictureTaken, TimeZoneInfo birthdayTimezone)
        {
            _birthDayUtc = TimeZoneInfo.ConvertTimeToUtc(birthday, birthdayTimezone);


            if (pictureTaken != null)
            {
                _pictureUtcTime = pictureTaken.Value;  //TimeZoneInfo.ConvertTimeToUtc((DateTime)pictureTaken, birthdayTimezone);
            }
            else
            {
                _pictureUtcTime = TimeZoneInfo.ConvertTimeToUtc(birthday, birthdayTimezone);
            }
        }


        public List<string> CalcYears()
        {

            List<string> ageYearsList = [];

            _birthDayUtc.GetElapsedTime(_pictureUtcTime, out int ageYears, out int ageMonths, out int ageDays, out int ageHours, out int ageMinutes, out int ageSeconds);

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
