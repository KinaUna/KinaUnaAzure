using KinaUna.Data.Extensions;
using KinaUna.Data.Models.AccessManagement;
using KinaUna.Data.Models.DTOs;
using KinaUna.Data.Models.Family;
using KinaUnaWeb.Models;
using KinaUnaWeb.Models.ItemViewModels;
using KinaUnaWeb.Models.TypeScriptModels.Calendar;
using KinaUnaWeb.Models.TypeScriptModels.Timeline;
using KinaUnaWeb.Services;
using KinaUnaWeb.Services.HttpClients;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using KinaUna.Data.Models.Timeline;

namespace KinaUnaWeb.Controllers
{
    /// <summary>
    /// Calendar controller. Handles all actions related to the Calendar.
    /// </summary>
    /// <param name="calendarsHttpClient"></param>
    /// <param name="viewModelSetupService"></param>
    public class CalendarController(
        ICalendarsHttpClient calendarsHttpClient,
        ICalendarRemindersHttpClient calendarRemindersHttpClient,
        ITodoItemsHttpClient todoItemsHttpClient,
        IViewModelSetupService viewModelSetupService,
        IUserInfosHttpClient userInfosHttpClient,
        IProgenyHttpClient progenyHttpClient,
        IFamiliesHttpClient familiesHttpClient) : Controller
    {
        /// <summary>
        /// Calendar Index page.
        /// </summary>
        /// <param name="eventId">Optional EventId of a CalendarItem to show in a popup.</param>
        /// <param name="childId">The Id of the Progeny to show the calendar for.</param>
        /// <param name="familyId">The Id of the Family to show the calendar for.</param>
        /// <returns>View with a CalendarListViewModel.</returns>
        [AllowAnonymous]
        public async Task<IActionResult> Index(int? eventId, int childId = 0, int familyId = 0)
        {
            // Todo: Add EventDate parameter for popup with recurring events.
            BaseItemsViewModel baseModel = await viewModelSetupService.SetupViewModel(Request.GetLanguageIdFromCookie(), User.GetEmail(), childId, familyId);
            CalendarListViewModel model = new(baseModel)
            {
                PopupEventId = eventId ?? 0
            };

            if (model.PopupEventId != 0)
            {
                model.EventsList.Add(await calendarsHttpClient.GetCalendarItem(model.PopupEventId));
            }
            return View(model);
        }

        [AllowAnonymous]
        [HttpPost]
        public async Task<IActionResult> GetCalendarList([FromBody] CalendarItemsRequest request)
        {
            UserInfo currentUserInfo = await userInfosHttpClient.GetUserInfo(User.GetEmail());
            request.StartDate = new DateTime(request.StartYear, request.StartMonth, request.StartDay);
            request.EndDate = new DateTime(request.EndYear, request.EndMonth, request.EndDay);

            List<CalendarItem> calendarItems = await calendarsHttpClient.GetProgeniesCalendarList(request);

            calendarItems = [.. calendarItems.OrderBy(e => e.StartTime)];
            List<CalendarItem> resultList = [];
            
            foreach (CalendarItem ev in calendarItems)
            {
                if (!ev.StartTime.HasValue || !ev.EndTime.HasValue) continue;

                ev.StartTime = TimeZoneInfo.ConvertTimeFromUtc(ev.StartTime.Value,
                    TimeZoneInfo.FindSystemTimeZoneById(currentUserInfo.Timezone));
                ev.EndTime = TimeZoneInfo.ConvertTimeFromUtc(ev.EndTime.Value,
                    TimeZoneInfo.FindSystemTimeZoneById(currentUserInfo.Timezone));

                // ToDo: Replace format string with configuration or user defined value
                ev.StartString = ev.StartTime.Value.ToString("yyyy-MM-dd") + "T" + ev.StartTime.Value.ToString("HH:mm:ss");
                ev.EndString = ev.EndTime.Value.ToString("yyyy-MM-dd") + "T" + ev.EndTime.Value.ToString("HH:mm:ss");
                if (ev.ItemPerMission != null)
                {
                    ev.IsReadonly = ev.ItemPerMission.PermissionLevel < PermissionLevel.Edit;
                }
                else
                {
                    ev.IsReadonly = true;
                }
                 
                // Todo: Add color property
                resultList.Add(ev);
            }

            return Json(resultList);
        }
        /// <summary>
        /// Display a single CalendarItem.
        /// </summary>
        /// <param name="eventId">The EventId of the CalendarItem to show.</param>
        /// <param name="partialView">If true, returns partial view. For inline fetching of HTML to show in a modal or popup.</param>
        /// <param name="year">Optional year for recurring events..</param>
        /// <param name="month">Optional month for recurring events.</param>
        /// <param name="day">Optional day for recurring events.</param>
        /// <returns></returns>
        [AllowAnonymous]
        public async Task<IActionResult> ViewEvent(int eventId, bool partialView = false, int year = 0, int month = 0, int day = 0)
        {
            if (!partialView)
            {
                return RedirectToAction("Index", new{eventId});
            }

            CalendarItem eventItem = await calendarsHttpClient.GetCalendarItem(eventId);
            if (eventItem.EventId == 0)
            {
                return PartialView("_AccessDeniedPartial");
            }

            BaseItemsViewModel baseModel = await viewModelSetupService.SetupViewModel(Request.GetLanguageIdFromCookie(), User.GetEmail(), eventItem.ProgenyId, eventItem.FamilyId, false);
            CalendarItemViewModel model = new(baseModel);
            
            model.SetCalendarItem(eventItem);
            // Year, month and day are used for recurring events, and is in the user's timezone, make sure to update the event's start and end times after converting from UTC.
            if (model.CalendarItem.RecurrenceRuleId > 0 && year > 0 && month > 0 && day > 0 && model.CalendarItem.StartTime.HasValue && model.CalendarItem.EndTime.HasValue)
            {
                TimeSpan eventDuration = model.CalendarItem.EndTime.Value - model.CalendarItem.StartTime.Value;
                model.CalendarItem.StartTime = new DateTime(year, month, day, model.CalendarItem.StartTime.Value.Hour, model.CalendarItem.StartTime.Value.Minute, model.CalendarItem.StartTime.Value.Second);
                model.CalendarItem.EndTime = model.CalendarItem.StartTime.Value + eventDuration;
            }

            if (model.CalendarItem.ProgenyId > 0)
            {
                model.CalendarItem.Progeny = model.CurrentProgeny;
                model.CalendarItem.Progeny.PictureLink = model.CalendarItem.Progeny.GetProfilePictureUrl();
            }

            if (model.CalendarItem.FamilyId > 0)
            {
                model.CalendarItem.Family = model.CurrentFamily;
                model.CalendarItem.Family.PictureLink = model.CalendarItem.Family.GetProfilePictureUrl();
            }
            
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
            BaseItemsViewModel baseModel = await viewModelSetupService.SetupViewModel(Request.GetLanguageIdFromCookie(), User.GetEmail(), 0, 0, false);
            CalendarItemViewModel model = new(baseModel);

            model.ProgenyList = await viewModelSetupService.GetProgenySelectList();
            model.SetProgenyList();
            model.FamilyList = await viewModelSetupService.GetFamilySelectList();
            model.SetFamilyList();

            model.CalendarItem.StartTime = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, TimeZoneInfo.FindSystemTimeZoneById(model.CurrentUser.Timezone));
            model.CalendarItem.EndTime = model.CalendarItem.StartTime + TimeSpan.FromMinutes(10);
            model.SetReminderOffsetList(await viewModelSetupService.CreateReminderOffsetSelectListItems(model.LanguageId));
            model.SetRecurrenceFrequencyList();
            model.SetEndOptionsList();
            model.CalendarItem.RecurrenceRule.ByDay = "";
            model.CalendarItem.RecurrenceRule.ByMonthDay = "";
            model.SetMonthlyByDayPrefixList();
            model.SetMonthsSelectList();

            return PartialView("_AddEventPartial", model);
        }

