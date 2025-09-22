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
    public class KanbansController(
        IViewModelSetupService viewModelSetupService,
        IKanbanBoardsHttpClient kanbanBoardsHttpClient,
        IUserInfosHttpClient userInfosHttpClient,
        IProgenyHttpClient progenyHttpClient,
        IKanbanItemsHttpClient kanbanItemsHttpClient,
        ITodoItemsHttpClient todoItemsHttpClient) : Controller
    {
        [AllowAnonymous]
        [HttpGet]
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
        [HttpGet]
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
            kanbanBoard.Progeny = await progenyHttpClient.GetProgeny(kanbanBoard.ProgenyId);
            
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
        [HttpGet]
        public async Task<IActionResult> ViewKanbanBoard(int kanbanBoardId, bool partialView = false)
        {
            KanbanBoard kanbanBoard = await kanbanBoardsHttpClient.GetKanbanBoard(kanbanBoardId);

            BaseItemsViewModel baseModel = await viewModelSetupService.SetupViewModel(Request.GetLanguageIdFromCookie(), User.GetEmail(), kanbanBoard.ProgenyId);
            KanbanBoardViewModel model = new(baseModel)
            {
                KanbanBoard = kanbanBoard
            };

            model.KanbanBoard.Progeny = model.CurrentProgeny;
            UserInfo kanbanBoardUserInfo = await userInfosHttpClient.GetUserInfoByUserId(model.KanbanBoard.CreatedBy);
            model.KanbanBoard.CreatedBy = kanbanBoardUserInfo.FullName();

            if (partialView)
            {
                return PartialView("_ViewKanbanBoardPartial", model);
            }

            return View(model);
        }

        [HttpGet]
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
        public async Task<IActionResult> AddKanbanBoard([FromForm] KanbanBoardViewModel model)
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
            model.KanbanBoard = await kanbanBoardsHttpClient.AddKanbanBoard(kanbanBoard);

            return Json(model.KanbanBoard);
        }

        [HttpGet]
        public async Task<IActionResult> EditKanbanBoard(int kanbanBoardId)
        {
            KanbanBoard kanbanBoard = await kanbanBoardsHttpClient.GetKanbanBoard(kanbanBoardId);
            BaseItemsViewModel baseModel = await viewModelSetupService.SetupViewModel(Request.GetLanguageIdFromCookie(), User.GetEmail(), kanbanBoard.ProgenyId);

            KanbanBoardViewModel model = new(baseModel)
            {
                KanbanBoard = kanbanBoard
            };
            if (model.CurrentUser == null)
            {
                return PartialView("_NotFoundPartial");
            }

            if (!model.CurrentProgeny.IsInAdminList(model.CurrentUser.UserEmail))
            {
                return PartialView("_AccessDeniedPartial");
            }

            if (User.Identity != null && User.Identity.IsAuthenticated && model.CurrentUser.UserId != null)
            {
                model.ProgenyList = await viewModelSetupService.GetProgenySelectList(model.CurrentUser);
                model.SetProgenyList();
                model.KanbanBoard.Progeny = model.CurrentProgeny;
                model.KanbanBoard.Progeny.PictureLink = model.KanbanBoard.Progeny.GetProfilePictureUrl();
            }

            model.SetPropertiesFromKanbanBoard(kanbanBoard);

            model.SetAccessLevelList();

            return PartialView("_EditKanbanBoardPartial", model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditKanbanBoard([FromForm] KanbanBoardViewModel model)
        {
            BaseItemsViewModel baseModel = await viewModelSetupService.SetupViewModel(Request.GetLanguageIdFromCookie(), User.GetEmail(), model.KanbanBoard.ProgenyId);
            model.SetBaseProperties(baseModel);

            if (!model.CurrentProgeny.IsInAdminList(model.CurrentUser.UserEmail))
            {
                return PartialView("_AccessDeniedPartial");
            }

            KanbanBoard existingKanbanBoard = await kanbanBoardsHttpClient.GetKanbanBoard(model.KanbanBoard.KanbanBoardId);
            if (existingKanbanBoard == null)
            {
                return PartialView("_NotFoundPartial");
            }

            KanbanBoard kanbanBoard = model.CreateKanbanBoard();
            kanbanBoard.Columns = existingKanbanBoard.Columns;
            kanbanBoard.CreatedBy = existingKanbanBoard.CreatedBy;
            kanbanBoard.CreatedTime = existingKanbanBoard.CreatedTime;
            kanbanBoard.ModifiedBy = model.CurrentUser.UserId;
            kanbanBoard.ModifiedTime = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, TimeZoneInfo.FindSystemTimeZoneById(model.CurrentUser.Timezone));
            kanbanBoard.SetColumnsListFromColumns();

            model.KanbanBoard = await kanbanBoardsHttpClient.UpdateKanbanBoard(kanbanBoard);

            return Json(model.KanbanBoard);
        }

        [HttpPost]
        public async Task<IActionResult> UpdateKanbanBoardColumns([FromBody] KanbanBoard kanbanBoard)
        {
            BaseItemsViewModel baseModel = await viewModelSetupService.SetupViewModel(Request.GetLanguageIdFromCookie(), User.GetEmail(), kanbanBoard.ProgenyId);
            KanbanBoardViewModel model = new(baseModel);
            model.SetBaseProperties(baseModel);

            if (!model.CurrentProgeny.IsInAdminList(model.CurrentUser.UserEmail))
            {
                return PartialView("_AccessDeniedPartial");
            }
            KanbanBoard existingKanbanBoard = await kanbanBoardsHttpClient.GetKanbanBoard(kanbanBoard.KanbanBoardId);
            existingKanbanBoard.Columns = kanbanBoard.Columns;
            existingKanbanBoard.ModifiedBy = model.CurrentUser.UserId;
            existingKanbanBoard.ModifiedTime = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, TimeZoneInfo.FindSystemTimeZoneById(model.CurrentUser.Timezone));

            KanbanBoard updatedKanbanBoard = await kanbanBoardsHttpClient.UpdateKanbanBoard(existingKanbanBoard);

            return Json(updatedKanbanBoard);
        }

        [HttpGet]
        public async Task<IActionResult> DeleteKanbanBoard(int kanbanBoardId)
        {
            KanbanBoard kanbanBoard = await kanbanBoardsHttpClient.GetKanbanBoard(kanbanBoardId);
            BaseItemsViewModel baseModel = await viewModelSetupService.SetupViewModel(Request.GetLanguageIdFromCookie(), User.GetEmail(), kanbanBoard.ProgenyId);
            KanbanBoardViewModel model = new(baseModel)
            {
                KanbanBoard = kanbanBoard
            };

            if (model.CurrentUser == null)
            {
                return PartialView("_NotFoundPartial");
            }
            if (!model.CurrentProgeny.IsInAdminList(model.CurrentUser.UserEmail))
            {
                return PartialView("_AccessDeniedPartial");
            }

            model.KanbanBoard.Progeny = model.CurrentProgeny;
            model.KanbanBoard.Progeny.PictureLink = model.KanbanBoard.Progeny.GetProfilePictureUrl();

            model.SetPropertiesFromKanbanBoard(kanbanBoard);

            return PartialView("_DeleteKanbanBoardPartial", model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteKanbanBoard([FromForm] KanbanBoardViewModel model)
        {
            BaseItemsViewModel baseModel = await viewModelSetupService.SetupViewModel(Request.GetLanguageIdFromCookie(), User.GetEmail(), model.KanbanBoard.ProgenyId);
            model.SetBaseProperties(baseModel);
            if (!model.CurrentProgeny.IsInAdminList(model.CurrentUser.UserEmail))
            {
                return PartialView("_AccessDeniedPartial");
            }

            KanbanBoard existingKanbanBoard = await kanbanBoardsHttpClient.GetKanbanBoard(model.KanbanBoard.KanbanBoardId);
            if (existingKanbanBoard == null)
            {
                return PartialView("_NotFoundPartial");
            }

            if (model.DeleteTodoItems)
            {
                List<KanbanItem> kanbanItems = await kanbanItemsHttpClient.GetKanbanItemsForBoard(model.KanbanBoard.KanbanBoardId);
                foreach (KanbanItem kanbanItem in kanbanItems)
                {
                    await todoItemsHttpClient.DeleteTodoItem(kanbanItem.TodoItemId);
                }
            }

            KanbanBoard deletedKanbanBoard = await kanbanBoardsHttpClient.DeleteKanbanBoard(existingKanbanBoard, false);
            
            return Json(deletedKanbanBoard);
        }

        [HttpGet]
        public async Task<IActionResult> CopyKanbanBoard(int itemId)
        {
            KanbanBoard kanbanBoard = await kanbanBoardsHttpClient.GetKanbanBoard(itemId);
            BaseItemsViewModel baseModel = await viewModelSetupService.SetupViewModel(Request.GetLanguageIdFromCookie(), User.GetEmail(), kanbanBoard.ProgenyId);
            KanbanBoardViewModel model = new(baseModel);

            if (model.CurrentAccessLevel > kanbanBoard.AccessLevel)
            {
                return PartialView("_AccessDeniedPartial");
            }

            model.SetPropertiesFromKanbanBoard(kanbanBoard);

            if (User.Identity != null && User.Identity.IsAuthenticated && model.CurrentUser.UserId != null)
            {
                model.ProgenyList = await viewModelSetupService.GetProgenySelectList(model.CurrentUser);
                model.SetProgenyList();
                model.KanbanBoard.Progeny = model.CurrentProgeny;
                model.KanbanBoard.Progeny.PictureLink = model.KanbanBoard.Progeny.GetProfilePictureUrl();
            }

            model.SetAccessLevelList();
            
            return PartialView("_CopyKanbanBoardPartial", model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CopyKanbanBoard([FromForm] KanbanBoardViewModel model)
        {
            BaseItemsViewModel baseModel = await viewModelSetupService.SetupViewModel(Request.GetLanguageIdFromCookie(), User.GetEmail(), model.KanbanBoard.ProgenyId);
            model.SetBaseProperties(baseModel);

            if (!model.CurrentProgeny.IsInAdminList(model.CurrentUser.UserEmail))
            {
                return PartialView("_AccessDeniedPartial");
            }
            int originalKanbanBoardId = model.KanbanBoard.KanbanBoardId;

            KanbanBoard copiedKanbanBoard = model.CreateKanbanBoard();

            model.KanbanBoard = await kanbanBoardsHttpClient.AddKanbanBoard(copiedKanbanBoard);

            // Copy the Kanban Items, if CopyTodoItemsOption is 0 (Deep copy) or 1 (Shallow copy).
            if (model.CopyTodoItemsOption < 2)
            {
                List<KanbanItem> kanbanItems = await kanbanItemsHttpClient.GetKanbanItemsForBoard(originalKanbanBoardId);
                foreach (KanbanItem kanbanItem in kanbanItems)
                {
                    // If CopyTodoItemsOption is 0, we need to create a new TodoItem for each KanbanItem.
                    if (model.CopyTodoItemsOption == 0)
                    {
                        TodoItem todoItem = await todoItemsHttpClient.GetTodoItem(kanbanItem.TodoItemId);
                        todoItem.TodoItemId = 0;
                        todoItem.UId = Guid.NewGuid().ToString();
                        todoItem.CreatedBy = model.CurrentUser.UserId;
                        todoItem.CreatedTime = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, TimeZoneInfo.FindSystemTimeZoneById(model.CurrentUser.Timezone));
                        todoItem.ModifiedBy = model.CurrentUser.UserId;
                        todoItem.ModifiedTime = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, TimeZoneInfo.FindSystemTimeZoneById(model.CurrentUser.Timezone));
                        todoItem.ProgenyId = model.KanbanBoard.ProgenyId;
                        TodoItem newTodoItem = await todoItemsHttpClient.AddTodoItem(todoItem);
                        kanbanItem.TodoItemId = newTodoItem.TodoItemId;
                    }

                    kanbanItem.KanbanItemId = 0;
                    kanbanItem.KanbanBoardId = model.KanbanBoard.KanbanBoardId;
                    kanbanItem.CreatedBy = model.CurrentUser.UserId;
                    kanbanItem.CreatedTime = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, TimeZoneInfo.FindSystemTimeZoneById(model.CurrentUser.Timezone));
                    kanbanItem.ModifiedBy = model.CurrentUser.UserId;
                    kanbanItem.ModifiedTime = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, TimeZoneInfo.FindSystemTimeZoneById(model.CurrentUser.Timezone));
                    kanbanItem.UId = Guid.NewGuid().ToString();


                    await kanbanItemsHttpClient.AddKanbanItem(kanbanItem);
                }
            }
            

            return PartialView("_KanbanBoardCopiedPartial", model);
        }
    }
}
