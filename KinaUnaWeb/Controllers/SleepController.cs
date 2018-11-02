﻿using KinaUnaWeb.Data;
using KinaUnaWeb.Models;
using KinaUnaWeb.Models.ItemViewModels;
using KinaUnaWeb.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace KinaUnaWeb.Controllers
{
    public class SleepController : Controller
    {
        private WebDbContext _context;
        private readonly IProgenyHttpClient _progenyHttpClient;
        private int _progId = 2;
        private bool _userIsProgenyAdmin = false;
        private readonly string _defaultUser = "testuser@niviaq.com";

        public SleepController(WebDbContext context, IProgenyHttpClient progenyHttpClient)
        {
            _context = context;
            _progenyHttpClient = progenyHttpClient;
        }

        [AllowAnonymous]
        public async Task<IActionResult> Index(int childId = 0)
        {
            _progId = childId;
            string userEmail = HttpContext.User.FindFirst("email")?.Value ?? _defaultUser;
            string userTimeZone = HttpContext.User.FindFirst("timezone")?.Value ?? "Romance Standard Time";
            if (string.IsNullOrEmpty(userTimeZone))
            {
                userTimeZone = "Romance Standard Time";
            }
            UserInfo userinfo = await _progenyHttpClient.GetUserInfo(userEmail);
            if (childId == 0 && userinfo.ViewChild > 0)
            {
                _progId = userinfo.ViewChild;
            }
            if (_progId == 0)
            {
                _progId = 2;
            }
            Progeny progeny = new Progeny();
            progeny = await _progenyHttpClient.GetProgeny(_progId);
            List<UserAccess> accessList = await _progenyHttpClient.GetProgenyAccessList(_progId);

            int userAccessLevel = 5;

            if (accessList.Count != 0)
            {
                UserAccess userAccess = accessList.SingleOrDefault(u => u.UserId == userEmail);
                if (userAccess != null)
                {
                    userAccessLevel = userAccess.AccessLevel;
                }
            }

            if (progeny.Admins.ToUpper().Contains(userEmail.ToUpper()))
            {
                _userIsProgenyAdmin = true;
                userAccessLevel = 0;
            }

            SleepViewModel model = new SleepViewModel();
            model.ProgenyId = _progId;
            model.SleepTotal = TimeSpan.Zero;
            model.SleepLastYear = TimeSpan.Zero;
            model.SleepLastMonth = TimeSpan.Zero;
            List<Sleep> sList = _context.SleepDb.Where(s => s.ProgenyId == _progId).ToList();
            List<Sleep> sleepList = new List<Sleep>();
            DateTime yearAgo = new DateTime(DateTime.UtcNow.Year - 1, DateTime.UtcNow.Month, DateTime.UtcNow.Day, DateTime.UtcNow.Hour, DateTime.UtcNow.Minute, 0);
            DateTime monthAgo = DateTime.UtcNow - TimeSpan.FromDays(30);
            if (sList.Count != 0)
            {
                foreach (Sleep s in sList)
                {

                    bool isLessThanYear = s.SleepEnd > yearAgo;
                    bool isLessThanMonth = s.SleepEnd > monthAgo;
                    s.SleepStart = TimeZoneInfo.ConvertTimeFromUtc(s.SleepStart,
                        TimeZoneInfo.FindSystemTimeZoneById(userinfo.Timezone));
                    s.SleepEnd = TimeZoneInfo.ConvertTimeFromUtc(s.SleepEnd,
                        TimeZoneInfo.FindSystemTimeZoneById(userinfo.Timezone));
                    DateTimeOffset sOffset = new DateTimeOffset(s.SleepStart,
                        TimeZoneInfo.FindSystemTimeZoneById(userinfo.Timezone).GetUtcOffset(s.SleepStart));
                    DateTimeOffset eOffset = new DateTimeOffset(s.SleepEnd,
                        TimeZoneInfo.FindSystemTimeZoneById(userinfo.Timezone).GetUtcOffset(s.SleepEnd));
                    s.SleepDuration = eOffset - sOffset;

                    model.SleepTotal = model.SleepTotal + s.SleepDuration;
                    if (isLessThanYear)
                    {
                        model.SleepLastYear = model.SleepLastYear + s.SleepDuration;
                    }

                    if (isLessThanMonth)
                    {
                        model.SleepLastMonth = model.SleepLastMonth + s.SleepDuration;
                    }

                    if (s.AccessLevel >= userAccessLevel)
                    {
                        sleepList.Add(s);
                    }
                }
                sleepList = sleepList.OrderBy(s => s.SleepStart).ToList();
                model.SleepList = sleepList;

                model.TotalAverage = model.SleepTotal / (DateTime.UtcNow - sleepList.First().SleepStart).TotalDays;
                model.LastYearAverage = model.SleepLastYear / (DateTime.UtcNow - yearAgo).TotalDays;
                model.LastMonthAverage = model.SleepLastMonth / 30;

            }
            else
            {
                Sleep s = new Sleep();
                s.ProgenyId = _progId;
                s.SleepStart = DateTime.UtcNow;
                s.SleepEnd = DateTime.UtcNow;
                s.CreatedDate = DateTime.UtcNow;
                s.SleepNotes = "No sleep data found.";
                model.SleepList = new List<Sleep>();
                model.SleepList.Add(s);
                model.TotalAverage = TimeSpan.Zero;
                model.LastYearAverage = TimeSpan.Zero;
                model.LastMonthAverage = TimeSpan.Zero;
            }

            
            model.IsAdmin = _userIsProgenyAdmin;
            model.Progeny = progeny;

            List<Sleep> chartList = new List<Sleep>();
            foreach (Sleep chartItem in model.SleepList)
            {
                double durationStartDate = 0.0;
                double durationEndDate = 0.0;
                if (chartItem.SleepStart.Date == chartItem.SleepEnd.Date)
                {
                    durationStartDate = durationStartDate + chartItem.SleepDuration.TotalMinutes;
                    if (chartList.SingleOrDefault(s => s.SleepStart.Date == chartItem.SleepStart.Date) !=
                        null)
                    {
                        chartList.SingleOrDefault(s => s.SleepStart.Date == chartItem.SleepStart.Date)
                            .SleepDuration += TimeSpan.FromMinutes(durationStartDate);
                    }
                    else
                    {

                        Sleep newSleep = new Sleep();
                        newSleep.SleepStart = chartItem.SleepStart;
                        newSleep.SleepDuration = TimeSpan.FromMinutes(durationStartDate);
                        chartList.Add(newSleep);
                    }
                }
                else
                {
                    DateTimeOffset sOffset = new DateTimeOffset(chartItem.SleepStart,
                        TimeZoneInfo.FindSystemTimeZoneById(userinfo.Timezone).GetUtcOffset(chartItem.SleepStart));
                    DateTimeOffset s2Offset = new DateTimeOffset(chartItem.SleepStart.Date + TimeSpan.FromDays(1),
                        TimeZoneInfo.FindSystemTimeZoneById(userinfo.Timezone)
                            .GetUtcOffset(chartItem.SleepStart.Date + TimeSpan.FromDays(1)));
                    DateTimeOffset eOffset = new DateTimeOffset(chartItem.SleepEnd,
                        TimeZoneInfo.FindSystemTimeZoneById(userinfo.Timezone).GetUtcOffset(chartItem.SleepEnd));
                    DateTimeOffset e2Offset = new DateTimeOffset(chartItem.SleepEnd.Date,
                        TimeZoneInfo.FindSystemTimeZoneById(userinfo.Timezone)
                            .GetUtcOffset(chartItem.SleepEnd.Date));
                    TimeSpan sDateDuration = s2Offset - sOffset;
                    TimeSpan eDateDuration = eOffset - e2Offset;
                    durationStartDate = chartItem.SleepDuration.TotalMinutes - (eDateDuration.TotalMinutes);
                    durationEndDate = chartItem.SleepDuration.TotalMinutes - sDateDuration.TotalMinutes;
                    if (chartList.SingleOrDefault(s => s.SleepStart.Date == chartItem.SleepStart.Date) !=
                        null)
                    {
                        chartList.SingleOrDefault(s => s.SleepStart.Date == chartItem.SleepStart.Date)
                            .SleepDuration += TimeSpan.FromMinutes(durationStartDate);
                    }
                    else
                    {
                        Sleep newSleep = new Sleep();
                        newSleep.SleepStart = chartItem.SleepStart;
                        newSleep.SleepDuration = TimeSpan.FromMinutes(durationStartDate);
                        chartList.Add(newSleep);
                    }
                    if (chartList.SingleOrDefault(s => s.SleepStart.Date == chartItem.SleepEnd.Date) !=
                        null)
                    {
                        chartList.SingleOrDefault(s => s.SleepStart.Date == chartItem.SleepEnd.Date)
                            .SleepDuration += TimeSpan.FromMinutes(durationEndDate);
                    }
                    else
                    {
                        Sleep newSleep = new Sleep();
                        newSleep.SleepStart = chartItem.SleepEnd;
                        newSleep.SleepDuration = TimeSpan.FromMinutes(durationEndDate);
                        chartList.Add(newSleep);
                    }
                }
            }

            model.ChartList = chartList.OrderBy(s => s.SleepStart).ToList();
            return View(model);
        }

        [AllowAnonymous]
        public async Task<IActionResult> SleepCalendar(int childId = 0)
        {
            _progId = childId;
            string userEmail = HttpContext.User.FindFirst("email")?.Value ?? _defaultUser;
            string userTimeZone = HttpContext.User.FindFirst("timezone")?.Value ?? "Romance Standard Time";
            if (string.IsNullOrEmpty(userTimeZone))
            {
                userTimeZone = "Romance Standard Time";
            }
            UserInfo userinfo = await _progenyHttpClient.GetUserInfo(userEmail);
            if (childId == 0 && userinfo.ViewChild > 0)
            {
                _progId = userinfo.ViewChild;
            }

            Progeny progeny = new Progeny();
            progeny = await _progenyHttpClient.GetProgeny(_progId);
            List<UserAccess> accessList = await _progenyHttpClient.GetProgenyAccessList(_progId);

            int userAccessLevel = 5;

            if (accessList.Count != 0)
            {
                UserAccess userAccess = accessList.SingleOrDefault(u => u.UserId == userEmail);
                if (userAccess != null)
                {
                    userAccessLevel = userAccess.AccessLevel;
                }
            }

            if (progeny.Admins.ToUpper().Contains(userEmail.ToUpper()))
            {
                _userIsProgenyAdmin = true;
                userAccessLevel = 0;
            }

            SleepViewModel model = new SleepViewModel();
            model.ProgenyId = _progId;
            
            List<Sleep> sList = _context.SleepDb.Where(s => s.ProgenyId == _progId).ToList();
            List<Sleep> sleepList = new List<Sleep>();

            if (sList.Count != 0)
            {
                foreach (Sleep s in sList)
                {
                    s.SleepStart = TimeZoneInfo.ConvertTimeFromUtc(s.SleepStart,
                        TimeZoneInfo.FindSystemTimeZoneById(userinfo.Timezone));
                    s.SleepEnd = TimeZoneInfo.ConvertTimeFromUtc(s.SleepEnd,
                        TimeZoneInfo.FindSystemTimeZoneById(userinfo.Timezone));
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
            else
            {
                Sleep s = new Sleep();
                s.ProgenyId = _progId;
                s.SleepStart = DateTime.UtcNow;
                s.SleepEnd = DateTime.UtcNow;
                s.CreatedDate = DateTime.UtcNow;
                s.SleepNotes = "No sleep data found.";
                model.SleepList = new List<Sleep>();
                model.SleepList.Add(s);
            }
            model.IsAdmin = _userIsProgenyAdmin;
            model.Progeny = progeny;
            return View(model);
        }
    }
}