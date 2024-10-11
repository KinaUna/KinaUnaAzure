using System;
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
using KinaUna.Data.Models.DTOs;

namespace KinaUnaWeb.Controllers
{
    public class TimelineController(ITimelineHttpClient timelineHttpClient, ITimeLineItemsService timeLineItemsService, IViewModelSetupService viewModelSetupService, IProgenyHttpClient progenyHttpClient)
        : Controller
    {
        /// <summary>
        /// Page for showing the Timeline.
        /// </summary>
        /// <param name="childId">The Id of the Progeny to show the timeline for.</param>
        /// <param name="sortOrder">Sort order. 0 = oldest first, 1 = newest first.</param>
        /// <param name="items">Number of TimeLineItems to get.</param>
        /// <param name="skip">Number of TimeLineItems to skip.</param>
        /// <param name="year">Start year.</param>
        /// <param name="month">Start month.</param>
        /// <param name="day">Start day.</param>
        /// <param name="tagFilter">Filter by tag.</param>
        /// <param name="categoryFilter">Filter by category.</param>
        /// <param name="contextFilter">Filter by context.</param>
        /// <returns>View with TimeLineViewModel.</returns>
        [AllowAnonymous]
        public async Task<IActionResult> Index(int childId = 0, int sortOrder = 1, int items = 10, int skip = 0, int year=0, int month=0, int day=0, string tagFilter = "", string categoryFilter = "", string contextFilter = "")
        {
            BaseItemsViewModel baseModel = await viewModelSetupService.SetupViewModel(Request.GetLanguageIdFromCookie(), User.GetEmail(), childId);

            TimelineRequestViewModel model = new(baseModel);
            model.SetRequestParameters(skip, items, year, month, day, tagFilter, categoryFilter, contextFilter, sortOrder);
            
            return View(model);
        }

        /// <summary>
        /// HttpPost method for fetching a TimelineResponse, with a list of TimeLineItems.
        /// </summary>
        /// <param name="parameters">TimelineRequest object, with the parameters for getting the list of TimeLineItems.</param>
        /// <returns>Json of TimelineResponse object.</returns>
        [AllowAnonymous]
        [HttpPost]
        public async Task<IActionResult> GetTimelineData([FromBody] TimelineRequest parameters)
        {
            BaseItemsViewModel baseModel = await viewModelSetupService.SetupViewModel(Request.GetLanguageIdFromCookie(), User.GetEmail(), parameters.ProgenyId);
            TimelineRequestViewModel model = new(baseModel)
            {
                TimelineRequest = parameters
            };

            model.TimelineRequest.AccessLevel = baseModel.CurrentAccessLevel;

            return Json(await timelineHttpClient.GetTimeLineData(model.TimelineRequest));
        }

        /// <summary>
        /// HttpPost method for fetching a list of TimeLineItems.
        /// </summary>
        /// <param name="parameters">TimeLineParameters object.</param>
        /// <returns>Json of TimelineList object.</returns>
        [AllowAnonymous]
        [HttpPost]
        public async Task<IActionResult> GetTimelineList([FromBody] TimelineParameters parameters)
        {
            TimelineList timelineList = new()
            {
                TimelineItems = await timelineHttpClient.GetProgeniesTimeline(parameters.Progenies, parameters.SortBy)
            };

            if (timelineList.TimelineItems.Count > 0)
            {
                timelineList.FirstItemYear = timelineList.TimelineItems.Min(t => t.ProgenyTime).Year;
                if (parameters.Year != 0)
                {
                    DateTime startDate = new(parameters.Year, parameters.Month, parameters.Day, 23, 59, 59);
                    if (parameters.SortBy == 1)
                    {

                        timelineList.TimelineItems = timelineList.TimelineItems.Where(t => t.ProgenyTime <= startDate).ToList();
                    }
                    else
                    {
                        startDate = new(parameters.Year, parameters.Month, parameters.Day, 0, 0, 0);
                        timelineList.TimelineItems = timelineList.TimelineItems.Where(t => t.ProgenyTime >= startDate).ToList();
                    }
                }

                timelineList.AllItemsCount = timelineList.TimelineItems.Count;
                timelineList.RemainingItemsCount = timelineList.TimelineItems.Count - parameters.Skip - parameters.Count;
                timelineList.TimelineItems = timelineList.TimelineItems.Skip(parameters.Skip).Take(parameters.Count).ToList();
            }
            else
            {
                timelineList.FirstItemYear = DateTime.Now.Year;
                timelineList.AllItemsCount = 0;
                timelineList.RemainingItemsCount = 0;
            }

            return Json(timelineList);

        }

        /// <summary>
        /// HttpPost method for fetching a list of TimeLineItems from this day in previous years.
        /// </summary>
        /// <param name="parameters">TimeLineParameters object.</param>
        /// <returns>Json of TimelineList object.</returns>
        [AllowAnonymous]
        [HttpPost]
        public async Task<IActionResult> GetYearAgoList([FromBody] TimelineParameters parameters)
        {
            TimelineList timelineList = new()
            {
                TimelineItems = await progenyHttpClient.GetProgeniesYearAgo(parameters.Progenies)
            };
            timelineList.AllItemsCount = timelineList.TimelineItems.Count;
            timelineList.RemainingItemsCount = timelineList.TimelineItems.Count - parameters.Skip - parameters.Count;
            timelineList.TimelineItems = timelineList.TimelineItems.Skip(parameters.Skip).Take(parameters.Count).ToList();

            return Json(timelineList);

        }

        /// <summary>
        /// HttpPost method for fetching HTML for a TimeLineItem with a Picture.
        /// </summary>
        /// <param name="model">PictureViewModel with the Picture properties.</param>
        /// <returns>PartialView with the PictureViewModel.</returns>
        [AllowAnonymous]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult TimeLinePhotoPartial(PictureViewModel model)
        {
            return PartialView("_TimeLinePhotoPartial", model);
        }

        /// <summary>
        /// HttpPost method for fetching HTML for a TimeLineItem with a Video.
        /// </summary>
        /// <param name="model">VideoViewModel with the Video properties.</param>
        /// <returns>PartialView with the VideoViewModel.</returns>
        [AllowAnonymous]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult TimeLineVideoPartial(VideoViewModel model)
        {
            return PartialView("_TimeLineVideoPartial", model);
        }

        /// <summary>
        /// HttpPost method for fetching HTML for a TimeLineItem with a CalendarItem.
        /// </summary>
        /// <param name="model">CalendarItem with the event properties.</param>
        /// <returns>PartialView with the CalendarItem.</returns>
        [AllowAnonymous]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult TimeLineEventPartial(CalendarItem model)
        {
            return PartialView("_TimeLineEventPartial", model);
        }

        /// <summary>
        /// HttpPost method for fetching HTML for a TimeLineItem with a VocabularyItem.
        /// </summary>
        /// <param name="model">VocabularyItem.</param>
        /// <returns>PartialView with the VocabularyItem.</returns>
        [AllowAnonymous]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult TimeLineVocabularyPartial(VocabularyItem model)
        {
            return PartialView("_TimeLineVocabularyPartial", model);
        }

        /// <summary>
        /// HttpPost method for fetching HTML for a TimeLineItem with a Skill.
        /// </summary>
        /// <param name="model">Skill object with the Skill's properties.</param>
        /// <returns>PartialView with the Skill.</returns>
        [AllowAnonymous]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult TimeLineSkillPartial(Skill model)
        {
            return PartialView("_TimeLineSkillPartial", model);
        }

        /// <summary>
        /// HttpPost method for fetching HTML for a TimeLineItem with a Friend.
        /// </summary>
        /// <param name="model">Friend object with the Friend's properties.</param>
        /// <returns>PartialView with the Friend.</returns>
        [AllowAnonymous]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult TimeLineFriendPartial(Friend model)
        {
            return PartialView("_TimeLineFriendPartial", model);
        }

        /// <summary>
        /// HttpPost method for fetching HTML for a TimeLineItem with a Measurement.
        /// </summary>
        /// <param name="model">Measurement object with the Measurement's properties.</param>
        /// <returns>PartialView with the Measurement.</returns>
        [AllowAnonymous]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult TimeLineMeasurementPartial(Measurement model)
        {
            return PartialView("_TimeLineMeasurementPartial", model);
        }

        /// <summary>
        /// HttpPost method for fetching HTML for a TimeLineItem with a Sleep item.
        /// </summary>
        /// <param name="model">Sleep object with the Sleep item's properties.</param>
        /// <returns>PartialView with the Sleep object.</returns>
        [AllowAnonymous]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult TimeLineSleepPartial(Sleep model)
        {
            return PartialView("_TimeLineSleepPartial", model);
        }

        /// <summary>
        /// HttpPost method for fetching HTML for a TimeLineItem with a Note.
        /// </summary>
        /// <param name="model">Note object with the Note's properties.</param>
        /// <returns>PartialView with the Note.</returns>
        [AllowAnonymous]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult TimeLineNotePartial(Note model)
        {
            return PartialView("_TimeLineNotePartial", model);
        }

        /// <summary>
        /// HttpPost method for fetching HTML for a TimeLineItem with a Contact.
        /// </summary>
        /// <param name="model">Contact object with the Contact's properties.</param>
        /// <returns>PartialView with the Contact.</returns>
        [AllowAnonymous]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult TimeLineContactPartial(Contact model)
        {
            return PartialView("_TimeLineContactPartial", model);
        }

        /// <summary>
        /// HttpPost method for fetching HTML for a TimeLineItem with a Vaccination.
        /// </summary>
        /// <param name="model">Vaccination object with the Vaccination's properties.</param>
        /// <returns>PartialView with the Vaccination.</returns>
        [AllowAnonymous]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult TimeLineVaccinationPartial(Vaccination model)
        {
            return PartialView("_TimeLineVaccinationPartial", model);
        }

        /// <summary>
        /// HttpPost method for fetching HTML for a TimeLineItem with a Location.
        /// </summary>
        /// <param name="model">Location object with the Location's properties.</param>
        /// <returns>PartialView with the Location.</returns>
        [AllowAnonymous]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult TimeLineLocationPartial(Location model)
        {
            return PartialView("_TimeLineLocationPartial", model);
        }

        /// <summary>
        /// Get method for fetching HTML for a TimeLineItem.
        /// </summary>
        /// <param name="model">TimeLineItemViewModel</param>
        /// <returns>PartialView for the TimeLineItem type with the TimeLineItem as a model.</returns>
        [AllowAnonymous]
        public async Task<ActionResult> GetTimeLineItem(TimeLineItemViewModel model)
        {
            BaseItemsViewModel baseModel = await viewModelSetupService.SetupViewModel(Request.GetLanguageIdFromCookie(), User.GetEmail(), 0);
            model.SetBaseProperties(baseModel);
            
            TimeLineItemPartialViewModel timeLineItemPartialViewModel = await timeLineItemsService.GetTimeLineItemPartialViewModel(model);
            return PartialView(timeLineItemPartialViewModel.PartialViewName, timeLineItemPartialViewModel.TimeLineItem);
        }

        /// <summary>
        /// HttpPost method for fetching HTML for a TimeLineItem.
        /// </summary>
        /// <param name="model">TimeLineItemViewModel</param>
        /// <returns>PartialView for the TimeLineItem type with the TimeLineItem as a model.</returns>
        [AllowAnonymous]
        [HttpPost]
        public async Task<ActionResult> GetTimelineItemElement([FromBody] TimeLineItemViewModel model)
        {
            BaseItemsViewModel baseModel = await viewModelSetupService.SetupViewModel(Request.GetLanguageIdFromCookie(), User.GetEmail(), 0);
            model.SetBaseProperties(baseModel);

            TimeLineItemPartialViewModel timeLineItemPartialViewModel = await timeLineItemsService.GetTimeLineItemPartialViewModel(model);
            return PartialView(timeLineItemPartialViewModel.PartialViewName, timeLineItemPartialViewModel.TimeLineItem);
        }
    }
}