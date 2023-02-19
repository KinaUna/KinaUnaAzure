using KinaUnaWeb.Models.ItemViewModels;
using KinaUnaWeb.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Linq;
using System.Threading.Tasks;
using KinaUna.Data.Extensions;
using KinaUna.Data.Models;
using KinaUnaWeb.Models;

namespace KinaUnaWeb.Controllers
{
    public class TimelineController : Controller
    {
        private readonly IUserInfosHttpClient _userInfosHttpClient;
        private readonly ITimelineHttpClient _timelineHttpClient;
        private readonly ITimeLineItemsService _timeLineItemsService;
        private readonly IViewModelSetupService _viewModelSetupService;

        public TimelineController(IUserInfosHttpClient userInfosHttpClient, ITimelineHttpClient timelineHttpClient, ITimeLineItemsService timeLineItemsService, IViewModelSetupService viewModelSetupService)
        {
            _userInfosHttpClient = userInfosHttpClient;
            _timelineHttpClient = timelineHttpClient;
            _timeLineItemsService = timeLineItemsService;
            _viewModelSetupService = viewModelSetupService;
        }

        [AllowAnonymous]
        public async Task<IActionResult> Index(int childId = 0, int sortBy = 1, int items = 0)
        {
            BaseItemsViewModel baseModel = await _viewModelSetupService.SetupViewModel(Request.GetLanguageIdFromCookie(), User.GetEmail(), childId);
            TimeLineViewModel model = new TimeLineViewModel(baseModel);
            
            model.SortBy = sortBy;
            
            model.TimeLineItems = await _timelineHttpClient.GetTimeline(childId, model.CurrentAccessLevel, sortBy);
            
            model.Items = items;
            
            return View(model);
        }

        [AllowAnonymous]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult TimeLinePhotoPartial(PictureViewModel model)
        {

            return View(model);
        }

        [AllowAnonymous]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult TimeLineVideoPartial(VideoViewModel model)
        {

            return View(model);
        }

        [AllowAnonymous]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult TimeLineEventPartial(CalendarItem model)
        {

            return View(model);
        }

        [AllowAnonymous]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult TimeLineVocabularyPartial(VocabularyItem model)
        {

            return View(model);
        }

        [AllowAnonymous]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult TimeLineSkillPartial(Skill model)
        {

            return View(model);
        }

        [AllowAnonymous]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult TimeLineFriendPartial(Friend model)
        {

            return View(model);
        }

        [AllowAnonymous]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult TimeLineMeasurementPartial(Measurement model)
        {

            return View(model);
        }

        [AllowAnonymous]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult TimeLineSleepPartial(Sleep model)
        {

            return View(model);
        }

        [AllowAnonymous]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult TimeLineNotePartial(Note model)
        {

            return View(model);
        }

        [AllowAnonymous]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult TimeLineContactPartial(Contact model)
        {

            return View(model);
        }

        [AllowAnonymous]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult TimeLineVaccinationPartial(Vaccination model)
        {

            return View(model);
        }

        [AllowAnonymous]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult TimeLineLocationPartial(Location model)
        {

            return View(model);
        }

        [AllowAnonymous]
        public async Task<ActionResult> GetTimeLineItem(TimeLineItemViewModel model)
        {
            model.LanguageId = Request.GetLanguageIdFromCookie();
            model.CurrentUser = await _userInfosHttpClient.GetUserInfo(User.GetEmail());
            
            TimeLineItemPartialViewModel timeLineItemPartialViewModel = await _timeLineItemsService.GetTimeLineItemPartialViewModel(model);
            return PartialView(timeLineItemPartialViewModel.PartialViewName, timeLineItemPartialViewModel.TimeLineItem);
        }
    }
}