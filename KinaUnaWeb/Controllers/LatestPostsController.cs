using KinaUnaWeb.Models.ItemViewModels;
using KinaUnaWeb.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using KinaUna.Data.Extensions;
using KinaUna.Data.Models;

namespace KinaUnaWeb.Controllers
{
    public class LatestPostsController : Controller
    {
        private readonly IUserInfosHttpClient _userInfosHttpClient;
        private readonly ITimeLineItemsService _timeLineItemsService;
        
        public LatestPostsController(IUserInfosHttpClient userInfosHttpClient, ITimeLineItemsService timeLineItemsService)
        {
            _userInfosHttpClient = userInfosHttpClient;
            _timeLineItemsService = timeLineItemsService;
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
            model.CurrentUser = await _userInfosHttpClient.GetUserInfo(User.GetEmail());
            TimeLineItemPartialViewModel timeLineItemPartialViewModel = await _timeLineItemsService.GetTimeLineItemPartialViewModel(model);
            return PartialView(timeLineItemPartialViewModel.PartialViewName, timeLineItemPartialViewModel.TimeLineItem);
        }
    }
}