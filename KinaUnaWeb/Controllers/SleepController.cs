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
using Microsoft.AspNetCore.Mvc.Rendering;
using KinaUna.Data.Contexts;

namespace KinaUnaWeb.Controllers
{
    public class SleepController : Controller
    {
        private readonly IProgenyHttpClient _progenyHttpClient;
        private readonly IUserInfosHttpClient _userInfosHttpClient;
        private readonly ISleepHttpClient _sleepHttpClient;
        private readonly IUserAccessHttpClient _userAccessHttpClient;
        private readonly WebDbContext _context;
        private readonly IPushMessageSender _pushMessageSender;

        public SleepController(IProgenyHttpClient progenyHttpClient, IUserInfosHttpClient userInfosHttpClient, ISleepHttpClient sleepHttpClient, IUserAccessHttpClient userAccessHttpClient,
            IPushMessageSender pushMessageSender, WebDbContext context)
        {
            _progenyHttpClient = progenyHttpClient;
            _userInfosHttpClient = userInfosHttpClient;
            _sleepHttpClient = sleepHttpClient;
            _userAccessHttpClient = userAccessHttpClient;
            _pushMessageSender = pushMessageSender;
            _context = context;
        }

        [AllowAnonymous]
        public async Task<IActionResult> Index(int childId = 0)
        {
            SleepViewModel model = new SleepViewModel();
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

            if (progeny.IsInAdminList(userEmail))
            {
                model.IsAdmin = true;
                userAccessLevel = (int)AccessLevel.Private;
            }

            
            model.SleepList = new List<Sleep>();
            model.ChartList = new List<Sleep>();
            model.ProgenyId = childId;
            model.SleepTotal = TimeSpan.Zero;
            model.SleepLastYear = TimeSpan.Zero;
            model.SleepLastMonth = TimeSpan.Zero;
            List<Sleep> sList = await _sleepHttpClient.GetSleepList(childId, userAccessLevel);
            DateTime yearAgo = new DateTime(DateTime.UtcNow.Year - 1, DateTime.UtcNow.Month, DateTime.UtcNow.Day, DateTime.UtcNow.Hour, DateTime.UtcNow.Minute, 0);
            DateTime monthAgo = DateTime.UtcNow - TimeSpan.FromDays(30);
            if (sList.Count != 0)
            {
                foreach (Sleep slp in sList)
                {
                    if (slp.AccessLevel >= userAccessLevel)
                    {
                        // Calculate average sleep.
                        bool isLessThanYear = slp.SleepEnd > yearAgo;
                        bool isLessThanMonth = slp.SleepEnd > monthAgo;
                        slp.SleepStart = TimeZoneInfo.ConvertTimeFromUtc(slp.SleepStart,
                            TimeZoneInfo.FindSystemTimeZoneById(model.CurrentUser.Timezone));
                        slp.SleepEnd = TimeZoneInfo.ConvertTimeFromUtc(slp.SleepEnd,
                            TimeZoneInfo.FindSystemTimeZoneById(model.CurrentUser.Timezone));
                        DateTimeOffset startOffset = new DateTimeOffset(slp.SleepStart,
                            TimeZoneInfo.FindSystemTimeZoneById(model.CurrentUser.Timezone).GetUtcOffset(slp.SleepStart));
                        DateTimeOffset endOffset = new DateTimeOffset(slp.SleepEnd,
                            TimeZoneInfo.FindSystemTimeZoneById(model.CurrentUser.Timezone).GetUtcOffset(slp.SleepEnd));
                        slp.SleepDuration = endOffset - startOffset;

                        model.SleepTotal = model.SleepTotal + slp.SleepDuration;
                        if (isLessThanYear)
                        {
                            model.SleepLastYear = model.SleepLastYear + slp.SleepDuration;
                        }

                        if (isLessThanMonth)
                        {
                            model.SleepLastMonth = model.SleepLastMonth + slp.SleepDuration;
                        }

                        // Calculate chart values
                        double durationStartDate = 0.0;
                        if (slp.SleepStart.Date == slp.SleepEnd.Date)
                        {
                            durationStartDate = durationStartDate + slp.SleepDuration.TotalMinutes;
                            Sleep slpItem = model.ChartList.SingleOrDefault(s => s.SleepStart.Date == slp.SleepStart.Date);
                            if (slpItem != null)
                            {
                                slpItem.SleepDuration += TimeSpan.FromMinutes(durationStartDate);
                            }
                            else
                            {
                                Sleep newSleep = new Sleep();
                                newSleep.SleepStart = slp.SleepStart;
                                newSleep.SleepDuration = TimeSpan.FromMinutes(durationStartDate);
                                model.ChartList.Add(newSleep);
                            }
                        }
                        else
                        {
                            DateTimeOffset sOffset = new DateTimeOffset(slp.SleepStart,
                                TimeZoneInfo.FindSystemTimeZoneById(model.CurrentUser.Timezone).GetUtcOffset(slp.SleepStart));
                            DateTimeOffset s2Offset = new DateTimeOffset(slp.SleepStart.Date + TimeSpan.FromDays(1),
                                TimeZoneInfo.FindSystemTimeZoneById(model.CurrentUser.Timezone)
                                    .GetUtcOffset(slp.SleepStart.Date + TimeSpan.FromDays(1)));
                            DateTimeOffset eOffset = new DateTimeOffset(slp.SleepEnd,
                                TimeZoneInfo.FindSystemTimeZoneById(model.CurrentUser.Timezone).GetUtcOffset(slp.SleepEnd));
                            DateTimeOffset e2Offset = new DateTimeOffset(slp.SleepEnd.Date,
                                TimeZoneInfo.FindSystemTimeZoneById(model.CurrentUser.Timezone)
                                    .GetUtcOffset(slp.SleepEnd.Date));
                            TimeSpan sDateDuration = s2Offset - sOffset;
                            TimeSpan eDateDuration = eOffset - e2Offset;
                            durationStartDate = slp.SleepDuration.TotalMinutes - (eDateDuration.TotalMinutes);
                            double durationEndDate = slp.SleepDuration.TotalMinutes - sDateDuration.TotalMinutes;
                            Sleep slpItem = model.ChartList.SingleOrDefault(s => s.SleepStart.Date == slp.SleepStart.Date);
                            if (slpItem != null)
                            {
                                slpItem.SleepDuration += TimeSpan.FromMinutes(durationStartDate);
                            }
                            else
                            {
                                Sleep newSleep = new Sleep();
                                newSleep.SleepStart = slp.SleepStart;
                                newSleep.SleepDuration = TimeSpan.FromMinutes(durationStartDate);
                                model.ChartList.Add(newSleep);
                            }

                            Sleep slpItem2 = model.ChartList.SingleOrDefault(s => s.SleepStart.Date == slp.SleepEnd.Date);
                            if (slpItem2 != null)
                            {
                                slpItem2.SleepDuration += TimeSpan.FromMinutes(durationEndDate);
                            }
                            else
                            {
                                Sleep newSleep = new Sleep();
                                newSleep.SleepStart = slp.SleepEnd;
                                newSleep.SleepDuration = TimeSpan.FromMinutes(durationEndDate);
                                model.ChartList.Add(newSleep);
                            }
                        }

                        model.SleepList.Add(slp);
                    }
                }
                model.SleepList = model.SleepList.OrderBy(s => s.SleepStart).ToList();
                model.ChartList = model.ChartList.OrderBy(s => s.SleepStart).ToList();
                
                model.TotalAverage = model.SleepTotal / (DateTime.UtcNow - model.SleepList.First().SleepStart).TotalDays;
                model.LastYearAverage = model.SleepLastYear / (DateTime.UtcNow - yearAgo).TotalDays;
                model.LastMonthAverage = model.SleepLastMonth / 30;

            }
            
            model.Progeny = progeny;
            
            return View(model);
        }

