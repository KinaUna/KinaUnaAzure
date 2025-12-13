using System;
using System.Collections.Generic;
using System.Linq;
using KinaUna.Data.Models;

namespace KinaUnaProgenyApi.Models
{
    public class SleepDetailsModel
    {
        public List<Sleep> SleepList { get; private set; }

        public void CreateSleepList(Sleep currentSleep, List<Sleep> sleepList, int sortOrder, string userTimeZone)
        {
            if (sortOrder == 0)
            {
                sleepList = [.. sleepList.OrderBy(s => s.SleepStart)];
            }
            else
            {
                sleepList = [.. sleepList.OrderByDescending(s => s.SleepStart)];
            }

            SleepList = [currentSleep];

            int currentSleepIndex = sleepList.IndexOf(currentSleep);
            if (currentSleepIndex > 0)
            {
                SleepList.Add(sleepList[currentSleepIndex - 1]);
            }
            else
            {
                SleepList.Add(sleepList[^1]);
            }

            if (sleepList.Count < currentSleepIndex + 1)
            {
                SleepList.Add(sleepList[currentSleepIndex + 1]);
            }
            else
            {
                SleepList.Add(sleepList[0]);
            }

            foreach (Sleep s in SleepList)
            {
                TimeZoneInfo timeZone = TimeZoneInfo.FindSystemTimeZoneById(userTimeZone);
                DateTime localStart = TimeZoneInfo.ConvertTimeFromUtc(s.SleepStart, timeZone);
                DateTime localEnd = TimeZoneInfo.ConvertTimeFromUtc(s.SleepEnd, timeZone);
                
                DateTimeOffset sOffset = new(localStart, timeZone.GetUtcOffset(s.SleepStart));
                DateTimeOffset eOffset = new(localEnd, timeZone.GetUtcOffset(s.SleepEnd));
                s.SleepDuration = eOffset - sOffset;
            }
        }
    }
}
