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

namespace KinaUnaWeb.Controllers
{
    public class SleepController : Controller
    {
        private readonly IProgenyHttpClient _progenyHttpClient;
        private readonly IUserInfosHttpClient _userInfosHttpClient;
        private readonly ISleepHttpClient _sleepHttpClient;
        private readonly IUserAccessHttpClient _userAccessHttpClient;

        public SleepController(IProgenyHttpClient progenyHttpClient, IUserInfosHttpClient userInfosHttpClient, ISleepHttpClient sleepHttpClient, IUserAccessHttpClient userAccessHttpClient)
        {
            _progenyHttpClient = progenyHttpClient;
            _userInfosHttpClient = userInfosHttpClient;
            _sleepHttpClient = sleepHttpClient;
            _userAccessHttpClient = userAccessHttpClient;
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
    }
}