        [AllowAnonymous]
        public async Task<IActionResult> SleepCalendar(int childId = 0)
        {
            SleepViewModel model = new SleepViewModel();
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

            if (progeny.IsInAdminList(userEmail))
            {
                model.IsAdmin = true;
                userAccessLevel = (int)AccessLevel.Private;
            }
            
            model.ProgenyId = childId;

            List<Sleep> allSleepList = await _sleepHttpClient.GetSleepList(childId, userAccessLevel);
            List<Sleep> sleepList = new List<Sleep>();

            if (allSleepList.Count != 0)
            {
                foreach (Sleep s in allSleepList)
                {
                    s.SleepStart = TimeZoneInfo.ConvertTimeFromUtc(s.SleepStart,
                        TimeZoneInfo.FindSystemTimeZoneById(model.CurrentUser.Timezone));
                    s.SleepEnd = TimeZoneInfo.ConvertTimeFromUtc(s.SleepEnd,
                        TimeZoneInfo.FindSystemTimeZoneById(model.CurrentUser.Timezone));
                    s.SleepDuration = s.SleepEnd - s.SleepStart;
                    s.StartString = s.SleepStart.ToString("yyyy-MM-dd") + "T" + s.SleepStart.ToString("HH:mm:ss");
                    s.EndString = s.SleepEnd.ToString("yyyy-MM-dd") + "T" + s.SleepEnd.ToString("HH:mm:ss");
                    if (s.AccessLevel >= userAccessLevel)
                    {
                        sleepList.Add(s);
                    }
                }
                sleepList = sleepList.OrderBy(s => s.SleepStart).ToList();
                model.SleepList = sleepList;

            }
            
            model.Progeny = progeny;
            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> AddSleep()
        {
            SleepViewModel model = new SleepViewModel();
            model.LanguageId = Request.GetLanguageIdFromCookie();
            string userEmail = User.GetEmail();
            model.CurrentUser = await _userInfosHttpClient.GetUserInfo(userEmail);

            if (model.CurrentUser == null)
            {
                return RedirectToAction("Index");
            }

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
        public async Task<IActionResult> AddSleep(SleepViewModel model)
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

            Sleep sleepItem = new Sleep();
            sleepItem.ProgenyId = model.ProgenyId;
            sleepItem.Progeny = prog;
            sleepItem.CreatedDate = DateTime.UtcNow;
            if (model.SleepStart.HasValue && model.SleepEnd.HasValue)
            {
                sleepItem.SleepStart = TimeZoneInfo.ConvertTimeToUtc(model.SleepStart.Value, TimeZoneInfo.FindSystemTimeZoneById(model.CurrentUser.Timezone));
                sleepItem.SleepEnd = TimeZoneInfo.ConvertTimeToUtc(model.SleepEnd.Value, TimeZoneInfo.FindSystemTimeZoneById(model.CurrentUser.Timezone));
            }
            sleepItem.SleepRating = model.SleepRating;
            if (sleepItem.SleepRating == 0)
            {
                sleepItem.SleepRating = 3;
            }
            sleepItem.SleepNotes = model.SleepNotes;
            sleepItem.AccessLevel = model.AccessLevel;
            sleepItem.Author = model.CurrentUser.UserId;

            sleepItem = await _sleepHttpClient.AddSleep(sleepItem);

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
            List<UserAccess> usersToNotif = await _userAccessHttpClient.GetProgenyAccessList(sleepItem.ProgenyId);
            foreach (UserAccess ua in usersToNotif)
            {
                if (ua.AccessLevel <= sleepItem.AccessLevel)
                {
                    UserInfo uaUserInfo = await _userInfosHttpClient.GetUserInfo(ua.UserId);
                    if (uaUserInfo.UserId != "Unknown")
                    {
                        DateTime sleepStart = TimeZoneInfo.ConvertTimeFromUtc(sleepItem.SleepStart,
                            TimeZoneInfo.FindSystemTimeZoneById(uaUserInfo.Timezone));
                        DateTime sleepEnd = TimeZoneInfo.ConvertTimeFromUtc(sleepItem.SleepEnd,
                            TimeZoneInfo.FindSystemTimeZoneById(uaUserInfo.Timezone));

                        WebNotification notification = new WebNotification();
                        notification.To = uaUserInfo.UserId;
                        notification.From = authorName;
                        notification.Message = "Start: " + sleepStart.ToString("dd-MMM-yyyy HH:mm") + "\r\nEnd: " + sleepEnd.ToString("dd-MMM-yyyy HH:mm");
                        notification.DateTime = DateTime.UtcNow;
                        notification.Icon = model.CurrentUser.ProfilePicture;
                        notification.Title = "Sleep Added for " + prog.NickName;
                        notification.Link = "/Sleep?childId=" + prog.Id;
                        notification.Type = "Notification";
                        await _context.WebNotificationsDb.AddAsync(notification);
                        await _context.SaveChangesAsync();

                        await _pushMessageSender.SendMessage(uaUserInfo.UserId, notification.Title, notification.Message,
                            Constants.WebAppUrl + notification.Link, "kinaunasleep" + prog.Id);
                    }
                }
            }

            // Todo: send notification to others.
            return RedirectToAction("Index", "Sleep");
        }

        [HttpGet]
        public async Task<IActionResult> EditSleep(int itemId)
        {
            SleepViewModel model = new SleepViewModel();
            model.LanguageId = Request.GetLanguageIdFromCookie();
            string userEmail = User.GetEmail();
            model.CurrentUser = await _userInfosHttpClient.GetUserInfo(userEmail);

            Sleep sleep = await _sleepHttpClient.GetSleepItem(itemId);
            Progeny prog = await _progenyHttpClient.GetProgeny(sleep.ProgenyId);
            if (!prog.IsInAdminList(model.CurrentUser.UserEmail))
            {
                // Todo: Show no access info.
                return RedirectToAction("Index");
            }
            model.ProgenyId = sleep.ProgenyId;
            model.Progeny = prog;
            model.SleepId = sleep.SleepId;
            model.AccessLevel = sleep.AccessLevel;
            model.Author = sleep.Author;
            model.CreatedDate = sleep.CreatedDate;
            model.SleepStart = TimeZoneInfo.ConvertTimeFromUtc(sleep.SleepStart, TimeZoneInfo.FindSystemTimeZoneById(model.CurrentUser.Timezone));
            model.SleepEnd = TimeZoneInfo.ConvertTimeFromUtc(sleep.SleepEnd, TimeZoneInfo.FindSystemTimeZoneById(model.CurrentUser.Timezone));
            model.SleepRating = sleep.SleepRating;
            if (model.SleepRating == 0)
            {
                model.SleepRating = 3;
            }
            model.SleepNotes = sleep.SleepNotes;
            model.Progeny = prog;
            model.AccessLevelListEn[model.AccessLevel].Selected = true;
            model.AccessLevelListDa[model.AccessLevel].Selected = true;
            model.AccessLevelListDe[model.AccessLevel].Selected = true;
            ViewBag.RatingList = new List<SelectListItem>();
            SelectListItem selItem1 = new SelectListItem();
            selItem1.Text = "1";
            selItem1.Value = "1";
            SelectListItem selItem2 = new SelectListItem();
            selItem2.Text = "2";
            selItem2.Value = "2";
            SelectListItem selItem3 = new SelectListItem();
            selItem3.Text = "3";
            selItem3.Value = "3";
            SelectListItem selItem4 = new SelectListItem();
            selItem4.Text = "4";
            selItem4.Value = "4";
            SelectListItem selItem5 = new SelectListItem();
            selItem5.Text = "5";
            selItem5.Value = "5";
            ViewBag.RatingList.Add(selItem1);
            ViewBag.RatingList.Add(selItem2);
            ViewBag.RatingList.Add(selItem3);
            ViewBag.RatingList.Add(selItem4);
            ViewBag.RatingList.Add(selItem5);
            ViewBag.RatingList[model.SleepRating - 1].Selected = true;

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
        public async Task<IActionResult> EditSleep(SleepViewModel model)
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
                Sleep editedSleep = new Sleep();
                editedSleep.ProgenyId = model.ProgenyId;
                editedSleep.Progeny = prog;
                editedSleep.SleepId = model.SleepId;
                editedSleep.AccessLevel = model.AccessLevel;
                editedSleep.Author = model.Author;
                editedSleep.CreatedDate = model.CreatedDate;
                if (model.SleepStart.HasValue && model.SleepEnd.HasValue)
                {
                    editedSleep.SleepStart = TimeZoneInfo.ConvertTimeToUtc(model.SleepStart.Value, TimeZoneInfo.FindSystemTimeZoneById(model.CurrentUser.Timezone));
                    editedSleep.SleepEnd = TimeZoneInfo.ConvertTimeToUtc(model.SleepEnd.Value, TimeZoneInfo.FindSystemTimeZoneById(model.CurrentUser.Timezone));
                }
                editedSleep.SleepRating = model.SleepRating;
                if (editedSleep.SleepRating == 0)
                {
                    editedSleep.SleepRating = 3;
                }
                editedSleep.SleepNotes = model.SleepNotes;
                await _sleepHttpClient.UpdateSleep(editedSleep);
            }
            return RedirectToAction("Index", "Sleep");
        }

