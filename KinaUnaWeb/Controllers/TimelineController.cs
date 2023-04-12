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

namespace KinaUnaWeb.Controllers
{
    public class TimelineController : Controller
    {
        private readonly ITimelineHttpClient _timelineHttpClient;
        private readonly ITimeLineItemsService _timeLineItemsService;
        private readonly IViewModelSetupService _viewModelSetupService;
        private readonly IProgenyHttpClient _progenyHttpClient;
        public TimelineController(ITimelineHttpClient timelineHttpClient, ITimeLineItemsService timeLineItemsService, IViewModelSetupService viewModelSetupService, IProgenyHttpClient progenyHttpClient)
        {
            _timelineHttpClient = timelineHttpClient;
            _timeLineItemsService = timeLineItemsService;
            _viewModelSetupService = viewModelSetupService;
            _progenyHttpClient = progenyHttpClient;
        }

        [AllowAnonymous]
        public async Task<IActionResult> Index(int childId = 0, int sortBy = 1, int items = 10)
        {
            BaseItemsViewModel baseModel = await _viewModelSetupService.SetupViewModel(Request.GetLanguageIdFromCookie(), User.GetEmail(), childId);
            TimeLineViewModel model = new(baseModel)
            {
                SortBy = sortBy,
                Items = items
            };
            
            return View(model);
        }

        [AllowAnonymous]
        [HttpPost]
        public async Task<IActionResult> GetTimelineList([FromBody] TimelineParameters parameters)
        {
            TimelineList timelineList = new();
            timelineList.TimelineItems = await _timelineHttpClient.GetTimeline(parameters.ProgenyId, 0, parameters.SortBy);
            timelineList.AllItemsCount = timelineList.TimelineItems.Count;
            timelineList.RemainingItemsCount = timelineList.TimelineItems.Count - parameters.Skip - parameters.Count; 
            timelineList.TimelineItems = timelineList.TimelineItems.Skip(parameters.Skip).Take(parameters.Count).ToList();

            return Json(timelineList);

        }

        [AllowAnonymous]
        [HttpPost]
        public async Task<IActionResult> GetYearAgoList([FromBody] TimelineParameters parameters)
        {
            TimelineList timelineList = new();
            timelineList.TimelineItems = await _progenyHttpClient.GetProgenyYearAgo(parameters.ProgenyId, 0);
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
            BaseItemsViewModel baseModel = await _viewModelSetupService.SetupViewModel(Request.GetLanguageIdFromCookie(), User.GetEmail(), 0);
            model.SetBaseProperties(baseModel);
            
            TimeLineItemPartialViewModel timeLineItemPartialViewModel = await _timeLineItemsService.GetTimeLineItemPartialViewModel(model);
            return PartialView(timeLineItemPartialViewModel.PartialViewName, timeLineItemPartialViewModel.TimeLineItem);
        }

        [AllowAnonymous]
        [HttpPost]
        public async Task<ActionResult> GetTimelineItemElement([FromBody] TimeLineItemViewModel model)
        {
            BaseItemsViewModel baseModel = await _viewModelSetupService.SetupViewModel(Request.GetLanguageIdFromCookie(), User.GetEmail(), 0);
            model.SetBaseProperties(baseModel);

            TimeLineItemPartialViewModel timeLineItemPartialViewModel = await _timeLineItemsService.GetTimeLineItemPartialViewModel(model);
            return PartialView(timeLineItemPartialViewModel.PartialViewName, timeLineItemPartialViewModel.TimeLineItem);
        }
    }
}