        /// <summary>
        /// HttpPost action to add a new CalendarItem.
        /// </summary>
        /// <param name="model">CalendarItemViewModel with the properties for the new CalendarItem.</param>
        /// <returns>Redirect to Calendar/Index page.</returns>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddEvent([FromForm] CalendarItemViewModel model)
        {
            BaseItemsViewModel baseModel = await viewModelSetupService.SetupViewModel(Request.GetLanguageIdFromCookie(), User.GetEmail(), model.CalendarItem.ProgenyId, model.CalendarItem.FamilyId, false);
            model.SetBaseProperties(baseModel);

            bool canUserAdd = false;
            if (model.CalendarItem.ProgenyId > 0)
            {
                List<Progeny> progenies = await progenyHttpClient.GetProgeniesUserCanAccess(PermissionLevel.Add);
                if (progenies.Exists(p => p.Id == model.CalendarItem.ProgenyId))
                {
                    canUserAdd = true;
                }
            }

            if (model.CalendarItem.FamilyId > 0)
            {
                List<Family> families = await familiesHttpClient.GetFamiliesUserCanAccess(PermissionLevel.Add);
                if (families.Exists(f => f.FamilyId == model.CalendarItem.FamilyId))
                {
                    canUserAdd = true;
                }
            }

            if (!canUserAdd)
            {
                // Todo: Show that no children are available to add kanban for.
                return RedirectToAction("Index");
            }

            CalendarItem eventItem = model.CreateCalendarItem();
            
            model.CalendarItem = await calendarsHttpClient.AddCalendarItem(eventItem);
            if (!model.CalendarItem.StartTime.HasValue || !model.CalendarItem.EndTime.HasValue) return PartialView("_EventAddedPartial", model);
            
            model.CalendarItem.StartTime = TimeZoneInfo.ConvertTimeFromUtc(model.CalendarItem.StartTime.Value, TimeZoneInfo.FindSystemTimeZoneById(model.CurrentUser.Timezone));
            model.CalendarItem.EndTime = TimeZoneInfo.ConvertTimeFromUtc(model.CalendarItem.EndTime.Value, TimeZoneInfo.FindSystemTimeZoneById(model.CurrentUser.Timezone));
            model.SetReminderOffsetList(await viewModelSetupService.CreateReminderOffsetSelectListItems(model.LanguageId));

            if (model.CalendarItem.ProgenyId > 0)
            {
                model.CalendarItem.Progeny = await progenyHttpClient.GetProgeny(model.CalendarItem.ProgenyId);
            }
            if (model.CalendarItem.FamilyId > 0)
            {
                model.CalendarItem.Family = await familiesHttpClient.GetFamily(model.CalendarItem.FamilyId);
            }

            return PartialView("_EventAddedPartial", model);
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
            if (eventItem == null || eventItem.EventId == 0)
            {
                return PartialView("_NotFoundPartial");
            }

            if (eventItem.ItemPerMission.PermissionLevel < PermissionLevel.Edit)
            {
                return Unauthorized();
            }

            BaseItemsViewModel baseModel = await viewModelSetupService.SetupViewModel(Request.GetLanguageIdFromCookie(), User.GetEmail(), eventItem.ProgenyId, eventItem.FamilyId, false);
            CalendarItemViewModel model = new(baseModel)
            {
                ProgenyList = await viewModelSetupService.GetProgenySelectList(eventItem.ProgenyId),
                FamilyList = await viewModelSetupService.GetFamilySelectList(eventItem.FamilyId)
            };
            
            model.SetCalendarItem(eventItem);

            model.SetReminderOffsetList(await viewModelSetupService.CreateReminderOffsetSelectListItems(model.LanguageId));

            List<CalendarReminder> calendarReminders = await calendarRemindersHttpClient.GetUsersCalendarRemindersForEvent(eventItem.EventId, model.CurrentUser.UserId);
            if (calendarReminders == null) return PartialView("_EditEventPartial", model);

            foreach (CalendarReminder calendarReminder in calendarReminders)
            {
                calendarReminder.NotifyTime = TimeZoneInfo.ConvertTimeFromUtc(calendarReminder.NotifyTime, TimeZoneInfo.FindSystemTimeZoneById(model.CurrentUser.Timezone));
            }

            model.CalendarReminders = calendarReminders;

            return PartialView("_EditEventPartial", model);
        }

