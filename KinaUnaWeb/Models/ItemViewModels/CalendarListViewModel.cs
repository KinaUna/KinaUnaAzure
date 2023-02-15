using System;
using KinaUna.Data.Models;
using Syncfusion.EJ2.Schedule;
using System.Collections.Generic;
using System.Linq;

namespace KinaUnaWeb.Models.ItemViewModels
{
    public class CalendarListViewModel: BaseItemsViewModel
    {
        public List<CalendarItem> EventsList { get; set; }
        
        public List<ScheduleView> ViewOptions { get; set; }

        public CalendarListViewModel(BaseItemsViewModel baseItemsViewModel)
        {
            SetBaseProperties(baseItemsViewModel);

            EventsList = new List<CalendarItem>();

            ViewOptions = new List<ScheduleView>
            {
                new ScheduleView { Option = View.Day, DateFormat = "dd/MMM/yyyy", FirstDayOfWeek = 1 },
                new ScheduleView { Option = View.Week, FirstDayOfWeek = 1, ShowWeekNumber = true, DateFormat = "dd/MMM/yyyy" },
                new ScheduleView { Option = View.Month, FirstDayOfWeek = 1, ShowWeekNumber = true, DateFormat = "dd/MMM/yyyy" },
                new ScheduleView { Option = View.Agenda, FirstDayOfWeek = 1, DateFormat = "dd/MMM/yyyy" }
            };
        }

        public void SetEventsList(List<CalendarItem> eventsList)
        {
            eventsList = eventsList.OrderBy(e => e.StartTime).ToList();
            EventsList = new List<CalendarItem>();

            foreach (CalendarItem ev in eventsList)
            {
                if (ev.AccessLevel == (int)AccessLevel.Public || ev.AccessLevel >= CurrentAccessLevel)
                {
                    if (ev.StartTime.HasValue && ev.EndTime.HasValue)
                    {
                        ev.StartTime = TimeZoneInfo.ConvertTimeFromUtc(ev.StartTime.Value,
                            TimeZoneInfo.FindSystemTimeZoneById(CurrentUser.Timezone));
                        ev.EndTime = TimeZoneInfo.ConvertTimeFromUtc(ev.EndTime.Value,
                            TimeZoneInfo.FindSystemTimeZoneById(CurrentUser.Timezone));

                        // ToDo: Replace format string with configuration or user defined value
                        ev.StartString = ev.StartTime.Value.ToString("yyyy-MM-dd") + "T" + ev.StartTime.Value.ToString("HH:mm:ss");
                        ev.EndString = ev.EndTime.Value.ToString("yyyy-MM-dd") + "T" + ev.EndTime.Value.ToString("HH:mm:ss");
                        ev.Start = ev.StartTime.Value;
                        ev.End = ev.EndTime.Value;
                        ev.IsReadonly = !IsCurrentUserProgenyAdmin;
                        // Todo: Add color property
                        EventsList.Add(ev);
                    }
                }
            }
        }
    }
}
