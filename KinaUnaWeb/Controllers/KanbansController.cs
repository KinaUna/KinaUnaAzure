using KinaUna.Data.Extensions;
using KinaUna.Data.Models.DTOs;
using KinaUnaWeb.Models;
using KinaUnaWeb.Models.ItemViewModels;
using KinaUnaWeb.Models.Kanbans;
using KinaUnaWeb.Models.TypeScriptModels.Kanbans;
using KinaUnaWeb.Services;
using KinaUnaWeb.Services.HttpClients;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace KinaUnaWeb.Controllers
{
    public class KanbansController(IViewModelSetupService viewModelSetupService,
        IKanbanBoardsHttpClient kanbanBoardsHttpClient,
        IUserInfosHttpClient userInfosHttpClient,
        IProgenyHttpClient progenyHttpClient) : Controller
    {
        [AllowAnonymous]
        public async Task<IActionResult> Index(int? kanbanBoardId, int childId = 0)
        {
            BaseItemsViewModel baseModel = await viewModelSetupService.SetupViewModel(Request.GetLanguageIdFromCookie(), User.GetEmail(), childId);
            KanbanBoardsListViewModel model = new(baseModel)
            {
                PopUpKanbanBoardId = kanbanBoardId ?? 0
            };

            if (model.PopUpKanbanBoardId != 0)
            {
                model.KanbanBoardsList.Add(await kanbanBoardsHttpClient.GetKanbanBoard(model.PopUpKanbanBoardId));
            }

            return View(model);
        }

        [AllowAnonymous]
        [HttpPost]
        public async Task<IActionResult> GetKanbanBoardsList([FromBody] KanbanBoardsPageParameters pageParameters)
        {
            if (pageParameters.LanguageId == 0)
            {
                pageParameters.LanguageId = Request.GetLanguageIdFromCookie();
            }

            if (pageParameters.CurrentPageNumber < 1)
            {
                pageParameters.CurrentPageNumber = 1;
            }

            if (pageParameters.ItemsPerPage < 1)
            {
                pageParameters.ItemsPerPage = 10;
            }

            KanbanBoardsRequest kanbanBoardsRequest = new()
            {
                ProgenyIds = pageParameters.Progenies,
                Skip = (pageParameters.CurrentPageNumber - 1) * pageParameters.ItemsPerPage,
                NumberOfItems = pageParameters.ItemsPerPage,
                TagFilter = pageParameters.TagFilter,
                ContextFilter = pageParameters.ContextFilter,
            };

            KanbanBoardsResponse kanbanBoardsResponse = await kanbanBoardsHttpClient.GetProgeniesKanbanBoardsList(kanbanBoardsRequest);

            return Json(kanbanBoardsResponse);

        }

        [AllowAnonymous]
        public async Task<IActionResult> GetKanbanBoard(int kanbanBoardId)
        {
            KanbanBoard kanbanBoard = await kanbanBoardsHttpClient.GetKanbanBoard(kanbanBoardId);

            BaseItemsViewModel baseModel = await viewModelSetupService.SetupViewModel(1, User.GetEmail(), kanbanBoard.ProgenyId);
            if (baseModel.CurrentAccessLevel > kanbanBoard.AccessLevel)
            {
                return Forbid();
            }

            return Json(kanbanBoard);
        }

        [AllowAnonymous]
        [HttpPost]
        public async Task<IActionResult> KanbanBoardElement([FromBody] KanbanBoardElementParameters elementParameters)
        {
            if (elementParameters.LanguageId == 0)
            {
                elementParameters.LanguageId = Request.GetLanguageIdFromCookie();
            }

            if (elementParameters.KanbanBoardId == 0)
            {
                return PartialView("_KanbanBoardElementPartial", new KanbanBoardElementResponse()
                {
                    IsCurrentUserProgenyAdmin = false,
                    KanbanBoard = new KanbanBoard(),
                    LanguageId = elementParameters.LanguageId
                });
            }

            KanbanBoard kanbanBoard = await kanbanBoardsHttpClient.GetKanbanBoard(elementParameters.KanbanBoardId);

            BaseItemsViewModel baseModel = await viewModelSetupService.SetupViewModel(elementParameters.LanguageId, User.GetEmail(), kanbanBoard.ProgenyId);
            KanbanBoardElementResponse response = new()
            {
                KanbanBoardId = kanbanBoard.KanbanBoardId,
                LanguageId = elementParameters.LanguageId,
                IsCurrentUserProgenyAdmin = baseModel.IsCurrentUserProgenyAdmin,
                KanbanBoard = kanbanBoard
            };

            return PartialView("_KanbanBoardElementPartial", response);
        }

        [AllowAnonymous]
        public async Task<IActionResult> ViewKanbanBoard(int kanbanBoardId, bool partialView = false)
        {
            KanbanBoard kanbanBoard = await kanbanBoardsHttpClient.GetKanbanBoard(kanbanBoardId);

            BaseItemsViewModel baseModel = await viewModelSetupService.SetupViewModel(Request.GetLanguageIdFromCookie(), User.GetEmail(), kanbanBoard.ProgenyId);
            KanbanBoardViewModel model = new(baseModel)
            {
                KanbanBoard = kanbanBoard
            };

            model.KanbanBoard.Progeny = model.CurrentProgeny;
            model.KanbanBoard.Progeny.PictureLink = model.KanbanBoard.Progeny.GetProfilePictureUrl();
            UserInfo kanbanBoardUserInfo = await userInfosHttpClient.GetUserInfoByUserId(model.KanbanBoard.CreatedBy);
            model.KanbanBoard.CreatedBy = kanbanBoardUserInfo.FullName();

            if (partialView)
            {
                return PartialView("_ViewKanbanBoardPartial", model);
            }

            return View(model);
        }

        public async Task<IActionResult> AddKanbanBoard()
        {
            BaseItemsViewModel baseModel = await viewModelSetupService.SetupViewModel(Request.GetLanguageIdFromCookie(), User.GetEmail(), 0);
            KanbanBoardViewModel model = new(baseModel);
            if (model.CurrentUser == null)
            {
                return PartialView("_NotFoundPartial");
            }

            if (User.Identity != null && User.Identity.IsAuthenticated && model.CurrentUser.UserId != null)
            {
                model.ProgenyList = await viewModelSetupService.GetProgenySelectList(model.CurrentUser);
                model.SetProgenyList();
            }

            model.KanbanBoard.CreatedTime = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, TimeZoneInfo.FindSystemTimeZoneById(model.CurrentUser.Timezone));

            model.SetAccessLevelList();

            return PartialView("_AddKanbanBoardPartial", model);
        }

        [HttpPost]
        public async Task<IActionResult> AddKanbanBoard([FromBody] KanbanBoardViewModel model)
        {
            BaseItemsViewModel baseModel = await viewModelSetupService.SetupViewModel(Request.GetLanguageIdFromCookie(), User.GetEmail(), model.KanbanBoard.ProgenyId);
            model.SetBaseProperties(baseModel);

            List<Progeny> progAdminList = await progenyHttpClient.GetProgenyAdminList(model.CurrentUser.UserEmail);
            if (progAdminList.Count == 0)
            {
                // Todo: Show that no children are available to add kanban for.
                return RedirectToAction("Index");
            }

            KanbanBoard kanbanBoard = model.CreateKanbanBoard();
            KanbanBoard addedKanbanBoard = await kanbanBoardsHttpClient.AddKanbanBoard(kanbanBoard);

            return Json(addedKanbanBoard);
        }
    }
}
