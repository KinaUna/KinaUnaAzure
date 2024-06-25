using System;
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
    public class CalendarController(ICalendarsHttpClient calendarsHttpClient, IViewModelSetupService viewModelSetupService) : Controller
    {
        [AllowAnonymous]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE0060:Remove unused parameter", Justification = "<Pending>")]
        public async Task<IActionResult> Index(int? id, int childId = 0)
        {

            BaseItemsViewModel baseModel = await viewModelSetupService.SetupViewModel(Request.GetLanguageIdFromCookie(), User.GetEmail(), childId);
            CalendarListViewModel model = new(baseModel);
            
            model.SetEventsList(await calendarsHttpClient.GetCalendarList(model.CurrentProgenyId, model.CurrentAccessLevel));

            return View(model);
        }

        [AllowAnonymous]
        public async Task<IActionResult> ViewEvent(int eventId)
        {
            CalendarItem eventItem = await calendarsHttpClient.GetCalendarItem(eventId);
            
            BaseItemsViewModel baseModel = await viewModelSetupService.SetupViewModel(Request.GetLanguageIdFromCookie(), User.GetEmail(), eventItem.ProgenyId);
            CalendarItemViewModel model = new(baseModel);
            
            if (eventItem.AccessLevel < model.CurrentAccessLevel)
            {
                // Todo: Show access denied instead of redirecting.
                RedirectToAction("Index");
            }
            
            model.SetCalendarItem(eventItem);
            
            return View(model);
        }

        [AllowAnonymous]
        public async Task<IActionResult> GetEventItem(int eventId)
        {
            CalendarItem eventItem = await calendarsHttpClient.GetCalendarItem(eventId);

            BaseItemsViewModel baseModel = await viewModelSetupService.SetupViewModel(Request.GetLanguageIdFromCookie(), User.GetEmail(), eventItem.ProgenyId);
            CalendarItemViewModel model = new(baseModel);

            if (eventItem.AccessLevel < model.CurrentAccessLevel)
            {
                // Todo: Show access denied instead of redirecting.
                return Unauthorized();
            }

            return PartialView("_GetEventItemPartial", eventItem);
        }

        [HttpGet]
        public async Task<IActionResult> AddEvent()
        {
            BaseItemsViewModel baseModel = await viewModelSetupService.SetupViewModel(Request.GetLanguageIdFromCookie(), User.GetEmail(), 0);
            CalendarItemViewModel model = new(baseModel);
           
            if (model.CurrentUser == null)
            {
                return RedirectToAction("Index");
            }
            
            if (User.Identity != null && User.Identity.IsAuthenticated && model.CurrentUser.UserEmail != null && model.CurrentUser.UserId != null)
            {
                model.ProgenyList = await viewModelSetupService.GetProgenySelectList(model.CurrentUser);
                model.SetProgenyList();
            }
            
            model.SetAccessLevelList();
            
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddEvent(CalendarItemViewModel model)
        {
            BaseItemsViewModel baseModel = await viewModelSetupService.SetupViewModel(Request.GetLanguageIdFromCookie(), User.GetEmail(), model.CurrentProgenyId);
            model.SetBaseProperties(baseModel);

            if (!model.CurrentProgeny.IsInAdminList(model.CurrentUser.UserEmail))
            {
                // Todo: Show no access info.
                return RedirectToAction("Index");
            }

            CalendarItem eventItem = model.CreateCalendarItem();
            
            _ = await calendarsHttpClient.AddCalendarItem(eventItem);
            
            return RedirectToAction("Index", "Calendar");
        }

        [HttpGet]
        public async Task<IActionResult> EditEvent(int itemId)
        {
            CalendarItem eventItem = await calendarsHttpClient.GetCalendarItem(itemId);

            BaseItemsViewModel baseModel = await viewModelSetupService.SetupViewModel(Request.GetLanguageIdFromCookie(), User.GetEmail(), eventItem.ProgenyId);
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
            BaseItemsViewModel baseModel = await viewModelSetupService.SetupViewModel(Request.GetLanguageIdFromCookie(), User.GetEmail(), model.CalendarItem.ProgenyId);
            model.SetBaseProperties(baseModel);

            if (!model.CurrentProgeny.IsInAdminList(model.CurrentUser.UserEmail))
            {
                // Todo: Show no access info.
                return RedirectToAction("Index");
            }

            if (!ModelState.IsValid) return RedirectToAction("Index", "Calendar");

            CalendarItem editedEvent = model.CreateCalendarItem();
                
            await calendarsHttpClient.UpdateCalendarItem(editedEvent);

            return RedirectToAction("Index", "Calendar");
        }

        [HttpGet]
        public async Task<IActionResult> DeleteEvent(int itemId)
        {
            CalendarItem calendarItem = await calendarsHttpClient.GetCalendarItem(itemId);
            
            BaseItemsViewModel baseModel = await viewModelSetupService.SetupViewModel(Request.GetLanguageIdFromCookie(), User.GetEmail(), calendarItem.ProgenyId);
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
            BaseItemsViewModel baseModel = await viewModelSetupService.SetupViewModel(Request.GetLanguageIdFromCookie(), User.GetEmail(), model.CurrentProgenyId);
            model.SetBaseProperties(baseModel);

            model.CalendarItem = await calendarsHttpClient.GetCalendarItem(model.CalendarItem.EventId);
            
            if (!model.CurrentProgeny.IsInAdminList(model.CurrentUser.UserEmail) && model.CurrentProgenyId != model.CalendarItem.ProgenyId)
            {
                // Todo: Show no access info.
                return RedirectToAction("Index");
            }

            await calendarsHttpClient.DeleteCalendarItem(model.CalendarItem.EventId);

            return RedirectToAction("Index", "Calendar");
        }

        [AllowAnonymous]
        [HttpPost]
        public async Task<IActionResult> GetUpcomingEventsList([FromBody] TimelineParameters parameters)
        {
            TimelineList timelineList = new();
            List<CalendarItem> upcomingCalendarItems = await calendarsHttpClient.GetCalendarList(parameters.ProgenyId, 0);
            upcomingCalendarItems = upcomingCalendarItems.Where(c => c.EndTime > DateTime.UtcNow).ToList();
            upcomingCalendarItems = [.. upcomingCalendarItems.OrderBy(c => c.StartTime)];
            
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