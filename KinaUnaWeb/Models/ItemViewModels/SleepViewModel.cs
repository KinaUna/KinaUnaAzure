using Microsoft.AspNetCore.Mvc.Rendering;
using System;
using System.Collections.Generic;
using KinaUna.Data.Models;
using System.Linq;

namespace KinaUnaWeb.Models.ItemViewModels
{
    public class SleepViewModel: BaseItemsViewModel
    {
        public List<SelectListItem> ProgenyList { get; set; }
        public List<SelectListItem> RatingList { get; set; }
        public List<Sleep> SleepList { get; set; }
        public List<Sleep> ChartList { get; set; }
        public List<SelectListItem> AccessLevelListEn { get; set; }
        public List<SelectListItem> AccessLevelListDa { get; set; }
        public List<SelectListItem> AccessLevelListDe { get; set; }
        public TimeSpan SleepTotal { get; set; }
        public TimeSpan TotalAverage { get; set; }
        public TimeSpan SleepLastMonth { get; set; }
        public TimeSpan LastMonthAverage { get; set; }
        public TimeSpan SleepLastYear { get; set; }
        public TimeSpan LastYearAverage { get; set; }

        public Sleep SleepItem { get; set; } = new();

        public SleepViewModel()
        {
            ProgenyList = new List<SelectListItem>();
            AccessLevelList aclList = new();
            AccessLevelListEn = aclList.AccessLevelListEn;
            AccessLevelListDa = aclList.AccessLevelListDa;
            AccessLevelListDe = aclList.AccessLevelListDe;
        }

        public SleepViewModel(BaseItemsViewModel baseItemsViewModel)
        {
            SetBaseProperties(baseItemsViewModel);
        }

        public void SetProgenyList()
        {
            SleepItem.ProgenyId = CurrentProgenyId;
            foreach (SelectListItem item in ProgenyList)
            {
                if (item.Value == CurrentProgenyId.ToString())
                {
                    item.Selected = true;
                }
                else
                {
                    item.Selected = false;
                }
            }
        }

        public void SetAccessLevelList()
        {
            AccessLevelList accessLevelList = new();
            AccessLevelListEn = accessLevelList.AccessLevelListEn;
            AccessLevelListDa = accessLevelList.AccessLevelListDa;
            AccessLevelListDe = accessLevelList.AccessLevelListDe;

            AccessLevelListEn[SleepItem.AccessLevel].Selected = true;
            AccessLevelListDa[SleepItem.AccessLevel].Selected = true;
            AccessLevelListDe[SleepItem.AccessLevel].Selected = true;

            if (LanguageId == 2)
            {
                AccessLevelListEn = AccessLevelListDe;
            }

            if (LanguageId == 3)
            {
                AccessLevelListEn = AccessLevelListDa;
            }
        }

        public void SetRatingList()
        {
            RatingList = new List<SelectListItem>();
            SelectListItem selItem1 = new()
            {
                Text = "1",
                Value = "1"
            };
            SelectListItem selItem2 = new()
            {
                Text = "2",
                Value = "2"
            };
            SelectListItem selItem3 = new()
            {
                Text = "3",
                Value = "3"
            };
            SelectListItem selItem4 = new()
            {
                Text = "4",
                Value = "4"
            };
            SelectListItem selItem5 = new()
            {
                Text = "5",
                Value = "5"
            };
            RatingList.Add(selItem1);
            RatingList.Add(selItem2);
            RatingList.Add(selItem3);
            RatingList.Add(selItem4);
            RatingList.Add(selItem5);
            RatingList[SleepItem.SleepRating - 1].Selected = true;
        }

        public Sleep CreateSleep()
        {
            Sleep sleep = new()
            {
                ProgenyId = SleepItem.ProgenyId,
                Progeny = CurrentProgeny,
                CreatedDate = SleepItem.CreatedDate,
                SleepStart = TimeZoneInfo.ConvertTimeToUtc(SleepItem.SleepStart, TimeZoneInfo.FindSystemTimeZoneById(CurrentUser.Timezone)),
                SleepEnd = TimeZoneInfo.ConvertTimeToUtc(SleepItem.SleepEnd, TimeZoneInfo.FindSystemTimeZoneById(CurrentUser.Timezone)),
                SleepRating = SleepItem.SleepRating
            };

            if (sleep.SleepRating == 0)
            {
                sleep.SleepRating = 3;
            }

            sleep.SleepNotes = SleepItem.SleepNotes;
            sleep.AccessLevel = SleepItem.AccessLevel;
            sleep.Author = SleepItem.Author;

            return sleep;
        }