        /// <summary>
        /// HttpPost action to update an edited CalendarItem.
        /// </summary>
        /// <param name="model">CalendarItemViewModel with the properties for updating the CalendarItem.</param>
        /// <returns>Redirects to Calendar/Index page.</returns>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditEvent([FromForm] CalendarItemViewModel model)
        {
            CalendarItem existingCalendarItem = await calendarsHttpClient.GetCalendarItem(model.CalendarItem.EventId);
            if (existingCalendarItem == null || existingCalendarItem.EventId == 0)
            {
                return PartialView("_NotFoundPartial");
            }

            if (existingCalendarItem.ItemPerMission.PermissionLevel < PermissionLevel.Edit)
            {
                return Unauthorized();
            }

            BaseItemsViewModel baseModel = await viewModelSetupService.SetupViewModel(Request.GetLanguageIdFromCookie(), User.GetEmail(), model.CalendarItem.ProgenyId, model.CalendarItem.FamilyId, false);
            model.SetBaseProperties(baseModel);
            
            CalendarItem editedEvent = model.CreateCalendarItem();
                
            model.CalendarItem = await calendarsHttpClient.UpdateCalendarItem(editedEvent);
            
            if (!model.CalendarItem.StartTime.HasValue || !model.CalendarItem.EndTime.HasValue) return PartialView("_NotFoundPartial", model); // Todo: Show error message instead.

            model.CalendarItem.StartTime = TimeZoneInfo.ConvertTimeFromUtc(model.CalendarItem.StartTime.Value, TimeZoneInfo.FindSystemTimeZoneById(model.CurrentUser.Timezone));
            model.CalendarItem.EndTime = TimeZoneInfo.ConvertTimeFromUtc(model.CalendarItem.EndTime.Value, TimeZoneInfo.FindSystemTimeZoneById(model.CurrentUser.Timezone));
            model.SetReminderOffsetList(await viewModelSetupService.CreateReminderOffsetSelectListItems(model.LanguageId));

            if (model.CalendarItem.ProgenyId > 0)
            {
                model.CalendarItem.Progeny = await progenyHttpClient.GetProgeny(model.CalendarItem.ProgenyId);
            }
            if (model.CalendarItem.FamilyId > 0)
            {
                model.CalendarItem.Family = await familiesHttpClient.GetFamily(model.CalendarItem.FamilyId);
            }

            return PartialView("_EventUpdatedPartial", model);
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
            if (calendarItem == null || calendarItem.EventId == 0 || calendarItem.ItemPerMission.PermissionLevel < PermissionLevel.Admin)
            {
                return PartialView("_AccessDeniedPartial");
            }

            BaseItemsViewModel baseModel = await viewModelSetupService.SetupViewModel(Request.GetLanguageIdFromCookie(), User.GetEmail(), calendarItem.ProgenyId, calendarItem.FamilyId, false);
            CalendarItemViewModel model = new(baseModel)
            {
                CalendarItem = calendarItem
            };

            if (calendarItem.ProgenyId > 0)
            {
                model.CalendarItem.Progeny = model.CurrentProgeny;
            }

            if (calendarItem.FamilyId > 0)
            {
                model.CalendarItem.Family = model.CurrentFamily;
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
        public async Task<IActionResult> DeleteEvent([FromForm] CalendarItemViewModel model)
        {
            BaseItemsViewModel baseModel = await viewModelSetupService.SetupViewModel(Request.GetLanguageIdFromCookie(), User.GetEmail(), model.CurrentProgenyId, model.CurrentFamilyId, false);
            model.SetBaseProperties(baseModel);

            model.CalendarItem = await calendarsHttpClient.GetCalendarItem(model.CalendarItem.EventId);
            if (model.CalendarItem == null || model.CalendarItem.EventId == 0)
            {
                return PartialView("_NotFoundPartial");
            }

            if (model.CalendarItem.ItemPerMission.PermissionLevel < PermissionLevel.Admin)
            {
                return PartialView("_AccessDeniedPartial");
            }

            await calendarsHttpClient.DeleteCalendarItem(model.CalendarItem.EventId);

            return RedirectToAction("Index", "Calendar");
        }

        /// <summary>
        /// Copy page for a CalendarItem.
        /// </summary>
        /// <param name="itemId">The EventId of the CalendarItem to copy.</param>
        /// <returns>View with CalendarItemViewModel.</returns>
        [HttpGet]
        public async Task<IActionResult> CopyEvent(int itemId)
        {
            CalendarItem eventItem = await calendarsHttpClient.GetCalendarItem(itemId);

            if (eventItem == null || eventItem.EventId == 0)
            {
                return PartialView("_AccessDeniedPartial");
            }
            
            BaseItemsViewModel baseModel = await viewModelSetupService.SetupViewModel(Request.GetLanguageIdFromCookie(), User.GetEmail(), eventItem.ProgenyId, eventItem.FamilyId, false);
            CalendarItemViewModel model = new(baseModel);
            
            model.SetCalendarItem(eventItem);

            model.SetReminderOffsetList(await viewModelSetupService.CreateReminderOffsetSelectListItems(model.LanguageId));

            model.ProgenyList = await viewModelSetupService.GetProgenySelectList(eventItem.ProgenyId);
            model.SetProgenyList();
            model.FamilyList = await viewModelSetupService.GetFamilySelectList(eventItem.FamilyId);
            model.SetFamilyList();

            List<CalendarReminder> calendarReminders = await calendarRemindersHttpClient.GetUsersCalendarRemindersForEvent(eventItem.EventId, model.CurrentUser.UserId);
            if (calendarReminders == null) return PartialView("_CopyEventPartial", model);

            foreach (CalendarReminder calendarReminder in calendarReminders)
            {
                calendarReminder.NotifyTime = TimeZoneInfo.ConvertTimeFromUtc(calendarReminder.NotifyTime, TimeZoneInfo.FindSystemTimeZoneById(model.CurrentUser.Timezone));
            }

            model.CalendarReminders = calendarReminders;

            return PartialView("_CopyEventPartial", model);
        }

        /// <summary>
        /// HttpPost action to copy a CalendarItem.
        /// </summary>
        /// <param name="model">CalendarItemViewModel with the properties for copying the CalendarItem.</param>
        /// <returns></returns>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CopyEvent([FromForm] CalendarItemViewModel model)
        {
            BaseItemsViewModel baseModel = await viewModelSetupService.SetupViewModel(Request.GetLanguageIdFromCookie(), User.GetEmail(), model.CalendarItem.ProgenyId, model.CalendarItem.FamilyId, false);
            model.SetBaseProperties(baseModel);
            bool canUserAdd = false;
            if (model.CalendarItem.ProgenyId > 0)
            {
                List<Progeny> progenies = await progenyHttpClient.GetProgeniesUserCanAccess(PermissionLevel.Add);
                if (progenies.Exists(p => p.Id == model.CalendarItem.ProgenyId))
                {
                    canUserAdd = true;
                }
            }

            if (model.CalendarItem.FamilyId > 0)
            {
                List<Family> families = await familiesHttpClient.GetFamiliesUserCanAccess(PermissionLevel.Add);
                if (families.Exists(f => f.FamilyId == model.CalendarItem.FamilyId))
                {
                    canUserAdd = true;
                }
            }

            if (!canUserAdd)
            {
                // Todo: Show that no family or family members are available to add kanban for.
                return PartialView("_AccessDeniedPartial");
            }

            CalendarItem editedEvent = model.CreateCalendarItem();

            model.CalendarItem = await calendarsHttpClient.AddCalendarItem(editedEvent);
            if (!model.CalendarItem.StartTime.HasValue || !model.CalendarItem.EndTime.HasValue) return PartialView("_EventCopiedPartial", model);

            model.CalendarItem.StartTime = TimeZoneInfo.ConvertTimeFromUtc(model.CalendarItem.StartTime.Value, TimeZoneInfo.FindSystemTimeZoneById(model.CurrentUser.Timezone));
            model.CalendarItem.EndTime = TimeZoneInfo.ConvertTimeFromUtc(model.CalendarItem.EndTime.Value, TimeZoneInfo.FindSystemTimeZoneById(model.CurrentUser.Timezone));
            model.SetReminderOffsetList(await viewModelSetupService.CreateReminderOffsetSelectListItems(model.LanguageId));

            if (model.CalendarItem.ProgenyId > 0)
            {
                model.CalendarItem.Progeny = await progenyHttpClient.GetProgeny(model.CalendarItem.ProgenyId);
            }
            if (model.CalendarItem.FamilyId > 0)
            {
                model.CalendarItem.Family = await familiesHttpClient.GetFamily(model.CalendarItem.FamilyId);
            }

            return PartialView("_EventCopiedPartial", model);
        }

        /// <summary>
        /// HttpPost action to get a list of upcoming CalendarItems and TodoItems.
        /// </summary>
        /// <param name="parameters">TimeLineParameters object.</param>
        /// <returns>Json of TimelineList object.</returns>
        [AllowAnonymous]
        [HttpPost]
        public async Task<IActionResult> GetUpcomingEventsList([FromBody] TimelineParameters parameters)
        {
            TimelineList timelineList = new();
            CalendarItemsRequest calendarItemsRequest = new()
            {
                ProgenyIds = parameters.Progenies,
                FamilyIds = parameters.Families,
                StartDate = DateTime.UtcNow.Date,
                EndDate = DateTime.UtcNow.Date.AddYears(10) // ToDo: Make this configurable
            };
            
            List<CalendarItem> upcomingCalendarItems = await calendarsHttpClient.GetProgeniesCalendarList(calendarItemsRequest);

            upcomingCalendarItems = [.. upcomingCalendarItems.Where(c => c.EndTime > DateTime.UtcNow)];
            upcomingCalendarItems = [.. upcomingCalendarItems.OrderBy(c => c.StartTime)];

            TodoItemsRequest todoItemsRequest = new()
            {
                ProgenyIds = parameters.Progenies,
                FamilyIds = parameters.Families,
                StartDate = DateTime.UtcNow.Date,
                EndDate = DateTime.UtcNow.Date.AddYears(10), // ToDo: Make this configurable
                SortBy = 0,
                GroupBy = 0,
                NumberOfItems = 0
            };

            TodoItemsResponse upcomingTodoItemsResponse = await todoItemsHttpClient.GetProgeniesTodoItemsList(todoItemsRequest);
            List<TodoItem> upcomingTodoItems = [.. upcomingTodoItemsResponse.TodoItems.Where(t => t.Status < (int)KinaUnaTypes.TodoStatusType.Completed)];
            upcomingTodoItems = [.. upcomingTodoItems.OrderBy(t => t.StartDate)];

            timelineList.AllItemsCount = upcomingCalendarItems.Count + upcomingTodoItems.Count;
            timelineList.RemainingItemsCount = timelineList.AllItemsCount - parameters.Skip - parameters.Count;
            
            foreach (CalendarItem eventItem in upcomingCalendarItems)
            {
                TimeLineItem eventTimelineItem = new()
                {
                    ProgenyId = eventItem.ProgenyId,
                    FamilyId = eventItem.FamilyId,
                    ItemId = eventItem.EventId.ToString(),
                    ItemType = (int)KinaUnaTypes.TimeLineType.Calendar,
                    ItemYear = eventItem.StartTime?.Year ?? DateTime.UtcNow.Year,
                    ItemMonth = eventItem.StartTime?.Month ?? DateTime.UtcNow.Month,
                    ItemDay = eventItem.StartTime?.Day ?? DateTime.UtcNow.Day,
                    ProgenyTime = eventItem.StartTime ?? DateTime.UtcNow,
                };
                timelineList.TimelineItems.Add(eventTimelineItem);
            }

            foreach (TodoItem todoItem in upcomingTodoItems)
            {
                if (!todoItem.StartDate.HasValue)
                {
                    todoItem.StartDate = DateTime.UtcNow + TimeSpan.FromDays(7); // Default to 7 days from now if no start date is set.
                }
                TimeLineItem todoTimelineItem = new()
                {
                    ProgenyId = todoItem.ProgenyId,
                    FamilyId = todoItem.FamilyId,
                    ItemId = todoItem.TodoItemId.ToString(),
                    ItemType = (int)KinaUnaTypes.TimeLineType.TodoItem,
                    ItemYear = todoItem.StartDate?.Year ?? DateTime.UtcNow.Year,
                    ItemMonth = todoItem.StartDate?.Month ?? DateTime.UtcNow.Month,
                    ItemDay = todoItem.StartDate?.Day ?? DateTime.UtcNow.Day,
                    ProgenyTime = todoItem.StartDate ?? DateTime.UtcNow,
                };
                timelineList.TimelineItems.Add(todoTimelineItem);
            }

            timelineList.TimelineItems = [.. timelineList.TimelineItems.OrderBy(t => t.ProgenyTime)];
            timelineList.TimelineItems = [.. timelineList.TimelineItems.Skip(parameters.Skip).Take(parameters.Count)];

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

            CalendarReminder calendarReminder = new()
            {
                EventId = calendarReminderRequest.EventId,
                UserId = currentUser.UserId
            };
            
            CalendarItem calendarItem = await calendarsHttpClient.GetCalendarItem(calendarReminderRequest.EventId);
            if (calendarItem == null || calendarItem.EventId == 0)
            {
                return BadRequest();
            }

            
            if (calendarReminderRequest.NotifyTimeOffsetType != 0 && calendarItem.StartTime.HasValue)
            {
                calendarReminder.NotifyTime = calendarItem.StartTime.Value.AddMinutes(-calendarReminderRequest.NotifyTimeOffsetType);
                calendarReminder.NotifyTimeOffsetType = calendarReminderRequest.NotifyTimeOffsetType;
            }
            else
            {
                bool notifyTimeParsed = DateTime.TryParseExact(calendarReminderRequest.NotifyTimeString, "dd/MM/yyyy HH:mm", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime notifyTime);
                if (!notifyTimeParsed)
                {
                    return BadRequest();
                }

                calendarReminder.NotifyTime = notifyTime;
                calendarReminder.NotifyTime = TimeZoneInfo.ConvertTimeToUtc(calendarReminder.NotifyTime, TimeZoneInfo.FindSystemTimeZoneById(currentUser.Timezone));
            }
            
            calendarReminder.RecurrenceRuleId = calendarItem.RecurrenceRuleId;
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
            if (existingCalendarReminder == null || existingCalendarReminder.CalendarReminderId == 0)
            {
                return NotFound();
            }

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