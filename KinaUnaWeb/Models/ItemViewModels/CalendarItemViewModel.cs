using System;
using System.Collections.Generic;
using KinaUna.Data.Models;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace KinaUnaWeb.Models.ItemViewModels
{
    public class CalendarItemViewModel: BaseItemsViewModel
    {
        public List<SelectListItem> ProgenyList { get; set; }
        public List<SelectListItem> AccessLevelListEn { get; set; }
        public List<SelectListItem> AccessLevelListDa { get; set; }
        public List<SelectListItem> AccessLevelListDe { get; set; }
        public CalendarItem CalendarItem { get; set; } = new CalendarItem();

        public CalendarItemViewModel()
        {
            ProgenyList = new List<SelectListItem>();
        }

        public CalendarItemViewModel(BaseItemsViewModel baseItemsViewModel)
        {
            SetBaseProperties(baseItemsViewModel);

            ProgenyList = new List<SelectListItem>();
        }
        
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

        public void SetAccessLevelList()
        {
            AccessLevelList accessLevelList = new AccessLevelList();
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

        public CalendarItem CreateCalendarItem()
        {
            CalendarItem eventItem = new CalendarItem();
            eventItem.EventId = CalendarItem.EventId;
            eventItem.ProgenyId = CalendarItem.ProgenyId;
            eventItem.Title = CalendarItem.Title;
            eventItem.Notes = CalendarItem.Notes;
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
    }
}
