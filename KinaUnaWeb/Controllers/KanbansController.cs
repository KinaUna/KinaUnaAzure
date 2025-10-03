using KinaUna.Data.Extensions;
using KinaUna.Data.Models;
using KinaUna.Data.Models.AccessManagement;
using KinaUna.Data.Models.DTOs;
using KinaUna.Data.Models.Family;
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
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace KinaUnaWeb.Controllers
{
    public class KanbansController(
        IViewModelSetupService viewModelSetupService,
        IKanbanBoardsHttpClient kanbanBoardsHttpClient,
        IUserInfosHttpClient userInfosHttpClient,
        IProgenyHttpClient progenyHttpClient,
        IFamiliesHttpClient familiesHttpClient,
        IKanbanItemsHttpClient kanbanItemsHttpClient,
        ITodoItemsHttpClient todoItemsHttpClient) : Controller
    {
        [AllowAnonymous]
        [HttpGet]
        public async Task<IActionResult> Index(int? kanbanBoardId, int childId = 0, int familyId = 0)
        {
            BaseItemsViewModel baseModel = await viewModelSetupService.SetupViewModel(Request.GetLanguageIdFromCookie(), User.GetEmail(), childId, familyId, false);
            KanbanBoardsListViewModel model = new(baseModel)
            {
                PopUpKanbanBoardId = kanbanBoardId ?? 0
            };
            
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
                FamilyIds = pageParameters.Families,
                Skip = (pageParameters.CurrentPageNumber - 1) * pageParameters.ItemsPerPage,
                NumberOfItems = pageParameters.ItemsPerPage,
                TagFilter = pageParameters.TagFilter,
                ContextFilter = pageParameters.ContextFilter,
            };

            KanbanBoardsResponse kanbanBoardsResponse = await kanbanBoardsHttpClient.GetKanbanBoardsList(kanbanBoardsRequest);

            return Json(kanbanBoardsResponse);

        }

        [AllowAnonymous]
        [HttpGet]
        public async Task<IActionResult> GetKanbanBoard(int kanbanBoardId)
        {
            KanbanBoard kanbanBoard = await kanbanBoardsHttpClient.GetKanbanBoard(kanbanBoardId);
            if (kanbanBoard == null || kanbanBoard.KanbanBoardId == 0)
            {
                return Json(new KanbanBoard());
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
                    KanbanBoard = new KanbanBoard(),
                    LanguageId = elementParameters.LanguageId
                });
            }

            KanbanBoard kanbanBoard = await kanbanBoardsHttpClient.GetKanbanBoard(elementParameters.KanbanBoardId);
            if (kanbanBoard.ProgenyId > 0)
            {
                kanbanBoard.Progeny = await progenyHttpClient.GetProgeny(kanbanBoard.ProgenyId);
            }

            if (kanbanBoard.FamilyId > 0)
            {
                kanbanBoard.Family = await familiesHttpClient.GetFamily(kanbanBoard.FamilyId);
            }
            
            KanbanBoardElementResponse response = new()
            {
                KanbanBoardId = kanbanBoard.KanbanBoardId,
                LanguageId = elementParameters.LanguageId,
                KanbanBoard = kanbanBoard
            };
            
            return PartialView("_KanbanBoardElementPartial", response);
        }

        [AllowAnonymous]
        [HttpGet]
        public async Task<IActionResult> ViewKanbanBoard(int kanbanBoardId, bool partialView = false)
        {
            KanbanBoard kanbanBoard = await kanbanBoardsHttpClient.GetKanbanBoard(kanbanBoardId);

            BaseItemsViewModel baseModel = await viewModelSetupService.SetupViewModel(Request.GetLanguageIdFromCookie(), User.GetEmail(), kanbanBoard.ProgenyId, kanbanBoard.FamilyId, false);
            KanbanBoardViewModel model = new(baseModel)
            {
                KanbanBoard = kanbanBoard
            };
            if (kanbanBoard.ProgenyId > 0)
            {
                kanbanBoard.Progeny = await progenyHttpClient.GetProgeny(kanbanBoard.ProgenyId);
            }
            if (kanbanBoard.FamilyId > 0)
            {
                kanbanBoard.Family = await familiesHttpClient.GetFamily(kanbanBoard.FamilyId);
            }

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
            BaseItemsViewModel baseModel = await viewModelSetupService.SetupViewModel(Request.GetLanguageIdFromCookie(), User.GetEmail(), 0, 0, false);
            KanbanBoardViewModel model = new(baseModel);
            if (model.CurrentUser == null)
            {
                return PartialView("_NotFoundPartial");
            }
            
            model.ProgenyList = await viewModelSetupService.GetProgenySelectList();
            model.SetProgenyList();
            model.FamilyList = await viewModelSetupService.GetFamilySelectList();
            model.SetFamilyList();
            model.KanbanBoard.CreatedTime = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, TimeZoneInfo.FindSystemTimeZoneById(model.CurrentUser.Timezone));
            
            return PartialView("_AddKanbanBoardPartial", model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddKanbanBoard([FromForm] KanbanBoardViewModel model)
        {
            BaseItemsViewModel baseModel = await viewModelSetupService.SetupViewModel(Request.GetLanguageIdFromCookie(), User.GetEmail(), model.KanbanBoard.ProgenyId, model.KanbanBoard.FamilyId, false);
            model.SetBaseProperties(baseModel);

            bool canUserAdd = false;
            if (model.KanbanBoard.ProgenyId > 0)
            {
                List<Progeny> progenies = await progenyHttpClient.GetProgeniesUserCanAccess(PermissionLevel.Add);
                if (progenies.Exists(p => p.Id == model.KanbanBoard.ProgenyId))
                {
                    canUserAdd = true;
                }
            }

            if (model.KanbanBoard.FamilyId > 0)
            {
                List<Family> families = await familiesHttpClient.GetFamiliesUserCanAccess(PermissionLevel.Add);
                if (families.Exists(f => f.FamilyId == model.KanbanBoard.FamilyId))
                {
                    canUserAdd = true;
                }
            }

            if (!canUserAdd)
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
            
            BaseItemsViewModel baseModel = await viewModelSetupService.SetupViewModel(Request.GetLanguageIdFromCookie(), User.GetEmail(), kanbanBoard.ProgenyId, kanbanBoard.FamilyId, false);
            KanbanBoardViewModel model = new(baseModel)
            {
                KanbanBoard = kanbanBoard
            };
            
            PermissionLevel userPermissionLevel = await userInfosHttpClient.GetItemPermissionForUser(KinaUnaTypes.TimeLineType.KanbanBoard, kanbanBoardId);
            if (userPermissionLevel < PermissionLevel.Edit)
            {
                return PartialView("_AccessDeniedPartial");
            }

            model.ProgenyList = await viewModelSetupService.GetProgenySelectList(kanbanBoard.ProgenyId);
            model.SetProgenyList();
            model.FamilyList = await viewModelSetupService.GetFamilySelectList(kanbanBoard.FamilyId);
            model.SetFamilyList();

            model.SetPropertiesFromKanbanBoard(kanbanBoard);

            return PartialView("_EditKanbanBoardPartial", model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditKanbanBoard([FromForm] KanbanBoardViewModel model)
        {
            BaseItemsViewModel baseModel = await viewModelSetupService.SetupViewModel(Request.GetLanguageIdFromCookie(), User.GetEmail(), model.KanbanBoard.ProgenyId, model.KanbanBoard.FamilyId, false);
            model.SetBaseProperties(baseModel);

            KanbanBoard existingKanbanBoard = await kanbanBoardsHttpClient.GetKanbanBoard(model.KanbanBoard.KanbanBoardId);
            if (existingKanbanBoard == null || existingKanbanBoard.KanbanBoardId == 0)
            {
                return PartialView("_NotFoundPartial");
            }

            PermissionLevel userPermissionLevel = await userInfosHttpClient.GetItemPermissionForUser(KinaUnaTypes.TimeLineType.KanbanBoard, model.KanbanBoard.KanbanBoardId);
            if (userPermissionLevel < PermissionLevel.Edit)
            {
                return PartialView("_AccessDeniedPartial");
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
            BaseItemsViewModel baseModel = await viewModelSetupService.SetupViewModel(Request.GetLanguageIdFromCookie(), User.GetEmail(), kanbanBoard.ProgenyId, kanbanBoard.FamilyId, false);
            KanbanBoardViewModel model = new(baseModel);
            model.SetBaseProperties(baseModel);
            
            KanbanBoard existingKanbanBoard = await kanbanBoardsHttpClient.GetKanbanBoard(kanbanBoard.KanbanBoardId);
            if (existingKanbanBoard == null || existingKanbanBoard.KanbanBoardId == 0)
            {
                return Json(kanbanBoard);
            }

            PermissionLevel userPermissionLevel = await userInfosHttpClient.GetItemPermissionForUser(KinaUnaTypes.TimeLineType.KanbanBoard, kanbanBoard.KanbanBoardId);
            if (userPermissionLevel < PermissionLevel.Edit)
            {
                return Json(kanbanBoard);
            }

            existingKanbanBoard.Columns = kanbanBoard.Columns;
            
            KanbanBoard updatedKanbanBoard = await kanbanBoardsHttpClient.UpdateKanbanBoard(existingKanbanBoard);
            
            return Json(updatedKanbanBoard);
        }

        [HttpGet]
        public async Task<IActionResult> DeleteKanbanBoard(int kanbanBoardId)
        {
            KanbanBoard kanbanBoard = await kanbanBoardsHttpClient.GetKanbanBoard(kanbanBoardId);
            PermissionLevel userPermissionLevel = await userInfosHttpClient.GetItemPermissionForUser(KinaUnaTypes.TimeLineType.KanbanBoard, kanbanBoard.KanbanBoardId);
            if (kanbanBoard.KanbanBoardId == 0 || userPermissionLevel < PermissionLevel.Admin)
            {
                return PartialView("_AccessDeniedPartial");
            }

            BaseItemsViewModel baseModel = await viewModelSetupService.SetupViewModel(Request.GetLanguageIdFromCookie(), User.GetEmail(), kanbanBoard.ProgenyId, kanbanBoard.FamilyId, false);
            KanbanBoardViewModel model = new(baseModel)
            {
                KanbanBoard = kanbanBoard
            };

            if (kanbanBoard.ProgenyId > 0)
            {
                model.KanbanBoard.Progeny = model.CurrentProgeny;
            }

            if (kanbanBoard.FamilyId > 0)
            {
                model.KanbanBoard.Family = model.CurrentFamily;
            }

            model.SetPropertiesFromKanbanBoard(kanbanBoard);

            return PartialView("_DeleteKanbanBoardPartial", model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteKanbanBoard([FromForm] KanbanBoardViewModel model)
        {
            BaseItemsViewModel baseModel = await viewModelSetupService.SetupViewModel(Request.GetLanguageIdFromCookie(), User.GetEmail(), model.KanbanBoard.ProgenyId, model.KanbanBoard.FamilyId, false);
            model.SetBaseProperties(baseModel);
            
            KanbanBoard existingKanbanBoard = await kanbanBoardsHttpClient.GetKanbanBoard(model.KanbanBoard.KanbanBoardId);
            if (existingKanbanBoard == null || existingKanbanBoard.KanbanBoardId == 0)
            {
                return PartialView("_NotFoundPartial");
            }

            PermissionLevel userPermissionLevel = await userInfosHttpClient.GetItemPermissionForUser(KinaUnaTypes.TimeLineType.KanbanBoard, existingKanbanBoard.KanbanBoardId);
            if (existingKanbanBoard.KanbanBoardId == 0 || userPermissionLevel < PermissionLevel.Admin)
            {
                return PartialView("_AccessDeniedPartial");
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
            if (kanbanBoard == null || kanbanBoard.KanbanBoardId == 0)
            {
                return PartialView("_AccessDeniedPartial");
            }

            BaseItemsViewModel baseModel = await viewModelSetupService.SetupViewModel(Request.GetLanguageIdFromCookie(), User.GetEmail(), kanbanBoard.ProgenyId, kanbanBoard.FamilyId, false);
            KanbanBoardViewModel model = new(baseModel);
            
            model.SetPropertiesFromKanbanBoard(kanbanBoard);
            
            model.ProgenyList = await viewModelSetupService.GetProgenySelectList(kanbanBoard.ProgenyId);
            model.SetProgenyList();
            model.FamilyList = await viewModelSetupService.GetFamilySelectList(kanbanBoard.FamilyId);
            model.SetFamilyList();

            return PartialView("_CopyKanbanBoardPartial", model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CopyKanbanBoard([FromForm] KanbanBoardViewModel model)
        {
            BaseItemsViewModel baseModel = await viewModelSetupService.SetupViewModel(Request.GetLanguageIdFromCookie(), User.GetEmail(), model.KanbanBoard.ProgenyId, model.KanbanBoard.FamilyId);
            model.SetBaseProperties(baseModel);
            
            bool canUserAdd = false;
            if (model.KanbanBoard.ProgenyId > 0)
            {
                List<Progeny> progenies = await progenyHttpClient.GetProgeniesUserCanAccess(PermissionLevel.Add);
                if (progenies.Exists(p => p.Id == model.KanbanBoard.ProgenyId))
                {
                    canUserAdd = true;
                }

            }

            if (model.KanbanBoard.FamilyId > 0)
            {
                List<Family> families = await familiesHttpClient.GetFamiliesUserCanAccess(PermissionLevel.Add);
                if (families.Exists(f => f.FamilyId == model.KanbanBoard.FamilyId))
                {
                    canUserAdd = true;
                }
            }

            if (!canUserAdd)
            {
                // Todo: Show that no children are available to add kanban for.
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