        public void SetPropertiesFromSleepItem(Sleep sleep)
        {
            SleepItem.ProgenyId = sleep.ProgenyId;
            SleepItem.Progeny = CurrentProgeny;
            SleepItem.SleepId = sleep.SleepId;
            SleepItem.AccessLevel = sleep.AccessLevel;
            SleepItem.Author = sleep.Author;
            SleepItem.CreatedDate = sleep.CreatedDate;
            SleepItem.SleepStart = TimeZoneInfo.ConvertTimeFromUtc(sleep.SleepStart, TimeZoneInfo.FindSystemTimeZoneById(CurrentUser.Timezone));
            SleepItem.SleepEnd = TimeZoneInfo.ConvertTimeFromUtc(sleep.SleepEnd, TimeZoneInfo.FindSystemTimeZoneById(CurrentUser.Timezone));
            SleepItem.SleepRating = sleep.SleepRating;
            if (SleepItem.SleepRating == 0)
            {
                SleepItem.SleepRating = 3;
            }

            SleepItem.SleepNotes = sleep.SleepNotes;
            SleepItem.Progeny = CurrentProgeny;
        }

        public void ProcessSleepListData(List<Sleep> sleepList)
        {
            SleepList = new List<Sleep>();
            ChartList = new List<Sleep>();
            SleepTotal = TimeSpan.Zero;
            SleepLastYear = TimeSpan.Zero;
            SleepLastMonth = TimeSpan.Zero;

            DateTime yearAgo = new(DateTime.UtcNow.Year - 1, DateTime.UtcNow.Month, DateTime.UtcNow.Day, DateTime.UtcNow.Hour, DateTime.UtcNow.Minute, 0);
            DateTime monthAgo = DateTime.UtcNow - TimeSpan.FromDays(30);
            if (sleepList.Count != 0)
            {
                foreach (Sleep sleep in sleepList)
                {
                    if (sleep.AccessLevel >= CurrentAccessLevel)
                    {
                        // Calculate average sleep.
                        bool isLessThanYear = sleep.SleepEnd > yearAgo;
                        bool isLessThanMonth = sleep.SleepEnd > monthAgo;
                        sleep.SleepStart = TimeZoneInfo.ConvertTimeFromUtc(sleep.SleepStart, TimeZoneInfo.FindSystemTimeZoneById(CurrentUser.Timezone));
                        sleep.SleepEnd = TimeZoneInfo.ConvertTimeFromUtc(sleep.SleepEnd, TimeZoneInfo.FindSystemTimeZoneById(CurrentUser.Timezone));
                        DateTimeOffset startOffset = new(sleep.SleepStart, TimeZoneInfo.FindSystemTimeZoneById(CurrentUser.Timezone).GetUtcOffset(sleep.SleepStart));
                        DateTimeOffset endOffset = new(sleep.SleepEnd, TimeZoneInfo.FindSystemTimeZoneById(CurrentUser.Timezone).GetUtcOffset(sleep.SleepEnd));
                        sleep.SleepDuration = endOffset - startOffset;

                        SleepTotal += sleep.SleepDuration;
                        if (isLessThanYear)
                        {
                            SleepLastYear += sleep.SleepDuration;
                        }

                        if (isLessThanMonth)
                        {
                            SleepLastMonth += sleep.SleepDuration;
                        }

                        // Calculate chart values
                        double durationStartDate = 0.0;
                        if (sleep.SleepStart.Date == sleep.SleepEnd.Date)
                        {
                            durationStartDate += sleep.SleepDuration.TotalMinutes;
                            Sleep slpItem = ChartList.SingleOrDefault(s => s.SleepStart.Date == sleep.SleepStart.Date);
                            if (slpItem != null)
                            {
                                slpItem.SleepDuration += TimeSpan.FromMinutes(durationStartDate);
                            }
                            else
                            {
                                Sleep newSleep = new()
                                {
                                    SleepStart = sleep.SleepStart,
                                    SleepDuration = TimeSpan.FromMinutes(durationStartDate)
                                };
                                ChartList.Add(newSleep);
                            }
                        }
                        else
                        {
                            DateTimeOffset sOffset = new(sleep.SleepStart, TimeZoneInfo.FindSystemTimeZoneById(CurrentUser.Timezone).GetUtcOffset(sleep.SleepStart));
                            DateTimeOffset s2Offset = new(sleep.SleepStart.Date + TimeSpan.FromDays(1), TimeZoneInfo.FindSystemTimeZoneById(CurrentUser.Timezone)
                                    .GetUtcOffset(sleep.SleepStart.Date + TimeSpan.FromDays(1)));
                            DateTimeOffset eOffset = new(sleep.SleepEnd, TimeZoneInfo.FindSystemTimeZoneById(CurrentUser.Timezone).GetUtcOffset(sleep.SleepEnd));
                            DateTimeOffset e2Offset = new(sleep.SleepEnd.Date, TimeZoneInfo.FindSystemTimeZoneById(CurrentUser.Timezone)
                                    .GetUtcOffset(sleep.SleepEnd.Date));
                            TimeSpan sDateDuration = s2Offset - sOffset;
                            TimeSpan eDateDuration = eOffset - e2Offset;
                            durationStartDate = sleep.SleepDuration.TotalMinutes - (eDateDuration.TotalMinutes);
                            double durationEndDate = sleep.SleepDuration.TotalMinutes - sDateDuration.TotalMinutes;
                            Sleep chartItemOverlappingWithStart = ChartList.SingleOrDefault(s => s.SleepStart.Date == sleep.SleepStart.Date);
                            if (chartItemOverlappingWithStart != null)
                            {
                                chartItemOverlappingWithStart.SleepDuration += TimeSpan.FromMinutes(durationStartDate);
                            }
                            else
                            {
                                Sleep chartItemToAdd = new()
                                {
                                    SleepStart = sleep.SleepStart,
                                    SleepDuration = TimeSpan.FromMinutes(durationStartDate)
                                };
                                ChartList.Add(chartItemToAdd);
                            }

                            Sleep chartItemOverlappingWithEnd = ChartList.SingleOrDefault(s => s.SleepStart.Date == sleep.SleepEnd.Date);
                            if (chartItemOverlappingWithEnd != null)
                            {
                                chartItemOverlappingWithEnd.SleepDuration += TimeSpan.FromMinutes(durationEndDate);
                            }
                            else
                            {
                                Sleep chartItemToAdd = new()
                                {
                                    SleepStart = sleep.SleepEnd,
                                    SleepDuration = TimeSpan.FromMinutes(durationEndDate)
                                };
                                ChartList.Add(chartItemToAdd);
                            }
                        }

                        SleepList.Add(sleep);
                    }
                }
                SleepList = SleepList.OrderBy(s => s.SleepStart).ToList();
                ChartList = ChartList.OrderBy(s => s.SleepStart).ToList();

                TotalAverage = SleepTotal / (DateTime.UtcNow - SleepList.First().SleepStart).TotalDays;
                LastYearAverage = SleepLastYear / (DateTime.UtcNow - yearAgo).TotalDays;
                LastMonthAverage = SleepLastMonth / 30;
            }
        }

