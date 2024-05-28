using System;
using System.Collections.Generic;
using System.Linq;
using KinaUna.Data.Models;

namespace KinaUnaProgenyApi.Models
{
    public class SleepDetailsModel
    {
        public List<Sleep> SleepList { get; private set; }

        public void CreateSleepList(Sleep currentSleep, List<Sleep> allSleepList, int accessLevel, int sortOrder, string userTimeZone)
        {
            List<Sleep> sleepList = [];
            foreach (Sleep sleep in allSleepList)
            {
                if (sleep.AccessLevel >= accessLevel)
                {
                    sleepList.Add(sleep);
                }
            }

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
                DateTimeOffset sOffset = new(s.SleepStart, TimeZoneInfo.FindSystemTimeZoneById(userTimeZone).GetUtcOffset(s.SleepStart));
                DateTimeOffset eOffset = new(s.SleepEnd, TimeZoneInfo.FindSystemTimeZoneById(userTimeZone).GetUtcOffset(s.SleepEnd));
                s.SleepDuration = eOffset - sOffset;
            }
        }
    }
}
