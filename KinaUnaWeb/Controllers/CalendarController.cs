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
using Microsoft.AspNetCore.Mvc.Rendering;

namespace KinaUnaWeb.Controllers
{
    public class CalendarController : Controller
    {
        private readonly IProgenyHttpClient _progenyHttpClient;
        private readonly IUserInfosHttpClient _userInfosHttpClient;
        private readonly ICalendarsHttpClient _calendarsHttpClient;
        private readonly IUserAccessHttpClient _userAccessHttpClient;
        private readonly IPushMessageSender _pushMessageSender;
        private readonly IWebNotificationsService _webNotificationsService;
        public CalendarController(IProgenyHttpClient progenyHttpClient, IUserInfosHttpClient userInfosHttpClient, ICalendarsHttpClient calendarsHttpClient,
            IUserAccessHttpClient userAccessHttpClient, IPushMessageSender pushMessageSender, IWebNotificationsService webNotificationsService)
        {
            _progenyHttpClient = progenyHttpClient;
            _userInfosHttpClient = userInfosHttpClient;
            _calendarsHttpClient = calendarsHttpClient;
            _userAccessHttpClient = userAccessHttpClient;
            _pushMessageSender = pushMessageSender;
            _webNotificationsService = webNotificationsService;
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

        [HttpGet]
        public async Task<IActionResult> AddEvent()
        {
            CalendarItemViewModel model = new CalendarItemViewModel();
            model.LanguageId = Request.GetLanguageIdFromCookie();
            string userEmail = User.GetEmail();
            model.CurrentUser = await _userInfosHttpClient.GetUserInfo(userEmail);
            if (model.CurrentUser == null)
            {
                return RedirectToAction("Index");
            }

            model.AllDay = false;
            List<Progeny> accessList = new List<Progeny>();
            if (User.Identity != null && User.Identity.IsAuthenticated && userEmail != null && model.CurrentUser.UserId != null)
            {

                accessList = await _progenyHttpClient.GetProgenyAdminList(userEmail);
                if (accessList.Any())
                {
                    foreach (Progeny prog in accessList)
                    {
                        SelectListItem selItem = new SelectListItem()
                        {
                            Text = accessList.Single(p => p.Id == prog.Id).NickName,
                            Value = prog.Id.ToString()
                        };
                        if (prog.Id == model.CurrentUser.ViewChild)
                        {
                            selItem.Selected = true;
                        }

                        model.ProgenyList.Add(selItem);
                    }
                }
            }
            model.Progeny = accessList[0];

            if (model.LanguageId == 2)
            {
                model.AccessLevelListEn = model.AccessLevelListDe;
            }

            if (model.LanguageId == 3)
            {
                model.AccessLevelListEn = model.AccessLevelListDa;
            }

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddEvent(CalendarItemViewModel model)
        {
            model.LanguageId = Request.GetLanguageIdFromCookie();
            string userEmail = User.GetEmail();
            model.CurrentUser = await _userInfosHttpClient.GetUserInfo(userEmail);

            Progeny prog = await _progenyHttpClient.GetProgeny(model.ProgenyId);
            if (!prog.IsInAdminList(model.CurrentUser.UserEmail))
            {
                // Todo: Show no access info.
                return RedirectToAction("Index");
            }

            CalendarItem eventItem = new CalendarItem();
            eventItem.ProgenyId = model.ProgenyId;
            eventItem.Title = model.Title;
            eventItem.Notes = model.Notes;
            if (model.StartTime != null && model.EndTime != null)
            {
                eventItem.StartTime = TimeZoneInfo.ConvertTimeToUtc(model.StartTime.Value,
                    TimeZoneInfo.FindSystemTimeZoneById(model.CurrentUser.Timezone));
                eventItem.EndTime = TimeZoneInfo.ConvertTimeToUtc(model.EndTime.Value,
                    TimeZoneInfo.FindSystemTimeZoneById(model.CurrentUser.Timezone));
            }
            else
            {
                return View();
            }
            eventItem.Location = model.Location;
            eventItem.Context = model.Context;
            eventItem.AllDay = model.AllDay;
            eventItem.AccessLevel = model.AccessLevel;
            eventItem.Author = model.CurrentUser.UserId;
            eventItem = await _calendarsHttpClient.AddCalendarItem(eventItem);

            string authorName = "";
            if (!string.IsNullOrEmpty(model.CurrentUser.FirstName))
            {
                authorName = model.CurrentUser.FirstName;
            }
            if (!string.IsNullOrEmpty(model.CurrentUser.MiddleName))
            {
                authorName = authorName + " " + model.CurrentUser.MiddleName;
            }
            if (!string.IsNullOrEmpty(model.CurrentUser.LastName))
            {
                authorName = authorName + " " + model.CurrentUser.LastName;
            }

            authorName = authorName.Trim();
            if (string.IsNullOrEmpty(authorName))
            {
                authorName = model.CurrentUser.UserName;
            }

            List<UserAccess> usersToNotif = await _userAccessHttpClient.GetProgenyAccessList(model.ProgenyId);
            Progeny progeny = await _progenyHttpClient.GetProgeny(model.ProgenyId);
            foreach (UserAccess ua in usersToNotif)
            {
                if (ua.AccessLevel <= eventItem.AccessLevel)
                {
                    UserInfo uaUserInfo = await _userInfosHttpClient.GetUserInfo(ua.UserId);
                    if (uaUserInfo.UserId != "Unknown")
                    {
                        if (eventItem.StartTime != null)
                        {
                            DateTime startTime = TimeZoneInfo.ConvertTimeFromUtc(eventItem.StartTime.Value,
                                TimeZoneInfo.FindSystemTimeZoneById(uaUserInfo.Timezone));
                            string eventTimeString = "\r\nStart: " + startTime.ToString("dd-MMM-yyyy HH:mm");

                            if (eventItem.EndTime != null)
                            {
                                DateTime endTime = TimeZoneInfo.ConvertTimeFromUtc(eventItem.EndTime.Value,
                                    TimeZoneInfo.FindSystemTimeZoneById(uaUserInfo.Timezone));
                                eventTimeString = eventTimeString + "\r\nEnd: " + endTime.ToString("dd-MMM-yyyy HH:mm");
                            }

                            WebNotification notification = new WebNotification();
                            notification.To = uaUserInfo.UserId;
                            notification.From = authorName;
                            notification.Message = eventItem.Title + eventTimeString;
                            notification.DateTime = DateTime.UtcNow;
                            notification.Icon = model.CurrentUser.ProfilePicture;
                            notification.Title = "A new calendar event was added for " + progeny.NickName;
                            notification.Link = "/Calendar/ViewEvent?eventId=" + eventItem.EventId + "&childId=" + progeny.Id;
                            notification.Type = "Notification";

                            notification = await _webNotificationsService.SaveNotification(notification);

                            await _pushMessageSender.SendMessage(uaUserInfo.UserId, notification.Title,
                                notification.Message, Constants.WebAppUrl + notification.Link, "kinaunacalendar" + progeny.Id);
                        }
                    }
                }
            }

            return RedirectToAction("Index", "Calendar");
        }

        [HttpGet]
        public async Task<IActionResult> EditEvent(int itemId)
        {

            CalendarItemViewModel model = new CalendarItemViewModel();

            model.LanguageId = Request.GetLanguageIdFromCookie();
            string userEmail = User.GetEmail();
            model.CurrentUser = await _userInfosHttpClient.GetUserInfo(userEmail);

            CalendarItem eventItem = await _calendarsHttpClient.GetCalendarItem(itemId);
            Progeny prog = await _progenyHttpClient.GetProgeny(eventItem.ProgenyId);
            if (!prog.IsInAdminList(model.CurrentUser.UserEmail))
            {
                // Todo: Show no access info.
                return RedirectToAction("Index");
            }
            model.ProgenyId = eventItem.ProgenyId;
            model.Progeny = prog;
            model.EventId = eventItem.EventId;
            model.AccessLevel = eventItem.AccessLevel;
            model.Author = eventItem.Author;
            model.Title = eventItem.Title;
            model.Notes = eventItem.Notes;
            if (eventItem.StartTime.HasValue && eventItem.EndTime.HasValue)
            {
                model.StartTime = TimeZoneInfo.ConvertTimeFromUtc(eventItem.StartTime.Value, TimeZoneInfo.FindSystemTimeZoneById(model.CurrentUser.Timezone));
                model.EndTime = TimeZoneInfo.ConvertTimeFromUtc(eventItem.EndTime.Value, TimeZoneInfo.FindSystemTimeZoneById(model.CurrentUser.Timezone));
            }
            model.Location = eventItem.Location;
            model.Context = eventItem.Context;
            model.AllDay = eventItem.AllDay;
            model.AccessLevelListEn[model.AccessLevel].Selected = true;
            model.AccessLevelListDa[model.AccessLevel].Selected = true;
            model.AccessLevelListDe[model.AccessLevel].Selected = true;

            if (model.LanguageId == 2)
            {
                model.AccessLevelListEn = model.AccessLevelListDe;
            }

            if (model.LanguageId == 3)
            {
                model.AccessLevelListEn = model.AccessLevelListDa;
            }

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditEvent(CalendarItemViewModel model)
        {
            model.LanguageId = Request.GetLanguageIdFromCookie();
            string userEmail = User.GetEmail();
            model.CurrentUser = await _userInfosHttpClient.GetUserInfo(userEmail);

            Progeny prog = await _progenyHttpClient.GetProgeny(model.ProgenyId);
            if (!prog.IsInAdminList(model.CurrentUser.UserEmail))
            {
                // Todo: Show no access info.
                return RedirectToAction("Index");
            }
            if (ModelState.IsValid)
            {
                CalendarItem editedEvent = new CalendarItem();
                editedEvent.ProgenyId = model.ProgenyId;
                editedEvent.EventId = model.EventId;
                editedEvent.AccessLevel = model.AccessLevel;
                editedEvent.Author = model.Author;
                editedEvent.Title = model.Title;
                editedEvent.Notes = model.Notes;
                if (model.StartTime.HasValue && model.EndTime.HasValue)
                {
                    editedEvent.StartTime = TimeZoneInfo.ConvertTimeToUtc(model.StartTime.Value, TimeZoneInfo.FindSystemTimeZoneById(model.CurrentUser.Timezone));
                    editedEvent.EndTime = TimeZoneInfo.ConvertTimeToUtc(model.EndTime.Value, TimeZoneInfo.FindSystemTimeZoneById(model.CurrentUser.Timezone));
                }
                editedEvent.Location = model.Location;
                editedEvent.Context = model.Context;
                editedEvent.AllDay = model.AllDay;
                await _calendarsHttpClient.UpdateCalendarItem(editedEvent);
            }
            return RedirectToAction("Index", "Calendar");
        }

        [HttpGet]
        public async Task<IActionResult> DeleteEvent(int itemId)
        {
            CalendarItemViewModel model = new CalendarItemViewModel();
            model.LanguageId = Request.GetLanguageIdFromCookie();
            string userEmail = User.GetEmail();
            model.CurrentUser = await _userInfosHttpClient.GetUserInfo(userEmail);
            model.CalendarItem = await _calendarsHttpClient.GetCalendarItem(itemId);

            Progeny prog = await _progenyHttpClient.GetProgeny(model.CalendarItem.ProgenyId);
            if (!prog.IsInAdminList(model.CurrentUser.UserEmail))
            {
                // Todo: Show no access info.
                return RedirectToAction("Index");
            }
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteEvent(CalendarItemViewModel model)
        {
            model.LanguageId = Request.GetLanguageIdFromCookie();
            string userEmail = User.GetEmail();
            model.CurrentUser = await _userInfosHttpClient.GetUserInfo(userEmail);

            model.CalendarItem = await _calendarsHttpClient.GetCalendarItem(model.CalendarItem.EventId);
            Progeny prog = await _progenyHttpClient.GetProgeny(model.CalendarItem.ProgenyId);
            if (!prog.IsInAdminList(model.CurrentUser.UserEmail))
            {
                // Todo: Show no access info.
                return RedirectToAction("Index");
            }

            await _calendarsHttpClient.DeleteCalendarItem(model.CalendarItem.EventId);

            return RedirectToAction("Index", "Calendar");
        }
    }
}