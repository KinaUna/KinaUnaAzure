using System;
using System.Collections.Generic;
using System.Linq;
using KinaUna.Data.Models;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace KinaUnaWeb.Models.ItemViewModels
{
    /// <summary>
    /// View model for CalendarItem objects.
    /// Extends the BaseItemsViewModel.
    /// </summary>
    public class CalendarItemViewModel: BaseItemsViewModel
    {
        public List<SelectListItem> ProgenyList { get; set; } = [];
        public List<SelectListItem> AccessLevelListEn { get; set; }
        public List<SelectListItem> AccessLevelListDa { get; set; }
        public List<SelectListItem> AccessLevelListDe { get; set; }
        public CalendarItem CalendarItem { get; set; } = new();
        public List<CalendarReminder> CalendarReminders { get; set; } = [];
        public List<SelectListItem> ReminderOffsetsList { get; set; } = [];
        public List<SelectListItem> RecurrenceFrequencyList { get; set; } = [];
        public List<SelectListItem> MonthsSelectList { get; set; } = [];
        public List<SelectListItem> EndOptionsList { get; set; } = [];
        
        public List<bool> MonthlyByDayPrefixList = [false, false, false, false, false, false]; // First, second, third, fourth, fifth, last.

        /// <summary>
        /// Parameterless constructor. Needed for initialization of the view model when objects are created in Razor views/passed as parameters in POST methods.
        /// </summary>
        public CalendarItemViewModel()
        {
            
        }

        public CalendarItemViewModel(BaseItemsViewModel baseItemsViewModel)
        {
            SetBaseProperties(baseItemsViewModel);
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
            CalendarItem.AccessLevel = eventItem.AccessLevel;
            CalendarItem.Author = eventItem.Author;
            CalendarItem.UId = eventItem.UId;
            CalendarItem.RecurrenceRuleId = eventItem.RecurrenceRuleId;

            if (eventItem.RecurrenceRuleId != 0)
            {
                CalendarItem.RecurrenceRule = eventItem.RecurrenceRule;
            }
            else
            {
                CalendarItem.RecurrenceRule = new();
            }

            SetAccessLevelList();
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
        /// Updates the selected progeny in the ProgenyList to CurrentProgenyId.
        /// </summary>
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

        /// <summary>
        /// Creates access level lists, sets the selected access level to the current CalendarItem's access level.
        /// </summary>
        public void SetAccessLevelList()
        {
            AccessLevelList accessLevelList = new();
            AccessLevelListEn = accessLevelList.AccessLevelListEn;
            AccessLevelListDa = accessLevelList.AccessLevelListDa;
            AccessLevelListDe = accessLevelList.AccessLevelListDe;

            AccessLevelListEn[CalendarItem.AccessLevel].Selected = true;
            AccessLevelListDa[CalendarItem.AccessLevel].Selected = true;
            AccessLevelListDe[CalendarItem.AccessLevel].Selected = true;

            if (LanguageId == 2)
            {
                AccessLevelListEn = AccessLevelListDe;
            }

            if (LanguageId == 3)
            {
                AccessLevelListEn = AccessLevelListDa;
            }
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
                Title = CalendarItem.Title,
                Notes = CalendarItem.Notes
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
            eventItem.AccessLevel = CalendarItem.AccessLevel;
            eventItem.Author = CalendarItem.Author;
            eventItem.UId = CalendarItem.UId;
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
                new SelectListItem { Value = "0", Text = "Never", Selected = false },
                new SelectListItem { Value = "1", Text = "Daily", Selected = false },
                new SelectListItem { Value = "2", Text = "Weekly", Selected = false },
                new SelectListItem { Value = "3", Text = "Monthly", Selected = false },
                new SelectListItem { Value = "4", Text = "Yearly", Selected = false }
            ];
            
            RecurrenceFrequencyList = frequencyItems;
            
            RecurrenceFrequencyList[CalendarItem.RecurrenceRule?.Frequency ?? 0].Selected = true;
        }

        public void SetMonthsSelectList()
        {
            List<SelectListItem> monthsList =
            [
                new SelectListItem { Value = "1", Text = "January", Selected = false },
                new SelectListItem { Value = "2", Text = "February", Selected = false },
                new SelectListItem { Value = "3", Text = "March", Selected = false },
                new SelectListItem { Value = "4", Text = "April", Selected = false },
                new SelectListItem { Value = "5", Text = "May", Selected = false },
                new SelectListItem { Value = "6", Text = "June", Selected = false },
                new SelectListItem { Value = "7", Text = "July", Selected = false },
                new SelectListItem { Value = "8", Text = "August", Selected = false },
                new SelectListItem { Value = "9", Text = "September", Selected = false },
                new SelectListItem { Value = "10", Text = "October", Selected = false },
                new SelectListItem { Value = "11", Text = "November", Selected = false },
                new SelectListItem { Value = "12", Text = "December", Selected = false }
            ];
            
            MonthsSelectList = monthsList;
            bool selectedMonthParsed = int.TryParse(CalendarItem.RecurrenceRule?.ByMonth, out int selectedMonth);
            if (selectedMonthParsed && selectedMonth is > 0 and < 13)
            {
                MonthsSelectList[selectedMonth - 1].Selected = true;
            }
            else
            {
                MonthsSelectList[0].Selected = true;
            }
        }

        public void SetEndOptionsList()
        {
            List<SelectListItem> endOptions =
            [
                new SelectListItem { Value = "0", Text = "Never", Selected = false },
                new SelectListItem { Value = "1", Text = "On date", Selected = false },
                new SelectListItem { Value = "2", Text = "After count", Selected = false }
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
                if (byDayPart.StartsWith("1"))
                {
                    MonthlyByDayPrefixList[0] = true;
                }

                if (byDayPart.StartsWith("2"))
                {
                    MonthlyByDayPrefixList[1] = true;
                }

                if (byDayPart.StartsWith("3"))
                {
                    MonthlyByDayPrefixList[2] = true;
                }

                if (byDayPart.StartsWith("4"))
                {
                    MonthlyByDayPrefixList[3] = true;
                }

                if (byDayPart.StartsWith("5"))
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
