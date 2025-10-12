using KinaUna.Data.Models.DTOs;
using Microsoft.AspNetCore.Mvc.Rendering;
using System;
using System.Collections.Generic;
using System.Text.Json;

namespace KinaUnaWeb.Models.ItemViewModels
{
    /// <summary>
    /// View model for CalendarItem objects.
    /// Extends the BaseItemsViewModel.
    /// </summary>
    public class CalendarItemViewModel: BaseItemsViewModel
    {
        public CalendarItem CalendarItem { get; set; } = new();
        public List<CalendarReminder> CalendarReminders { get; set; } = [];
        public List<SelectListItem> ReminderOffsetsList { get; set; } = [];
        public List<SelectListItem> RecurrenceFrequencyList { get; set; } = [];
        public List<SelectListItem> MonthsSelectList { get; set; } = [];
        public List<SelectListItem> EndOptionsList { get; set; } = [];
        
        public List<bool> MonthlyByDayPrefixList = [false, false, false, false, false, false]; // First, second, third, fourth, fifth, last.
        public int RepeatMonthlyType { get; set; }
        public int RepeatYearlyType { get; set; }

        /// <summary>
        /// Parameterless constructor. Needed for initialization of the view model when objects are created in Razor views/passed as parameters in POST methods.
        /// </summary>
        public CalendarItemViewModel()
        {
            ProgenyList = [];
            FamilyList = [];
        }

        public CalendarItemViewModel(BaseItemsViewModel baseItemsViewModel)
        {
            SetBaseProperties(baseItemsViewModel);
            ProgenyList = [];
            FamilyList = [];
        }

