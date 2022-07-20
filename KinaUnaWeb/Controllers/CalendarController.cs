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
        private readonly IUserInfosHttpClient _userInfosHttpClient;
        private readonly ICalendarsHttpClient _calendarsHttpClient;
        private readonly IUserAccessHttpClient _userAccessHttpClient;
        
        public CalendarController(IProgenyHttpClient progenyHttpClient, IUserInfosHttpClient userInfosHttpClient, ICalendarsHttpClient calendarsHttpClient, IUserAccessHttpClient userAccessHttpClient)
        {
            _progenyHttpClient = progenyHttpClient;
            _userInfosHttpClient = userInfosHttpClient;
            _calendarsHttpClient = calendarsHttpClient;
            _userAccessHttpClient = userAccessHttpClient;
        }

        [AllowAnonymous]
        public async Task<IActionResult> Index(int? id, int childId = 0)
        {
            CalendarItemViewModel model = new CalendarItemViewModel();
            model.LanguageId = Request.GetLanguageIdFromCookie();
            string userEmail = User.GetEmail();
            model.CurrentUser = await _userInfosHttpClient.GetUserInfo(userEmail);

            
            if (childId == 0 && model.CurrentUser.ViewChild > 0)
            {
                childId = model.CurrentUser.ViewChild;
            }

            if (childId == 0)
            {
                childId = Constants.DefaultChildId;
            }

            Progeny progeny = await _progenyHttpClient.GetProgeny(childId);
            List<UserAccess> accessList = await _userAccessHttpClient.GetProgenyAccessList(childId);

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
            currentUser.TimeZone = model.CurrentUser.Timezone;
            List<CalendarItem> eventsList = await _calendarsHttpClient.GetCalendarList(childId, userAccessLevel); // _context.CalendarDb.AsNoTracking().Where(e => e.ProgenyId == _progId).ToList();
            eventsList = eventsList.OrderBy(e => e.StartTime).ToList();
            
            model.ViewOptions.Add(new ScheduleView{Option = Syncfusion.EJ2.Schedule.View.Day, DateFormat = "dd/MMM/yyyy", FirstDayOfWeek = 1});
            model.ViewOptions.Add(new ScheduleView { Option = Syncfusion.EJ2.Schedule.View.Week, FirstDayOfWeek=1, ShowWeekNumber = true, DateFormat = "dd/MMM/yyyy" });
            model.ViewOptions.Add(new ScheduleView { Option = Syncfusion.EJ2.Schedule.View.Month, FirstDayOfWeek = 1, ShowWeekNumber = true, DateFormat = "dd/MMM/yyyy" });
            model.ViewOptions.Add(new ScheduleView { Option = Syncfusion.EJ2.Schedule.View.Agenda, FirstDayOfWeek = 1, DateFormat = "dd/MMM/yyyy" });
            model.IsAdmin = userIsProgenyAdmin;
            model.UserData = currentUser;
            model.Progeny = progeny;
            model.EventsList = new List<CalendarItem>();
            
            foreach (CalendarItem ev in eventsList)
            {
                if (ev.AccessLevel == (int)AccessLevel.Public || ev.AccessLevel >= userAccessLevel)
                {
                    if (ev.StartTime.HasValue && ev.EndTime.HasValue)
                    {
                        ev.StartTime = TimeZoneInfo.ConvertTimeFromUtc(ev.StartTime.Value,
                            TimeZoneInfo.FindSystemTimeZoneById(model.CurrentUser.Timezone));
                        ev.EndTime = TimeZoneInfo.ConvertTimeFromUtc(ev.EndTime.Value,
                                TimeZoneInfo.FindSystemTimeZoneById(model.CurrentUser.Timezone));
                        
                        // ToDo: Replace format string with configuration or userdefined value
                        ev.StartString = ev.StartTime.Value.ToString("yyyy-MM-dd") + "T" + ev.StartTime.Value.ToString("HH:mm:ss");
                        ev.EndString = ev.EndTime.Value.ToString("yyyy-MM-dd") + "T" + ev.EndTime.Value.ToString("HH:mm:ss");
                        ev.Start = ev.StartTime.Value;
                        ev.End = ev.EndTime.Value;
                        ev.IsReadonly = !model.IsAdmin;
                        // Todo: Add color property
                        model.EventsList.Add(ev);
                    }
                }
            }


            return View(model);
        }

        [AllowAnonymous]
        public async Task<IActionResult> ViewEvent(int eventId)
        {
            CalendarItem eventItem = await _calendarsHttpClient.GetCalendarItem(eventId); // _context.CalendarDb.AsNoTracking().SingleAsync(e => e.EventId == eventId);

            CalendarItemViewModel model = new CalendarItemViewModel();
            model.LanguageId = Request.GetLanguageIdFromCookie();
            string userEmail = User.GetEmail();
            model.CurrentUser = await _userInfosHttpClient.GetUserInfo(userEmail);
            
            Progeny progeny = await _progenyHttpClient.GetProgeny(eventItem.ProgenyId);
            List<UserAccess> accessList = await _userAccessHttpClient.GetProgenyAccessList(eventItem.ProgenyId);

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
                model.StartTime = TimeZoneInfo.ConvertTimeFromUtc(eventItem.StartTime.Value, TimeZoneInfo.FindSystemTimeZoneById(model.CurrentUser.Timezone));
                model.EndTime = TimeZoneInfo.ConvertTimeFromUtc(eventItem.EndTime.Value, TimeZoneInfo.FindSystemTimeZoneById(model.CurrentUser.Timezone));
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