        [HttpGet]
        public async Task<IActionResult> DeleteSleep(int itemId)
        {
            SleepViewModel model = new SleepViewModel();
            model.LanguageId = Request.GetLanguageIdFromCookie();
            string userEmail = User.GetEmail();
            model.CurrentUser = await _userInfosHttpClient.GetUserInfo(userEmail);
            model.Sleep = await _sleepHttpClient.GetSleepItem(itemId);

            Progeny prog = await _progenyHttpClient.GetProgeny(model.Sleep.ProgenyId);
            if (!prog.IsInAdminList(model.CurrentUser.UserEmail))
            {
                // Todo: Show no access info.
                return RedirectToAction("Index");
            }
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteSleep(SleepViewModel model)
        {
            model.LanguageId = Request.GetLanguageIdFromCookie();
            string userEmail = User.GetEmail();
            model.CurrentUser = await _userInfosHttpClient.GetUserInfo(userEmail);

            Sleep sleep = await _sleepHttpClient.GetSleepItem(model.Sleep.SleepId);

            Progeny prog = await _progenyHttpClient.GetProgeny(sleep.ProgenyId);
            if (!prog.IsInAdminList(model.CurrentUser.UserEmail))
            {
                // Todo: Show no access info.
                return RedirectToAction("Index");
            }

            await _sleepHttpClient.DeleteSleepItem(sleep.SleepId);

            return RedirectToAction("Index", "Sleep");
        }
    }
}