        public void SetProgenyList()
        {
            CalendarItem.ProgenyId = CurrentProgenyId;
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

        public void SetFamilyList()
        {
            CalendarItem.FamilyId = CurrentFamilyId;
            foreach (SelectListItem item in FamilyList)
            {
                if (item.Value == CurrentFamilyId.ToString())
                {
                    item.Selected = true;
                }
                else
                {
                    item.Selected = false;
                }
            }
        }
        /// <summary>
        /// Sets the CalendarItem property of this view model.
        /// Converts start and end times from UTC to the user's timezone.
        /// </summary>
        /// <param name="eventItem">The CalendarItem with the properties to set for the CalendarItem property. Start and end times should be in UTC timezone.</param>
        public void SetCalendarItem(CalendarItem eventItem)
        {
            CalendarItem.EventId = eventItem.EventId;
            CalendarItem.ProgenyId = eventItem.ProgenyId;
            CalendarItem.FamilyId = eventItem.FamilyId;
            CalendarItem.Title = eventItem.Title;
            CalendarItem.AllDay = eventItem.AllDay;
            if (eventItem.StartTime.HasValue && eventItem.EndTime.HasValue)
            {
                CalendarItem.StartTime = TimeZoneInfo.ConvertTimeFromUtc(eventItem.StartTime.Value, TimeZoneInfo.FindSystemTimeZoneById(CurrentUser.Timezone));
                CalendarItem.EndTime = TimeZoneInfo.ConvertTimeFromUtc(eventItem.EndTime.Value, TimeZoneInfo.FindSystemTimeZoneById(CurrentUser.Timezone));
            }

            CalendarItem.Notes = eventItem.Notes;
            CalendarItem.Location = eventItem.Location;
            CalendarItem.Context = eventItem.Context;
            CalendarItem.Author = eventItem.Author;
            CalendarItem.UId = eventItem.UId;
            CalendarItem.RecurrenceRuleId = eventItem.RecurrenceRuleId;
            CalendarItem.ItemPerMission = eventItem.ItemPerMission;

            if (eventItem.RecurrenceRuleId != 0)
            {
                CalendarItem.RecurrenceRule = eventItem.RecurrenceRule;
            }
            else
            {
                CalendarItem.RecurrenceRule = new();
                if (CalendarItem.StartTime.HasValue)
                {
                    CalendarItem.RecurrenceRule.ByMonthDay = CalendarItem.StartTime.Value.Day.ToString();
                }
            }

            int frequencyConverted = 0;
            if (CalendarItem.RecurrenceRule.Frequency == 1)
            {
                frequencyConverted = 1;
            }

            if (CalendarItem.RecurrenceRule.Frequency == 2)
            {
                frequencyConverted = 2;
            }

            if (CalendarItem.RecurrenceRule.Frequency == 3)
            {
                frequencyConverted = 3;
                RepeatMonthlyType = 1;
            }
            
            if (CalendarItem.RecurrenceRule.Frequency == 4)
            {
                frequencyConverted = 3;
                RepeatMonthlyType = 0;
            }
            
            if (CalendarItem.RecurrenceRule.Frequency == 5)
            {
                frequencyConverted = 4;
                RepeatYearlyType = 1;
            }

            if (CalendarItem.RecurrenceRule.Frequency == 6)
            {
                frequencyConverted = 4;
                RepeatYearlyType = 0;
            }

            CalendarItem.RecurrenceRule.Frequency = frequencyConverted;

            SetRecurrenceFrequencyList();
            SetEndOptionsList();
            SetMonthlyByDayPrefixList();
            SetMonthsSelectList();
        }

        /// <summary>
        /// Sets the CalendarReminder property of this view model.
        /// Converts NotifyTime from UTC to the user's timezone.
        /// </summary>
        /// <param name="calendarReminders">The list of CalendarReminder objects to set as the CalendarReminders property. NotifyTime should be in UTC timezone.</param>
        public void SetCalendarReminders(List<CalendarReminder> calendarReminders)
        {
            foreach (CalendarReminder calendarReminder in calendarReminders)
            {
                calendarReminder.NotifyTime = TimeZoneInfo.ConvertTimeFromUtc(calendarReminder.NotifyTime, TimeZoneInfo.FindSystemTimeZoneById(CurrentUser.Timezone));
            }

            CalendarReminders = calendarReminders;
        }
        
        /// <summary>
        /// Creates a new CalendarItem object from the properties of this view model.
        /// Converts start and end times from the user's timezone to UTC.
        /// </summary>
        /// <returns>CalendarItem object. Start and end times are in UTC timezone.</returns>
        public CalendarItem CreateCalendarItem()
        {
            CalendarItem eventItem = new()
            {
                EventId = CalendarItem.EventId,
                ProgenyId = CalendarItem.ProgenyId,
                FamilyId = CalendarItem.FamilyId,
                Title = CalendarItem.Title,
                Notes = CalendarItem.Notes,
                ItemPermissionsDtoList = string.IsNullOrWhiteSpace(ItemPermissionsListAsString) ? [] : JsonSerializer.Deserialize<List<ItemPermissionDto>>(ItemPermissionsListAsString)
            };

            if (CalendarItem.StartTime != null && CalendarItem.EndTime != null)
            {
                eventItem.StartTime = TimeZoneInfo.ConvertTimeToUtc(CalendarItem.StartTime.Value,
                    TimeZoneInfo.FindSystemTimeZoneById(CurrentUser.Timezone));
                eventItem.EndTime = TimeZoneInfo.ConvertTimeToUtc(CalendarItem.EndTime.Value,
                    TimeZoneInfo.FindSystemTimeZoneById(CurrentUser.Timezone));
            }
            
            eventItem.Location = CalendarItem.Location;
            eventItem.Context = CalendarItem.Context;
            eventItem.AllDay = CalendarItem.AllDay;
            eventItem.Author = CalendarItem.Author;
            eventItem.UId = CalendarItem.UId;
            eventItem.RecurrenceRuleId = CalendarItem.RecurrenceRuleId;
            eventItem.RecurrenceRule = CalendarItem.RecurrenceRule;
            if (CalendarItem.RecurrenceRule.Frequency == 3)
            {
                if(RepeatMonthlyType == 1)
                {
                    eventItem.RecurrenceRule.Frequency = 3;
                }
                else
                {
                    eventItem.RecurrenceRule.Frequency = 4;
                }
            }else if (CalendarItem.RecurrenceRule.Frequency == 4)
            {
                if(RepeatYearlyType == 1)
                {
                    eventItem.RecurrenceRule.Frequency = 5;
                }
                else
                {
                    eventItem.RecurrenceRule.Frequency = 6;
                }
            }

            return eventItem;
        }

        public void SetReminderOffsetList(List<SelectListItem> offsetItems)
        {
            ReminderOffsetsList = offsetItems;
        }

        public void SetRecurrenceFrequencyList()
        {
            List<SelectListItem> frequencyItems =
            [
                new() { Value = "0", Text = "Never", Selected = false },
                new() { Value = "1", Text = "Daily", Selected = false },
                new() { Value = "2", Text = "Weekly", Selected = false },
                new() { Value = "3", Text = "Monthly", Selected = false },
                new() { Value = "4", Text = "Yearly", Selected = false }
            ];
            
            RecurrenceFrequencyList = frequencyItems;
            
            RecurrenceFrequencyList[CalendarItem.RecurrenceRule?.Frequency ?? 0].Selected = true;
        }

        public void SetMonthsSelectList()
        {
            List<SelectListItem> monthsList =
            [
                new() { Value = "1", Text = "January", Selected = false },
                new() { Value = "2", Text = "February", Selected = false },
                new() { Value = "3", Text = "March", Selected = false },
                new() { Value = "4", Text = "April", Selected = false },
                new() { Value = "5", Text = "May", Selected = false },
                new() { Value = "6", Text = "June", Selected = false },
                new() { Value = "7", Text = "July", Selected = false },
                new() { Value = "8", Text = "August", Selected = false },
                new() { Value = "9", Text = "September", Selected = false },
                new() { Value = "10", Text = "October", Selected = false },
                new() { Value = "11", Text = "November", Selected = false },
                new() { Value = "12", Text = "December", Selected = false }
            ];
            
            MonthsSelectList = monthsList;
            bool selectedMonthParsed = int.TryParse(CalendarItem.RecurrenceRule?.ByMonth, out int selectedMonth);
            if (selectedMonthParsed && selectedMonth is > 0 and < 13)
            {
                MonthsSelectList[selectedMonth - 1].Selected = true;
            }
            else
            {
                int indexOfStart = 0;
                if (CalendarItem.StartTime.HasValue)
                {
                    indexOfStart = CalendarItem.StartTime.Value.Month - 1;
                    MonthsSelectList[indexOfStart].Selected = true;
                }
                else
                {
                    MonthsSelectList[0].Selected = true;
                }

                if (CalendarItem.RecurrenceRule != null)
                {
                    CalendarItem.RecurrenceRule.ByMonth = MonthsSelectList[indexOfStart].Value;
                }
            }
        }

        public void SetEndOptionsList()
        {
            List<SelectListItem> endOptions =
            [
                new() { Value = "0", Text = "Never", Selected = false },
                new() { Value = "1", Text = "On date", Selected = false },
                new() { Value = "2", Text = "After count", Selected = false }
            ];

            EndOptionsList = endOptions;

            if (CalendarItem.RecurrenceRule != null)
            {
                EndOptionsList[CalendarItem.RecurrenceRule.EndOption].Selected = true;
            }
        }

        public void SetMonthlyByDayPrefixList()
        {
            if (CalendarItem.RecurrenceRule == null) return;
            
            if (string.IsNullOrWhiteSpace(CalendarItem.RecurrenceRule.ByDay)) return;

            string[] byDayParts = CalendarItem.RecurrenceRule.ByDay.Split(",");
            foreach (string byDayPart in byDayParts)
            {
                if (byDayPart.StartsWith('1'))
                {
                    MonthlyByDayPrefixList[0] = true;
                }

                if (byDayPart.StartsWith('2'))
                {
                    MonthlyByDayPrefixList[1] = true;
                }

                if (byDayPart.StartsWith('3'))
                {
                    MonthlyByDayPrefixList[2] = true;
                }

                if (byDayPart.StartsWith('4'))
                {
                    MonthlyByDayPrefixList[3] = true;
                }

                if (byDayPart.StartsWith('5'))
                {
                    MonthlyByDayPrefixList[4] = true;
                }

                if (byDayPart.StartsWith("-1"))
                {
                    MonthlyByDayPrefixList[5] = true;
                }
            }
        }
    }
}
