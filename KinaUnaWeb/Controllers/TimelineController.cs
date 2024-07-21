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

namespace KinaUnaWeb.Controllers
{
    public class TimelineController(ITimelineHttpClient timelineHttpClient, ITimeLineItemsService timeLineItemsService, IViewModelSetupService viewModelSetupService, IProgenyHttpClient progenyHttpClient)
        : Controller
    {
        [AllowAnonymous]
        public async Task<IActionResult> Index(int childId = 0, int sortBy = 1, int items = 10, int skip = 0, int year=0, int month=0, int day=0)
        {
            BaseItemsViewModel baseModel = await viewModelSetupService.SetupViewModel(Request.GetLanguageIdFromCookie(), User.GetEmail(), childId);
            TimeLineViewModel model = new(baseModel)
            {
                SortBy = sortBy,
                Items = items,
                Skip = skip,
                Year = year,
                Month = month,
                Day = day
            };

            if (year != 0)
            {
                model.SetParametersFromProperties();
                return View(model);
            }
            
            if (sortBy == 1)
            {
                model.Year = DateTime.Now.Year;
                model.Month = DateTime.Now.Month;
                model.Day = DateTime.Now.Day;
            }
            else
            {
                model.Year = 1900;
                model.Month = 1;
                model.Day = 1;
            }

            model.SetParametersFromProperties();

            return View(model);
        }

        [AllowAnonymous]
        [HttpPost]
        public async Task<IActionResult> GetTimelineList([FromBody] TimelineParameters parameters)
        {
            BaseItemsViewModel baseModel = await viewModelSetupService.SetupViewModel(Request.GetLanguageIdFromCookie(), User.GetEmail(), parameters.ProgenyId);

            TimelineList timelineList = new()
            {
                TimelineItems = await timelineHttpClient.GetTimeline(baseModel.CurrentProgenyId, baseModel.CurrentAccessLevel, parameters.SortBy)
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

        [AllowAnonymous]
        [HttpPost]
        public async Task<IActionResult> GetYearAgoList([FromBody] TimelineParameters parameters)
        {
            TimelineList timelineList = new()
            {
                TimelineItems = await progenyHttpClient.GetProgenyYearAgo(parameters.ProgenyId, 0)
            };
            timelineList.AllItemsCount = timelineList.TimelineItems.Count;
            timelineList.RemainingItemsCount = timelineList.TimelineItems.Count - parameters.Skip - parameters.Count;
            timelineList.TimelineItems = timelineList.TimelineItems.Skip(parameters.Skip).Take(parameters.Count).ToList();

            return Json(timelineList);

        }

        [AllowAnonymous]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult TimeLinePhotoPartial(PictureViewModel model)
        {
            return PartialView("_TimeLinePhotoPartial", model);
        }

        [AllowAnonymous]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult TimeLineVideoPartial(VideoViewModel model)
        {
            return PartialView("_TimeLineVideoPartial", model);
        }

        [AllowAnonymous]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult TimeLineEventPartial(CalendarItem model)
        {
            return PartialView("_TimeLineEventPartial", model);
        }

        [AllowAnonymous]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult TimeLineVocabularyPartial(VocabularyItem model)
        {
            return PartialView("_TimeLineVocabularyPartial", model);
        }

        [AllowAnonymous]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult TimeLineSkillPartial(Skill model)
        {
            return PartialView("_TimeLineSkillPartial", model);
        }

        [AllowAnonymous]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult TimeLineFriendPartial(Friend model)
        {
            return PartialView("_TimeLineFriendPartial", model);
        }

        [AllowAnonymous]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult TimeLineMeasurementPartial(Measurement model)
        {
            return PartialView("_TimeLineMeasurementPartial", model);
        }

        [AllowAnonymous]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult TimeLineSleepPartial(Sleep model)
        {
            return PartialView("_TimeLineSleepPartial", model);
        }

        [AllowAnonymous]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult TimeLineNotePartial(Note model)
        {
            return PartialView("_TimeLineNotePartial", model);
        }

        [AllowAnonymous]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult TimeLineContactPartial(Contact model)
        {
            return PartialView("_TimeLineContactPartial", model);
        }

        [AllowAnonymous]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult TimeLineVaccinationPartial(Vaccination model)
        {
            return PartialView("_TimeLineVaccinationPartial", model);
        }

        [AllowAnonymous]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult TimeLineLocationPartial(Location model)
        {
            return PartialView("_TimeLineLocationPartial", model);
        }

        [AllowAnonymous]
        public async Task<ActionResult> GetTimeLineItem(TimeLineItemViewModel model)
        {
            BaseItemsViewModel baseModel = await viewModelSetupService.SetupViewModel(Request.GetLanguageIdFromCookie(), User.GetEmail(), 0);
            model.SetBaseProperties(baseModel);
            
            TimeLineItemPartialViewModel timeLineItemPartialViewModel = await timeLineItemsService.GetTimeLineItemPartialViewModel(model);
            return PartialView(timeLineItemPartialViewModel.PartialViewName, timeLineItemPartialViewModel.TimeLineItem);
        }

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