﻿using System;
using System.Collections.Generic;
using System.Linq;
using KinaUnaWeb.Models.ItemViewModels;
using KinaUnaWeb.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using KinaUna.Data.Extensions;
using KinaUna.Data.Models;
using KinaUnaWeb.Models;
using KinaUnaWeb.Models.TypeScriptModels.Timeline;
using KinaUnaWeb.Services.HttpClients;

namespace KinaUnaWeb.Controllers
{
    public class CalendarController : Controller
    {
        private readonly ICalendarsHttpClient _calendarsHttpClient;
        private readonly IViewModelSetupService _viewModelSetupService;

        public CalendarController(ICalendarsHttpClient calendarsHttpClient, IViewModelSetupService viewModelSetupService)
        {
            _calendarsHttpClient = calendarsHttpClient;
            _viewModelSetupService = viewModelSetupService;
        }
        
        [AllowAnonymous]
        public async Task<IActionResult> Index(int? id, int childId = 0)
        {

            BaseItemsViewModel baseModel = await _viewModelSetupService.SetupViewModel(Request.GetLanguageIdFromCookie(), User.GetEmail(), childId);
            CalendarListViewModel model = new(baseModel);
            
            model.SetEventsList(await _calendarsHttpClient.GetCalendarList(model.CurrentProgenyId, model.CurrentAccessLevel));

            return View(model);
        }

        [AllowAnonymous]
        public async Task<IActionResult> ViewEvent(int eventId)
        {
            CalendarItem eventItem = await _calendarsHttpClient.GetCalendarItem(eventId);
            
            BaseItemsViewModel baseModel = await _viewModelSetupService.SetupViewModel(Request.GetLanguageIdFromCookie(), User.GetEmail(), eventItem.ProgenyId);
            CalendarItemViewModel model = new(baseModel);
            
            if (eventItem.AccessLevel < model.CurrentAccessLevel)
            {
                // Todo: Show access denied instead of redirecting.
                RedirectToAction("Index");
            }
            
            model.SetCalendarItem(eventItem);
            
            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> AddEvent()
        {
            BaseItemsViewModel baseModel = await _viewModelSetupService.SetupViewModel(Request.GetLanguageIdFromCookie(), User.GetEmail(), 0);
            CalendarItemViewModel model = new(baseModel);
           
            if (model.CurrentUser == null)
            {
                return RedirectToAction("Index");
            }
            
            if (User.Identity != null && User.Identity.IsAuthenticated && model.CurrentUser.UserEmail != null && model.CurrentUser.UserId != null)
            {
                model.ProgenyList = await _viewModelSetupService.GetProgenySelectList(model.CurrentUser);
                model.SetProgenyList();
            }
            
            model.SetAccessLevelList();
            
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddEvent(CalendarItemViewModel model)
        {
            BaseItemsViewModel baseModel = await _viewModelSetupService.SetupViewModel(Request.GetLanguageIdFromCookie(), User.GetEmail(), model.CurrentProgenyId);
            model.SetBaseProperties(baseModel);

            if (!model.CurrentProgeny.IsInAdminList(model.CurrentUser.UserEmail))
            {
                // Todo: Show no access info.
                return RedirectToAction("Index");
            }

            CalendarItem eventItem = model.CreateCalendarItem();
            
            _ = await _calendarsHttpClient.AddCalendarItem(eventItem);
            
            return RedirectToAction("Index", "Calendar");
        }

        [HttpGet]
        public async Task<IActionResult> EditEvent(int itemId)
        {
            CalendarItem eventItem = await _calendarsHttpClient.GetCalendarItem(itemId);

            BaseItemsViewModel baseModel = await _viewModelSetupService.SetupViewModel(Request.GetLanguageIdFromCookie(), User.GetEmail(), eventItem.ProgenyId);
            CalendarItemViewModel model = new(baseModel);
            
            if (!model.CurrentProgeny.IsInAdminList(model.CurrentUser.UserEmail))
            {
                // Todo: Show no access info.
                return RedirectToAction("Index");
            }
            
            model.SetCalendarItem(eventItem);
            
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditEvent(CalendarItemViewModel model)
        {
            BaseItemsViewModel baseModel = await _viewModelSetupService.SetupViewModel(Request.GetLanguageIdFromCookie(), User.GetEmail(), model.CalendarItem.ProgenyId);
            model.SetBaseProperties(baseModel);

            if (!model.CurrentProgeny.IsInAdminList(model.CurrentUser.UserEmail))
            {
                // Todo: Show no access info.
                return RedirectToAction("Index");
            }
            if (ModelState.IsValid)
            {
                CalendarItem editedEvent = model.CreateCalendarItem();
                
                await _calendarsHttpClient.UpdateCalendarItem(editedEvent);
            }

            return RedirectToAction("Index", "Calendar");
        }

        [HttpGet]
        public async Task<IActionResult> DeleteEvent(int itemId)
        {
            CalendarItem calendarItem = await _calendarsHttpClient.GetCalendarItem(itemId);
            
            BaseItemsViewModel baseModel = await _viewModelSetupService.SetupViewModel(Request.GetLanguageIdFromCookie(), User.GetEmail(), calendarItem.ProgenyId);
            CalendarItemViewModel model = new(baseModel)
            {
                CalendarItem = calendarItem
            };

            if (!model.CurrentProgeny.IsInAdminList(model.CurrentUser.UserEmail))
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
            BaseItemsViewModel baseModel = await _viewModelSetupService.SetupViewModel(Request.GetLanguageIdFromCookie(), User.GetEmail(), model.CurrentProgenyId);
            model.SetBaseProperties(baseModel);

            model.CalendarItem = await _calendarsHttpClient.GetCalendarItem(model.CalendarItem.EventId);
            
            if (!model.CurrentProgeny.IsInAdminList(model.CurrentUser.UserEmail) && model.CurrentProgenyId != model.CalendarItem.ProgenyId)
            {
                // Todo: Show no access info.
                return RedirectToAction("Index");
            }

            await _calendarsHttpClient.DeleteCalendarItem(model.CalendarItem.EventId);

            return RedirectToAction("Index", "Calendar");
        }

        [AllowAnonymous]
        [HttpPost]
        public async Task<IActionResult> GetUpcomingEventsList([FromBody] TimelineParameters parameters)
        {
            TimelineList timelineList = new();
            List<CalendarItem> upcomingCalendarItems = await _calendarsHttpClient.GetCalendarList(parameters.ProgenyId, 0);
            upcomingCalendarItems = upcomingCalendarItems.Where(c => c.EndTime > DateTime.UtcNow).ToList();
            upcomingCalendarItems = upcomingCalendarItems.OrderBy(c => c.StartTime).ToList();
            
            timelineList.AllItemsCount = upcomingCalendarItems.Count;
            timelineList.RemainingItemsCount = upcomingCalendarItems.Count - parameters.Skip - parameters.Count;

            upcomingCalendarItems = upcomingCalendarItems.Skip(parameters.Skip).Take(parameters.Count).ToList();

            foreach (CalendarItem eventItem in upcomingCalendarItems)
            {
                TimeLineItem eventTimelineItem = new()
                {
                    ProgenyId = eventItem.ProgenyId,
                    AccessLevel = eventItem.AccessLevel,
                    ItemId = eventItem.EventId.ToString(),
                    ItemType = (int)KinaUnaTypes.TimeLineType.Calendar
                };
                timelineList.TimelineItems.Add(eventTimelineItem);
            }
            
            return Json(timelineList);

        }
    }
}