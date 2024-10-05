﻿using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using KinaUnaWeb.Models.ItemViewModels;
using KinaUnaWeb.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using KinaUna.Data.Extensions;
using KinaUna.Data.Models;
using KinaUnaWeb.Models;
using KinaUnaWeb.Models.TypeScriptModels.Calendar;
using KinaUnaWeb.Models.TypeScriptModels.Timeline;
using KinaUnaWeb.Services.HttpClients;

namespace KinaUnaWeb.Controllers
{
    /// <summary>
    /// Calendar controller. Handles all actions related to the Calendar.
    /// </summary>
    /// <param name="calendarsHttpClient"></param>
    /// <param name="viewModelSetupService"></param>
    public class CalendarController(ICalendarsHttpClient calendarsHttpClient,
        ICalendarRemindersHttpClient calendarRemindersHttpClient,
        IViewModelSetupService viewModelSetupService,
        IUserInfosHttpClient userInfosHttpClient) : Controller
    {
        /// <summary>
        /// Calendar Index page.
        /// </summary>
        /// <param name="eventId">Optional EventId of a CalendarItem to show in a popup.</param>
        /// <param name="childId">The Id of the Progeny to show the calendar for.</param>
        /// <returns>View with a CalendarListViewModel.</returns>
        [AllowAnonymous]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE0060:Remove unused parameter", Justification = "<Pending>")]
        public async Task<IActionResult> Index(int? eventId, int childId = 0)
        {

            BaseItemsViewModel baseModel = await viewModelSetupService.SetupViewModel(Request.GetLanguageIdFromCookie(), User.GetEmail(), childId);
            CalendarListViewModel model = new(baseModel);
            
            model.SetEventsList(await calendarsHttpClient.GetCalendarList(model.CurrentProgenyId, model.CurrentAccessLevel));
            model.PopupEventId = eventId ?? 0;
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
            if (!partialView)
            {
                return RedirectToAction("Index", new{eventId});
            }

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
            model.SetReminderOffsetList(await viewModelSetupService.CreateReminderOffsetSelectListItems(model.LanguageId));

            List<CalendarReminder> calendarReminders = await calendarRemindersHttpClient.GetUsersCalendarRemindersForEvent(eventId, model.CurrentUser.UserId);
            if (calendarReminders == null) return PartialView("_CalendarItemDetailsPartial", model);

            foreach (CalendarReminder calendarReminder in calendarReminders)
            {
                calendarReminder.NotifyTime = TimeZoneInfo.ConvertTimeFromUtc(calendarReminder.NotifyTime, TimeZoneInfo.FindSystemTimeZoneById(model.CurrentUser.Timezone));
            }
            model.CalendarReminders = calendarReminders;

            return PartialView("_CalendarItemDetailsPartial", model);
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
            
            eventItem = await calendarsHttpClient.AddCalendarItem(eventItem);
            
            int eventId = eventItem.EventId;

            return RedirectToAction("Index", "Calendar", new{eventId});
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

            model.SetReminderOffsetList(await viewModelSetupService.CreateReminderOffsetSelectListItems(model.LanguageId));

            List<CalendarReminder> calendarReminders = await calendarRemindersHttpClient.GetUsersCalendarRemindersForEvent(eventItem.EventId, model.CurrentUser.UserId);
            if (calendarReminders == null) return View(model);

            foreach (CalendarReminder calendarReminder in calendarReminders)
            {
                calendarReminder.NotifyTime = TimeZoneInfo.ConvertTimeFromUtc(calendarReminder.NotifyTime, TimeZoneInfo.FindSystemTimeZoneById(model.CurrentUser.Timezone));
            }

            model.CalendarReminders = calendarReminders;

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
            int eventId = model.CalendarItem.EventId;

            return RedirectToAction("Index", "Calendar", new {eventId});
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

        /// <summary>
        /// Adds a new CalendarReminder.
        /// </summary>
        /// <param name="calendarReminderRequest">CalendarReminderRequest with the properties of the reminder to add.</param>
        /// <returns>HTML with the reminder element.</returns>
        [HttpPost]
        public async Task<IActionResult> AddReminder([FromBody]CalendarReminderRequest calendarReminderRequest)
        {
            UserInfo currentUser = await userInfosHttpClient.GetUserInfo(User.GetEmail());

            CalendarReminder calendarReminder = new CalendarReminder();
            calendarReminder.EventId = calendarReminderRequest.EventId;
            calendarReminder.UserId = currentUser.UserId;

            CalendarItem calendarItem = await calendarsHttpClient.GetCalendarItem(calendarReminderRequest.EventId);
            if (calendarItem == null)
            {
                return BadRequest();
            }

            //if (calendarItem.AccessLevel < currentUser.AccessLevel)
            //{
            //    return Unauthorized();
            //}

            if (calendarReminderRequest.NotifyTimeOffsetType != 0 && calendarItem.StartTime.HasValue)
            {
                calendarReminder.NotifyTime = calendarItem.StartTime.Value.AddMinutes(-calendarReminderRequest.NotifyTimeOffsetType);
            }
            else
            {
                bool notifyTimeParsed = DateTime.TryParseExact(calendarReminderRequest.NotifyTimeString, "dd/MM/yyyy HH:mm", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime notifyTime);
                if (!notifyTimeParsed)
                {
                    return BadRequest();
                }

                calendarReminder.NotifyTime = notifyTime;
            }
            

            calendarReminder.NotifyTime = TimeZoneInfo.ConvertTimeToUtc(calendarReminder.NotifyTime, TimeZoneInfo.FindSystemTimeZoneById(currentUser.Timezone));

            CalendarReminder newReminder = await calendarRemindersHttpClient.AddCalendarReminder(calendarReminder);

            newReminder.NotifyTime = TimeZoneInfo.ConvertTimeFromUtc(newReminder.NotifyTime, TimeZoneInfo.FindSystemTimeZoneById(currentUser.Timezone));

            return PartialView("_CalendarReminderItemPartial", newReminder);
        }

        /// <summary>
        /// Deletes a CalendarReminder.
        /// </summary>
        /// <param name="calendarReminderRequest">CalendarReminderRequest with the id of the reminder to delete.</param>
        /// <returns>Ok response.</returns>
        [HttpPost]
        public async Task<IActionResult> DeleteReminder([FromBody] CalendarReminderRequest calendarReminderRequest)
        {
            CalendarReminder existingCalendarReminder = await calendarRemindersHttpClient.GetCalendarReminder(calendarReminderRequest.CalendarReminderId);
            UserInfo currentUser = await userInfosHttpClient.GetUserInfo(User.GetEmail());
            if (existingCalendarReminder.UserId != currentUser.UserId)
            {
                return Unauthorized();
            }

            CalendarReminder result = await calendarRemindersHttpClient.DeleteCalendarReminder(existingCalendarReminder);
            if (result != null)
            {
                return Ok();
            }

            return BadRequest();
        }
    }
}