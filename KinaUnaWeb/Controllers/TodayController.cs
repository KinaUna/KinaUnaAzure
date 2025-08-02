using KinaUnaWeb.Models.ItemViewModels;
using KinaUnaWeb.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using KinaUna.Data.Extensions;
using KinaUna.Data.Models.DTOs;
using KinaUnaWeb.Models;
using KinaUnaWeb.Services.HttpClients;

namespace KinaUnaWeb.Controllers
{
    public class TodayController(ITimelineHttpClient timelineHttpClient, IViewModelSetupService viewModelSetupService)
        : Controller
    {
        /// <summary>
        /// Page for showing TimeLineItems for a day, and the same day in previous years, month, quarters, or weeks.
        /// </summary>
        /// <param name="progenyId">The Id of the Progeny to show TimeLineItems for.</param>
        /// <param name="items">Number of items to get.</param>
        /// <param name="skip">Number of items to skip.</param>
        /// <param name="year">Integer for the start date year.</param>
        /// <param name="month">Integer for the start date month.</param>
        /// <param name="day">Integer for the start date day.</param>
        /// <param name="period">Filter the results by TimeLineItem type.</param>
        /// <param name="tagFilter"></param>
        /// <param name="categoryFilter"></param>
        /// <param name="contextFilter"></param>
        /// <param name="sortOrder"></param>
        /// <returns></returns>
        [AllowAnonymous]
        public async Task<IActionResult> OnThisDay(int progenyId = 0, int items = 10, int skip = 0, int year = 0, int month = 0, int day = 0, int period = 4, string tagFilter = "", string categoryFilter = "", string contextFilter = "", int sortOrder = 1)
        {
            BaseItemsViewModel baseModel = await viewModelSetupService.SetupViewModel(Request.GetLanguageIdFromCookie(), User.GetEmail(), progenyId);
            OnThisDayViewModel model = new(baseModel);
            model.SetRequestParameters(skip, items, (OnThisDayPeriod)period, year, month, day, tagFilter, categoryFilter, contextFilter, sortOrder);

            return View(model);
        }

        /// <summary>
        /// HttpPost method for fetching an OnThisDayResponse, with a list of TimeLineItems for specific days according to the period in the parameters .
        /// </summary>
        /// <param name="parameters">OnThisDayRequest object, with the parameters for getting the list of TimeLineItems.</param>
        /// <returns>Json of OnThisDayResponse object.</returns>
        [AllowAnonymous]
        [HttpPost]
        public async Task<IActionResult> GetOnThisDayList([FromBody] OnThisDayRequest parameters)
        {
            BaseItemsViewModel baseModel = await viewModelSetupService.SetupViewModel(Request.GetLanguageIdFromCookie(), User.GetEmail(), parameters.ProgenyId);
            OnThisDayViewModel model = new(baseModel)
            {
                OnThisDayRequest = parameters
            };
            model.OnThisDayRequest.AccessLevel = baseModel.CurrentAccessLevel;

            return Json(await timelineHttpClient.GetOnThisDayTimeLineItems(model.OnThisDayRequest));
        }
    }
}