        public void ProcessSleepCalendarList(List<Sleep> sleepList)
        {
            SleepList = new List<Sleep>();

            if (sleepList.Count != 0)
            {
                foreach (Sleep sleep in sleepList)
                {
                    if (sleep.AccessLevel >= CurrentAccessLevel)
                    {
                        sleep.SleepStart = TimeZoneInfo.ConvertTimeFromUtc(sleep.SleepStart, TimeZoneInfo.FindSystemTimeZoneById(CurrentUser.Timezone));
                        sleep.SleepEnd = TimeZoneInfo.ConvertTimeFromUtc(sleep.SleepEnd, TimeZoneInfo.FindSystemTimeZoneById(CurrentUser.Timezone));
                        sleep.SleepDuration = sleep.SleepEnd - sleep.SleepStart;
                        sleep.StartString = sleep.SleepStart.ToString("yyyy-MM-dd") + "T" + sleep.SleepStart.ToString("HH:mm:ss");
                        sleep.EndString = sleep.SleepEnd.ToString("yyyy-MM-dd") + "T" + sleep.SleepEnd.ToString("HH:mm:ss");
                        SleepList.Add(sleep);
                    }
                }

                SleepList = SleepList.OrderBy(s => s.SleepStart).ToList();
            }
        }
    }
}
