using KinaUnaWeb.Data;
using KinaUnaWeb.Models;
using KinaUnaWeb.Models.ItemViewModels;
using KinaUnaWeb.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace KinaUnaWeb.Controllers
{
    public class CalendarController : Controller
    {
        private readonly WebDbContext _context;
        private readonly IProgenyHttpClient _progenyHttpClient;
        private int _progId = Constants.DefaultChildId;
        private bool _userIsProgenyAdmin;
        private readonly string _defaultUser = Constants.DefaultUserEmail;

        public CalendarController(WebDbContext context, IProgenyHttpClient progenyHttpClient)
        {
            _context = context;
            _progenyHttpClient = progenyHttpClient;
        }

        [AllowAnonymous]
        public async Task<IActionResult> Index(int? id, int childId = 0)
        {
            _progId = childId;
            string userEmail = HttpContext.User.FindFirst("email")?.Value ?? _defaultUser;
            
            UserInfo userinfo = await _progenyHttpClient.GetUserInfo(userEmail);
            if (childId == 0 && userinfo.ViewChild > 0)
            {
                _progId = userinfo.ViewChild;
            }

            if (_progId == 0)
            {
                _progId = Constants.DefaultChildId;
            }

            Progeny progeny = await _progenyHttpClient.GetProgeny(_progId);
            List<UserAccess> accessList = await _progenyHttpClient.GetProgenyAccessList(_progId);

            int userAccessLevel = (int)AccessLevel.Public;

            if (accessList.Count != 0)
            {
                UserAccess userAccess = accessList.SingleOrDefault(u => u.UserId.ToUpper() == userEmail.ToUpper());
                if (userAccess != null)
                {
                    userAccessLevel = userAccess.AccessLevel;
                }
            }

            if (progeny.Admins.ToUpper().Contains(userEmail.ToUpper()))
            {
                _userIsProgenyAdmin = true;
                userAccessLevel = (int)AccessLevel.Private;
            }

            ApplicationUser currentUser = new ApplicationUser();
            currentUser.TimeZone = userinfo.Timezone;
            var eventsList = _context.CalendarDb.AsNoTracking().Where(e => e.ProgenyId == _progId).ToList();
            eventsList = eventsList.OrderBy(e => e.StartTime).ToList();
            CalendarItemViewModel events = new CalendarItemViewModel();
            events.IsAdmin = _userIsProgenyAdmin;
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
                        if (ev.AllDay)
                        {
                            ev.EndTime = TimeZoneInfo.ConvertTimeFromUtc(ev.EndTime.Value + TimeSpan.FromDays(1),
                                TimeZoneInfo.FindSystemTimeZoneById(userinfo.Timezone));
                        }
                        else
                        {
                            ev.EndTime = TimeZoneInfo.ConvertTimeFromUtc(ev.EndTime.Value,
                                TimeZoneInfo.FindSystemTimeZoneById(userinfo.Timezone));
                        }
                        // ToDo: Replace format string with configuration or userdefined value
                        ev.StartString = ev.StartTime.Value.ToString("yyyy-MM-dd") + "T" + ev.StartTime.Value.ToString("HH:mm:ss");
                        ev.EndString = ev.EndTime.Value.ToString("yyyy-MM-dd") + "T" + ev.EndTime.Value.ToString("HH:mm:ss");
                        events.EventsList.Add(ev);
                    }
                }
            }


            return View(events);
        }

        [AllowAnonymous]
        public async Task<IActionResult> ViewEvent(int eventId)
        {
            CalendarItem eventItem = await _context.CalendarDb.AsNoTracking().SingleAsync(e => e.EventId == eventId);

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

            if (progeny.Admins.ToUpper().Contains(userEmail.ToUpper()))
            {
                _userIsProgenyAdmin = true;
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
            model.IsAdmin = _userIsProgenyAdmin;
            
            return View(model);
        }
    }
}