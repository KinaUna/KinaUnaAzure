using System;
using System.Collections.Generic;
using KinaUna.Data.Models;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace KinaUnaWeb.Models.ItemViewModels
{
    public class CalendarItemViewModel: BaseItemsViewModel
    {
        public int EventId { get; set; }
        public string Title { get; set; }
        public string Notes { get; set; }
        public DateTime? StartTime { get; set; }
        public DateTime? EndTime { get; set; }
        public string Location { get; set; }
        public string Context { get; set; }
        public bool AllDay { get; set; }
        public int AccessLevel { get; set; }
        public string Author { get; set; }
        public List<SelectListItem> ProgenyList { get; set; }
        public List<SelectListItem> AccessLevelListEn { get; set; }
        public List<SelectListItem> AccessLevelListDa { get; set; }
        public List<SelectListItem> AccessLevelListDe { get; set; }
        public string StartString { get; set; }
        public string EndString { get; set; }
        public CalendarItem CalendarItem { get; set; }

        public CalendarItemViewModel()
        {
            ProgenyList = new List<SelectListItem>();
            AccessLevelList accList = new AccessLevelList();
            AccessLevelListEn = accList.AccessLevelListEn;
            AccessLevelListDa = accList.AccessLevelListDa;
            AccessLevelListDe = accList.AccessLevelListDe;
        }

        public CalendarItemViewModel(BaseItemsViewModel baseItemsViewModel)
        {
            SetBaseProperties(baseItemsViewModel);

            ProgenyList = new List<SelectListItem>();
            AccessLevelList accList = new AccessLevelList();
            AccessLevelListEn = accList.AccessLevelListEn;
            AccessLevelListDa = accList.AccessLevelListDa;
            AccessLevelListDe = accList.AccessLevelListDe;
        }

        public void SetBaseProperties(BaseItemsViewModel baseItemsViewModel)
        {
            LanguageId = baseItemsViewModel.LanguageId;
            CurrentUser = baseItemsViewModel.CurrentUser;
            CurrentProgenyId = baseItemsViewModel.CurrentProgenyId;
            CurrentAccessLevel = baseItemsViewModel.CurrentAccessLevel;
            CurrentProgeny = baseItemsViewModel.CurrentProgeny;
            CurrentProgenyAccessList = baseItemsViewModel.CurrentProgenyAccessList;
            IsCurrentUserProgenyAdmin = baseItemsViewModel.IsCurrentUserProgenyAdmin;
        }

        public void SetCalendarItem(CalendarItem eventItem)
        {
            EventId = eventItem.EventId;
            Title = eventItem.Title;
            AllDay = eventItem.AllDay;
            if (eventItem.StartTime.HasValue && eventItem.EndTime.HasValue)
            {
                StartTime = TimeZoneInfo.ConvertTimeFromUtc(eventItem.StartTime.Value, TimeZoneInfo.FindSystemTimeZoneById(CurrentUser.Timezone));
                EndTime = TimeZoneInfo.ConvertTimeFromUtc(eventItem.EndTime.Value, TimeZoneInfo.FindSystemTimeZoneById(CurrentUser.Timezone));
            }
            Notes = eventItem.Notes;
            Location = eventItem.Location;
            Context = eventItem.Context;
            AccessLevel = eventItem.AccessLevel;
            
            SetAccessLevelList();
        }

        public void SetAccessLevelList()
        {
            AccessLevelListEn[AccessLevel].Selected = true;
            AccessLevelListDa[AccessLevel].Selected = true;
            AccessLevelListDe[AccessLevel].Selected = true;

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
            eventItem.EventId = EventId;
            eventItem.ProgenyId = CurrentProgenyId;
            eventItem.Title = Title;
            eventItem.Notes = Notes;
            if (StartTime != null && EndTime != null)
            {
                eventItem.StartTime = TimeZoneInfo.ConvertTimeToUtc(StartTime.Value,
                    TimeZoneInfo.FindSystemTimeZoneById(CurrentUser.Timezone));
                eventItem.EndTime = TimeZoneInfo.ConvertTimeToUtc(EndTime.Value,
                    TimeZoneInfo.FindSystemTimeZoneById(CurrentUser.Timezone));
            }
            
            eventItem.Location = Location;
            eventItem.Context = Context;
            eventItem.AllDay = AllDay;
            eventItem.AccessLevel = AccessLevel;
            eventItem.Author = CurrentUser.UserId;

            return eventItem;
        }
    }
}
