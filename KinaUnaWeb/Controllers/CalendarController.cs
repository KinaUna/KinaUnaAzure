using KinaUnaWeb.Models.ItemViewModels;
using KinaUnaWeb.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using KinaUna.Data;
using KinaUna.Data.Extensions;
using KinaUna.Data.Models;
using Syncfusion.EJ2.Schedule;

namespace KinaUnaWeb.Controllers
{
    public class CalendarController : Controller
    {
        private readonly IProgenyHttpClient _progenyHttpClient;
        private readonly string _defaultUser = Constants.DefaultUserEmail;

        public CalendarController(IProgenyHttpClient progenyHttpClient)
        {
            _progenyHttpClient = progenyHttpClient;
        }

        [AllowAnonymous]
        public async Task<IActionResult> Index(int? id, int childId = 0)
        {
            int progId = childId;
            string userEmail = HttpContext.User.FindFirst("email")?.Value ?? _defaultUser;
            
            UserInfo userinfo = await _progenyHttpClient.GetUserInfo(userEmail);
            if (childId == 0 && userinfo.ViewChild > 0)
            {
                progId = userinfo.ViewChild;
            }

            if (progId == 0)
            {
                progId = Constants.DefaultChildId;
            }

            Progeny progeny = await _progenyHttpClient.GetProgeny(progId);
            List<UserAccess> accessList = await _progenyHttpClient.GetProgenyAccessList(progId);

            int userAccessLevel = (int)AccessLevel.Public;

            if (accessList.Count != 0)
            {
                UserAccess userAccess = accessList.SingleOrDefault(u => u.UserId.ToUpper() == userEmail.ToUpper());
                if (userAccess != null)
                {
                    userAccessLevel = userAccess.AccessLevel;
                }
            }

            bool userIsProgenyAdmin = false;
            if (progeny.IsInAdminList(userEmail))
            {
                userIsProgenyAdmin = true;
                userAccessLevel = (int)AccessLevel.Private;
            }

            ApplicationUser currentUser = new ApplicationUser();
            currentUser.TimeZone = userinfo.Timezone;
            var eventsList = await _progenyHttpClient.GetCalendarList(progId, userAccessLevel); // _context.CalendarDb.AsNoTracking().Where(e => e.ProgenyId == _progId).ToList();
            eventsList = eventsList.OrderBy(e => e.StartTime).ToList();
            CalendarItemViewModel events = new CalendarItemViewModel();
            events.ViewOptions.Add(new ScheduleView{Option = Syncfusion.EJ2.Schedule.View.Day, DateFormat = "dd/MMM/yyyy", FirstDayOfWeek = 1});
            events.ViewOptions.Add(new ScheduleView { Option = Syncfusion.EJ2.Schedule.View.Week, FirstDayOfWeek=1, ShowWeekNumber = true, DateFormat = "dd/MMM/yyyy" });
            events.ViewOptions.Add(new ScheduleView { Option = Syncfusion.EJ2.Schedule.View.Month, FirstDayOfWeek = 1, ShowWeekNumber = true, DateFormat = "dd/MMM/yyyy" });
            events.ViewOptions.Add(new ScheduleView { Option = Syncfusion.EJ2.Schedule.View.Agenda, FirstDayOfWeek = 1, DateFormat = "dd/MMM/yyyy" });
            events.IsAdmin = userIsProgenyAdmin;
            events.UserData = currentUser;
            events.Progeny = progeny;
            events.EventsList = new List<CalendarItem>();
            
            foreach (CalendarItem ev in eventsList)
            {
                if (ev.AccessLevel == (int)AccessLevel.Public || ev.AccessLevel >= userAccessLevel)
                {
                    if (ev.StartTime.HasValue && ev.EndTime.HasValue)
                    {
                        ev.StartTime = TimeZoneInfo.ConvertTimeFromUtc(ev.StartTime.Value,
                            TimeZoneInfo.FindSystemTimeZoneById(userinfo.Timezone));
                        ev.EndTime = TimeZoneInfo.ConvertTimeFromUtc(ev.EndTime.Value,
                                TimeZoneInfo.FindSystemTimeZoneById(userinfo.Timezone));
                        
                        // ToDo: Replace format string with configuration or userdefined value
                        ev.StartString = ev.StartTime.Value.ToString("yyyy-MM-dd") + "T" + ev.StartTime.Value.ToString("HH:mm:ss");
                        ev.EndString = ev.EndTime.Value.ToString("yyyy-MM-dd") + "T" + ev.EndTime.Value.ToString("HH:mm:ss");
                        ev.Start = ev.StartTime.Value;
                        ev.End = ev.EndTime.Value;
                        ev.IsReadonly = !events.IsAdmin;
                        // Todo: Add color property
                        events.EventsList.Add(ev);
                    }
                }
            }


            return View(events);
        }

        [AllowAnonymous]
        public async Task<IActionResult> ViewEvent(int eventId)
        {
            CalendarItem eventItem = await _progenyHttpClient.GetCalendarItem(eventId); // _context.CalendarDb.AsNoTracking().SingleAsync(e => e.EventId == eventId);

            CalendarItemViewModel model = new CalendarItemViewModel();

            string userEmail = HttpContext.User.FindFirst("email")?.Value ?? _defaultUser;
            
            UserInfo userinfo = await _progenyHttpClient.GetUserInfo(userEmail);

            Progeny progeny = await _progenyHttpClient.GetProgeny(eventItem.ProgenyId);
            List<UserAccess> accessList = await _progenyHttpClient.GetProgenyAccessList(eventItem.ProgenyId);

            int userAccessLevel = (int)AccessLevel.Public;

            if (accessList.Count != 0)
            {
                UserAccess userAccess = accessList.SingleOrDefault(u => u.UserId.ToUpper() == userEmail.ToUpper());
                if (userAccess != null)
                {
                    userAccessLevel = userAccess.AccessLevel;
                }
            }

            bool userIsProgenyAdmin = false;
            if (progeny.IsInAdminList(userEmail))
            {
                userIsProgenyAdmin = true;
                userAccessLevel = (int)AccessLevel.Private;
            }

            if (eventItem.AccessLevel < userAccessLevel)
            {
                // Todo: Show access denied instead of redirecting.
                RedirectToAction("Index");
            }
            
            model.EventId = eventItem.EventId;
            model.ProgenyId = eventItem.ProgenyId;
            model.Progeny = progeny;

            
            model.Title = eventItem.Title;
            model.AllDay = eventItem.AllDay;
            if (eventItem.StartTime.HasValue && eventItem.EndTime.HasValue)
            {
                model.StartTime = TimeZoneInfo.ConvertTimeFromUtc(eventItem.StartTime.Value, TimeZoneInfo.FindSystemTimeZoneById(userinfo.Timezone));
                model.EndTime = TimeZoneInfo.ConvertTimeFromUtc(eventItem.EndTime.Value, TimeZoneInfo.FindSystemTimeZoneById(userinfo.Timezone));
            }
            model.Notes = eventItem.Notes;
            model.Location = eventItem.Location;
            model.Context = eventItem.Context;
            model.AccessLevel = eventItem.AccessLevel;
            model.IsAdmin = userIsProgenyAdmin;
            
            return View(model);
        }                
    }
}