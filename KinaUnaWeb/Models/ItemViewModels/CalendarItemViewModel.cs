using System;
using System.Collections.Generic;
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

            SetAccessLevelList();
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

            return eventItem;
        }

        public void SetReminderOffsetList(List<SelectListItem> offsetItems)
        {
            ReminderOffsetsList = offsetItems;
        }
    }
}
