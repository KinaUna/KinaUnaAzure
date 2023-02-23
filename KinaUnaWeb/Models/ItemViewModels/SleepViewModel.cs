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

        public Sleep SleepItem { get; set; } = new Sleep();

        public SleepViewModel()
        {
            ProgenyList = new List<SelectListItem>();
            AccessLevelList aclList = new AccessLevelList();
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
            AccessLevelList accessLevelList = new AccessLevelList();
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
            SelectListItem selItem1 = new SelectListItem();
            selItem1.Text = "1";
            selItem1.Value = "1";
            SelectListItem selItem2 = new SelectListItem();
            selItem2.Text = "2";
            selItem2.Value = "2";
            SelectListItem selItem3 = new SelectListItem();
            selItem3.Text = "3";
            selItem3.Value = "3";
            SelectListItem selItem4 = new SelectListItem();
            selItem4.Text = "4";
            selItem4.Value = "4";
            SelectListItem selItem5 = new SelectListItem();
            selItem5.Text = "5";
            selItem5.Value = "5";
            RatingList.Add(selItem1);
            RatingList.Add(selItem2);
            RatingList.Add(selItem3);
            RatingList.Add(selItem4);
            RatingList.Add(selItem5);
            RatingList[SleepItem.SleepRating - 1].Selected = true;
        }

        public Sleep CreateSleep()
        {
            Sleep sleep = new Sleep();
            sleep.ProgenyId = SleepItem.ProgenyId;
            sleep.Progeny = CurrentProgeny;
            sleep.CreatedDate = SleepItem.CreatedDate;
            sleep.SleepStart = TimeZoneInfo.ConvertTimeToUtc(SleepItem.SleepStart, TimeZoneInfo.FindSystemTimeZoneById(CurrentUser.Timezone));
            sleep.SleepEnd = TimeZoneInfo.ConvertTimeToUtc(SleepItem.SleepEnd, TimeZoneInfo.FindSystemTimeZoneById(CurrentUser.Timezone));

            sleep.SleepRating = SleepItem.SleepRating;
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

            DateTime yearAgo = new DateTime(DateTime.UtcNow.Year - 1, DateTime.UtcNow.Month, DateTime.UtcNow.Day, DateTime.UtcNow.Hour, DateTime.UtcNow.Minute, 0);
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
                        DateTimeOffset startOffset = new DateTimeOffset(sleep.SleepStart, TimeZoneInfo.FindSystemTimeZoneById(CurrentUser.Timezone).GetUtcOffset(sleep.SleepStart));
                        DateTimeOffset endOffset = new DateTimeOffset(sleep.SleepEnd, TimeZoneInfo.FindSystemTimeZoneById(CurrentUser.Timezone).GetUtcOffset(sleep.SleepEnd));
                        sleep.SleepDuration = endOffset - startOffset;

                        SleepTotal = SleepTotal + sleep.SleepDuration;
                        if (isLessThanYear)
                        {
                            SleepLastYear = SleepLastYear + sleep.SleepDuration;
                        }

                        if (isLessThanMonth)
                        {
                            SleepLastMonth = SleepLastMonth + sleep.SleepDuration;
                        }

                        // Calculate chart values
                        double durationStartDate = 0.0;
                        if (sleep.SleepStart.Date == sleep.SleepEnd.Date)
                        {
                            durationStartDate = durationStartDate + sleep.SleepDuration.TotalMinutes;
                            Sleep slpItem = ChartList.SingleOrDefault(s => s.SleepStart.Date == sleep.SleepStart.Date);
                            if (slpItem != null)
                            {
                                slpItem.SleepDuration += TimeSpan.FromMinutes(durationStartDate);
                            }
                            else
                            {
                                Sleep newSleep = new Sleep();
                                newSleep.SleepStart = sleep.SleepStart;
                                newSleep.SleepDuration = TimeSpan.FromMinutes(durationStartDate);
                                ChartList.Add(newSleep);
                            }
                        }
                        else
                        {
                            DateTimeOffset sOffset = new DateTimeOffset(sleep.SleepStart, TimeZoneInfo.FindSystemTimeZoneById(CurrentUser.Timezone).GetUtcOffset(sleep.SleepStart));
                            DateTimeOffset s2Offset = new DateTimeOffset(sleep.SleepStart.Date + TimeSpan.FromDays(1), TimeZoneInfo.FindSystemTimeZoneById(CurrentUser.Timezone)
                                    .GetUtcOffset(sleep.SleepStart.Date + TimeSpan.FromDays(1)));
                            DateTimeOffset eOffset = new DateTimeOffset(sleep.SleepEnd, TimeZoneInfo.FindSystemTimeZoneById(CurrentUser.Timezone).GetUtcOffset(sleep.SleepEnd));
                            DateTimeOffset e2Offset = new DateTimeOffset(sleep.SleepEnd.Date, TimeZoneInfo.FindSystemTimeZoneById(CurrentUser.Timezone)
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
                                Sleep chartItemToAdd = new Sleep();
                                chartItemToAdd.SleepStart = sleep.SleepStart;
                                chartItemToAdd.SleepDuration = TimeSpan.FromMinutes(durationStartDate);
                                ChartList.Add(chartItemToAdd);
                            }

                            Sleep chartItemOverlappingWithEnd = ChartList.SingleOrDefault(s => s.SleepStart.Date == sleep.SleepEnd.Date);
                            if (chartItemOverlappingWithEnd != null)
                            {
                                chartItemOverlappingWithEnd.SleepDuration += TimeSpan.FromMinutes(durationEndDate);
                            }
                            else
                            {
                                Sleep chartItemToAdd = new Sleep();
                                chartItemToAdd.SleepStart = sleep.SleepEnd;
                                chartItemToAdd.SleepDuration = TimeSpan.FromMinutes(durationEndDate);
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
