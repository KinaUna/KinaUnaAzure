using KinaUna.Data.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace KinaUnaProgenyApi.Models
{
    public class SleepStatsModel
    {
        public TimeSpan SleepTotal { get; set; }
        public TimeSpan TotalAverage { get; set; }
        public TimeSpan SleepLastMonth { get; set; }
        public TimeSpan LastMonthAverage { get; set; }
        public TimeSpan SleepLastYear { get; set; }
        public TimeSpan LastYearAverage { get; set; }

        public void ProcessSleepStats(List<Sleep> allSleepList, int accessLevel, string userTimeZone)
        {
            SleepTotal = TimeSpan.Zero;
            SleepLastYear = TimeSpan.Zero;
            SleepLastMonth = TimeSpan.Zero;

            List<Sleep> sleepList = new List<Sleep>();
            DateTime yearAgo = new DateTime(DateTime.UtcNow.Year - 1, DateTime.UtcNow.Month, DateTime.UtcNow.Day, DateTime.UtcNow.Hour, DateTime.UtcNow.Minute, 0);
            DateTime monthAgo = DateTime.UtcNow - TimeSpan.FromDays(30);
            if (allSleepList.Count != 0)
            {
                foreach (Sleep sleep in allSleepList)
                {

                    bool isLessThanYear = sleep.SleepEnd > yearAgo;
                    bool isLessThanMonth = sleep.SleepEnd > monthAgo;
                    sleep.SleepStart = TimeZoneInfo.ConvertTimeFromUtc(sleep.SleepStart,
                        TimeZoneInfo.FindSystemTimeZoneById(userTimeZone));
                    sleep.SleepEnd = TimeZoneInfo.ConvertTimeFromUtc(sleep.SleepEnd,
                        TimeZoneInfo.FindSystemTimeZoneById(userTimeZone));
                    DateTimeOffset sOffset = new DateTimeOffset(sleep.SleepStart,
                        TimeZoneInfo.FindSystemTimeZoneById(userTimeZone).GetUtcOffset(sleep.SleepStart));
                    DateTimeOffset eOffset = new DateTimeOffset(sleep.SleepEnd,
                        TimeZoneInfo.FindSystemTimeZoneById(userTimeZone).GetUtcOffset(sleep.SleepEnd));
                    sleep.SleepDuration = eOffset - sOffset;

                    SleepTotal = SleepTotal + sleep.SleepDuration;
                    if (isLessThanYear)
                    {
                        SleepLastYear = SleepLastYear + sleep.SleepDuration;
                    }

                    if (isLessThanMonth)
                    {
                        SleepLastMonth = SleepLastMonth + sleep.SleepDuration;
                    }

                    if (sleep.AccessLevel >= accessLevel)
                    {
                        sleepList.Add(sleep);
                    }
                }
                sleepList = sleepList.OrderBy(s => s.SleepStart).ToList();

                TotalAverage = SleepTotal / (DateTime.UtcNow - sleepList.First().SleepStart).TotalDays;
                LastYearAverage = SleepLastYear / (DateTime.UtcNow - yearAgo).TotalDays;
                LastMonthAverage = SleepLastMonth / 30;

            }
            else
            {
                TotalAverage = TimeSpan.Zero;
                LastYearAverage = TimeSpan.Zero;
                LastMonthAverage = TimeSpan.Zero;
            }
        }

        public List<Sleep> ProcessSleepChartData(List<Sleep> allSleepList, int accessLevel, string userTimeZone)
        {
            List<Sleep> sleepList = new List<Sleep>();
            foreach (Sleep sleep in allSleepList)
            {
                if (sleep.AccessLevel >= accessLevel)
                {
                    sleep.SleepStart = TimeZoneInfo.ConvertTimeFromUtc(sleep.SleepStart, TimeZoneInfo.FindSystemTimeZoneById(userTimeZone));
                    sleep.SleepEnd = TimeZoneInfo.ConvertTimeFromUtc(sleep.SleepEnd, TimeZoneInfo.FindSystemTimeZoneById(userTimeZone));
                    DateTimeOffset sOffset = new DateTimeOffset(sleep.SleepStart, TimeZoneInfo.FindSystemTimeZoneById(userTimeZone).GetUtcOffset(sleep.SleepStart));
                    DateTimeOffset eOffset = new DateTimeOffset(sleep.SleepEnd, TimeZoneInfo.FindSystemTimeZoneById(userTimeZone).GetUtcOffset(sleep.SleepEnd));
                    sleep.SleepDuration = eOffset - sOffset;
                    sleepList.Add(sleep);
                }
            }

            sleepList = sleepList.OrderBy(s => s.SleepStart).ToList();

            List<Sleep> chartList = new List<Sleep>();
            foreach (Sleep chartItem in sleepList)
            {
                double durationStartDate = 0.0;
                if (chartItem.SleepStart.Date == chartItem.SleepEnd.Date)
                {
                    durationStartDate = durationStartDate + chartItem.SleepDuration.TotalMinutes;
                    Sleep slpItem = chartList.SingleOrDefault(s => s.SleepStart.Date == chartItem.SleepStart.Date);
                    if (slpItem != null)
                    {
                        slpItem.SleepDuration += TimeSpan.FromMinutes(durationStartDate);
                    }
                    else
                    {
                        Sleep newSleep = new Sleep();
                        newSleep.SleepStart = chartItem.SleepStart;
                        newSleep.SleepDuration = TimeSpan.FromMinutes(durationStartDate);
                        chartList.Add(newSleep);
                    }
                }
                else
                {
                    DateTimeOffset sOffset = new DateTimeOffset(chartItem.SleepStart, TimeZoneInfo.FindSystemTimeZoneById(userTimeZone).GetUtcOffset(chartItem.SleepStart));
                    DateTimeOffset s2Offset = new DateTimeOffset(chartItem.SleepStart.Date + TimeSpan.FromDays(1), TimeZoneInfo.FindSystemTimeZoneById(userTimeZone)
                            .GetUtcOffset(chartItem.SleepStart.Date + TimeSpan.FromDays(1)));
                    DateTimeOffset eOffset = new DateTimeOffset(chartItem.SleepEnd, TimeZoneInfo.FindSystemTimeZoneById(userTimeZone).GetUtcOffset(chartItem.SleepEnd));
                    DateTimeOffset e2Offset = new DateTimeOffset(chartItem.SleepEnd.Date, TimeZoneInfo.FindSystemTimeZoneById(userTimeZone)
                            .GetUtcOffset(chartItem.SleepEnd.Date));

                    TimeSpan sDateDuration = s2Offset - sOffset;
                    TimeSpan eDateDuration = eOffset - e2Offset;
                    durationStartDate = chartItem.SleepDuration.TotalMinutes - (eDateDuration.TotalMinutes);
                    double durationEndDate = chartItem.SleepDuration.TotalMinutes - sDateDuration.TotalMinutes;

                    Sleep chartItemOverlappingWithStart = chartList.SingleOrDefault(s => s.SleepStart.Date == chartItem.SleepStart.Date);
                    if (chartItemOverlappingWithStart != null)
                    {
                        chartItemOverlappingWithStart.SleepDuration += TimeSpan.FromMinutes(durationStartDate);
                    }
                    else
                    {
                        Sleep chartItemToAdd = new Sleep();
                        chartItemToAdd.SleepStart = chartItem.SleepStart;
                        chartItemToAdd.SleepDuration = TimeSpan.FromMinutes(durationStartDate);
                        chartList.Add(chartItemToAdd);
                    }

                    Sleep chartItemOverlappingWithEnd = chartList.SingleOrDefault(s => s.SleepStart.Date == chartItem.SleepEnd.Date);
                    if (chartItemOverlappingWithEnd != null)
                    {
                        chartItemOverlappingWithEnd.SleepDuration += TimeSpan.FromMinutes(durationEndDate);
                    }
                    else
                    {
                        Sleep chartItemToAdd = new Sleep();
                        chartItemToAdd.SleepStart = chartItem.SleepEnd;
                        chartItemToAdd.SleepDuration = TimeSpan.FromMinutes(durationEndDate);
                        chartList.Add(chartItemToAdd);
                    }
                }
            }

            return chartList;
        }
    }
}
