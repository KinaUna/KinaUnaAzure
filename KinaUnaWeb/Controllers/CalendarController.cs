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
    /// <summary>
    /// Calendar controller. Handles all actions related to the Calendar.
    /// </summary>
    /// <param name="calendarsHttpClient"></param>
    /// <param name="viewModelSetupService"></param>
    public class CalendarController(ICalendarsHttpClient calendarsHttpClient, IViewModelSetupService viewModelSetupService) : Controller
    {
        /// <summary>
        /// Calendar Index page.
        /// </summary>
        /// <param name="id">Optional EventId of a CalendarItem to show in a popup.</param>
        /// <param name="childId">The Id of the Progeny to show the calendar for.</param>
        /// <returns>View with a CalendarListViewModel.</returns>
        [AllowAnonymous]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE0060:Remove unused parameter", Justification = "<Pending>")]
        public async Task<IActionResult> Index(int? id, int childId = 0)
        {

            BaseItemsViewModel baseModel = await viewModelSetupService.SetupViewModel(Request.GetLanguageIdFromCookie(), User.GetEmail(), childId);
            CalendarListViewModel model = new(baseModel);
            
            model.SetEventsList(await calendarsHttpClient.GetCalendarList(model.CurrentProgenyId, model.CurrentAccessLevel));

            return View(model);
        }

        /// <summary>
        /// Display a single CalendarItem.
        /// </summary>
        /// <param name="eventId">The EventId of the CalendarItem to show.</param>
        /// <param name="partialView">If true, returns partial view. For inline fetching of HTML to show in a modal or popup.</param>
        /// <returns></returns>
        [AllowAnonymous]
        public async Task<IActionResult> ViewEvent(int eventId, bool partialView = false)
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
            model.CalendarItem.Progeny = model.CurrentProgeny;
            model.CalendarItem.Progeny.PictureLink = model.CalendarItem.Progeny.GetProfilePictureUrl();

            if (partialView)
            {
                return PartialView("_CalendarItemDetailsPartial", model);
            }

            return View(model);
        }

        /// <summary>
        /// Page to add a new CalendarItem.
        /// </summary>
        /// <returns>View with CalendarItemViewModel</returns>
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

        /// <summary>
        /// HttpPost action to add a new CalendarItem.
        /// </summary>
        /// <param name="model">CalendarItemViewModel with the properties for the new CalendarItem.</param>
        /// <returns>Redirect to Calendar/Index page.</returns>
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

        /// <summary>
        /// Edit page for a CalendarItem.
        /// </summary>
        /// <param name="itemId">The EventId of the CalendarItem to edit.</param>
        /// <returns>View with CalendarItemViewModel.</returns>
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

        /// <summary>
        /// HttpPost action to update an edited CalendarItem.
        /// </summary>
        /// <param name="model">CalendarItemViewModel with the properties for updating the CalendarItem.</param>
        /// <returns>Redirects to Calendar/Index page.</returns>
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

        /// <summary>
        /// Page to delete a CalendarItem.
        /// </summary>
        /// <param name="itemId">The EventId of the CalendarItem to delete.</param>
        /// <returns>View with a CalendarItemViewModel.</returns>
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

        /// <summary>
        /// HttpPost action to delete a CalendarItem.
        /// </summary>
        /// <param name="model">CalendarItemViewModel with the CalendarItem properties.</param>
        /// <returns>Redirects to Calendar/Index page.</returns>
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

        /// <summary>
        /// HttpPost action to get a list of upcoming CalendarItems.
        /// </summary>
        /// <param name="parameters">TimeLineParameters object.</param>
        /// <returns>Json of TimelineList object.</returns>
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

        /// <summary>
        /// Provides a partial view with translations for the Scheduler.
        /// The HTML returned contains JavaScript with the translated strings for the Syncfusion Scheduler.
        /// </summary>
        /// <param name="languageId">The Id of the language to translate into.</param>
        /// <returns>PartialView.</returns>
        public IActionResult SchedulerTranslations(int languageId)
        {
            return PartialView("_SchedulerTranslationsPartial", languageId);
        }
    }
}