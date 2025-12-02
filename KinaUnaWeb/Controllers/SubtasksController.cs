using KinaUna.Data.Extensions;
using KinaUna.Data.Models.AccessManagement;
using KinaUna.Data.Models.DTOs;
using KinaUnaWeb.Models;
using KinaUnaWeb.Models.ItemViewModels;
using KinaUnaWeb.Models.TypeScriptModels.TodoItems;
using KinaUnaWeb.Services;
using KinaUnaWeb.Services.HttpClients;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace KinaUnaWeb.Controllers
{
    public class SubtasksController(
        ISubtasksHttpClient subtasksHttpClient,
        ITodoItemsHttpClient todoItemsHttpClient,
        IViewModelSetupService viewModelSetupService,
        IUserInfosHttpClient userInfosHttpClient,
        IProgenyHttpClient progenyHttpClient,
        IFamiliesHttpClient familiesHttpClient,
        IKanbanItemsHttpClient kanbanItemsHttpClient,
        IKanbanBoardsHttpClient kanbanBoardsHttpClient) : Controller
    {
        [AllowAnonymous]
        [HttpPost]
        public async Task<IActionResult> GetSubtasksList([FromBody] SubtasksPageParameters parameters)
        {
            if (parameters.LanguageId == 0)
            {
                parameters.LanguageId = Request.GetLanguageIdFromCookie();
            }

            if (parameters.CurrentPageNumber < 1)
            {
                parameters.CurrentPageNumber = 1;
            }

            if (parameters.ItemsPerPage < 1)
            {
                parameters.ItemsPerPage = 10;
            }

            SubtasksRequest request = new()
            {
                ParentTodoItemId = parameters.ParentTodoItemId,
                ProgenyId = parameters.ProgenyId,
                FamilyId = parameters.FamilyId,
                StartYear = parameters.StartYear,
                StartMonth = parameters.StartMonth,
                StartDay = parameters.StartDay,
                EndYear = parameters.EndYear,
                EndMonth = parameters.EndMonth,
                EndDay = parameters.EndDay,
                Skip = (parameters.CurrentPageNumber - 1) * parameters.ItemsPerPage,
                NumberOfItems = parameters.ItemsPerPage,
                LocationFilter = parameters.LocationFilter,
                TagFilter = parameters.TagFilter,
                ContextFilter = parameters.ContextFilter,
                StatusFilter = parameters.StatusFilter,
                Sort = parameters.Sort,
                SortBy = parameters.SortBy,
                GroupBy = parameters.GroupBy
            };

            request.SetStartDateAndEndDate();

            UserInfo currentUserInfo = await userInfosHttpClient.GetUserInfo(User.GetEmail());

            SubtasksResponse subtasksResponse = await subtasksHttpClient.GetSubtasksList(request);

            foreach (TodoItem subtask in subtasksResponse.Subtasks)
            {
                if (subtask.DueDate.HasValue)
                {
                    subtask.DueDate = TimeZoneInfo.ConvertTimeFromUtc(subtask.DueDate.Value,
                        TimeZoneInfo.FindSystemTimeZoneById(currentUserInfo.Timezone));
                }

                if (subtask.StartDate.HasValue)
                {
                    subtask.StartDate = TimeZoneInfo.ConvertTimeFromUtc(subtask.StartDate.Value,
                        TimeZoneInfo.FindSystemTimeZoneById(currentUserInfo.Timezone));
                }

                if (subtask.CompletedDate.HasValue)
                {
                    subtask.CompletedDate = TimeZoneInfo.ConvertTimeFromUtc(subtask.CompletedDate.Value,
                        TimeZoneInfo.FindSystemTimeZoneById(currentUserInfo.Timezone));
                }

                subtask.CreatedTime = TimeZoneInfo.ConvertTimeFromUtc(subtask.CreatedTime,
                    TimeZoneInfo.FindSystemTimeZoneById(currentUserInfo.Timezone));

            }

            SubtasksPageResponse pageResponse = new(subtasksResponse);

            return Json(pageResponse);
        }

        [AllowAnonymous]
        [HttpPost]
        public async Task<IActionResult> SubtaskElement([FromBody] TodoItemParameters parameters)
        {
            if (parameters.LanguageId == 0)
            {
                parameters.LanguageId = Request.GetLanguageIdFromCookie();
            }

            TodoItemResponse todoItemResponse = new()
            {
                LanguageId = parameters.LanguageId
            };

            if (parameters.TodoItemId == 0)
            {
                todoItemResponse.TodoItem = new TodoItem { TodoItemId = 0 };
            }
            else
            {
                todoItemResponse.TodoItem = await subtasksHttpClient.GetSubtask(parameters.TodoItemId);
                if (todoItemResponse.TodoItem.ProgenyId > 0)
                {
                    todoItemResponse.TodoItem.Progeny = await progenyHttpClient.GetProgeny(todoItemResponse.TodoItem.ProgenyId);
                }

                if (todoItemResponse.TodoItem.FamilyId > 0)
                {
                    todoItemResponse.TodoItem.Family = await familiesHttpClient.GetFamily(todoItemResponse.TodoItem.FamilyId);
                }

                todoItemResponse.TodoItemId = todoItemResponse.TodoItem.TodoItemId;

                UserInfo todoUserInfo = await userInfosHttpClient.GetUserInfoByUserId(todoItemResponse.TodoItem.CreatedBy);
                todoItemResponse.TodoItem.CreatedBy = todoUserInfo.FullName();
            }
            
            return PartialView("_SubtaskElementPartial", todoItemResponse);
        }
        
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddSubtaskInline(TodoItem todoItem)
        {
            TodoItem parentTodoItem = await todoItemsHttpClient.GetTodoItem(todoItem.ParentTodoItemId);
            if (parentTodoItem == null || parentTodoItem.TodoItemId == 0)
            {
                return NotFound();
            }

            if (parentTodoItem.ItemPerMission.PermissionLevel < PermissionLevel.Add)
            {
                return Unauthorized();
            }

            BaseItemsViewModel baseModel = await viewModelSetupService.SetupViewModel(Request.GetLanguageIdFromCookie(), User.GetEmail(), parentTodoItem.ProgenyId, parentTodoItem.FamilyId, false);
            TodoViewModel model = new(baseModel);
            
            todoItem.ProgenyId = parentTodoItem.ProgenyId;
            todoItem.FamilyId = parentTodoItem.FamilyId;
            todoItem.CreatedTime = DateTime.UtcNow;

            model.SetPropertiesFromTodoItem(todoItem);

            TodoItem subtask = model.CreateTodoItem();

            model.TodoItem = await subtasksHttpClient.AddSubtask(subtask);
            
            model.TodoItem.CreatedTime = TimeZoneInfo.ConvertTimeFromUtc(model.TodoItem.CreatedTime, TimeZoneInfo.FindSystemTimeZoneById(model.CurrentUser.Timezone));
            if (model.TodoItem.CompletedDate.HasValue)
            {
                model.TodoItem.CompletedDate = TimeZoneInfo.ConvertTimeFromUtc(model.TodoItem.CompletedDate.Value, TimeZoneInfo.FindSystemTimeZoneById(model.CurrentUser.Timezone));
            }

            if (model.TodoItem.StartDate.HasValue)
            {
                model.TodoItem.StartDate = TimeZoneInfo.ConvertTimeFromUtc(model.TodoItem.StartDate.Value, TimeZoneInfo.FindSystemTimeZoneById(model.CurrentUser.Timezone));
            }

            if (model.TodoItem.DueDate.HasValue)
            {
                model.TodoItem.DueDate = TimeZoneInfo.ConvertTimeFromUtc(model.TodoItem.DueDate.Value, TimeZoneInfo.FindSystemTimeZoneById(model.CurrentUser.Timezone));
            }

            if (model.TodoItem.ProgenyId > 0)
            {
                model.TodoItem.Progeny = await progenyHttpClient.GetProgeny(model.TodoItem.ProgenyId);
            }
            if (model.TodoItem.FamilyId > 0)
            {
                model.TodoItem.Family = await familiesHttpClient.GetFamily(model.TodoItem.FamilyId);
            }

            return Json(model.TodoItem);
        }

        [HttpGet]
        public async Task<IActionResult> EditSubtask(int itemId)
        {
            TodoItem subtask = await subtasksHttpClient.GetSubtask(itemId);
            if (subtask == null || subtask.TodoItemId == 0)
            {
                return PartialView("_NotFoundPartial");
            }
            if (subtask.ItemPerMission.PermissionLevel < PermissionLevel.Edit)
            {
                return PartialView("_AccessDeniedPartial");
            }

            BaseItemsViewModel baseModel = await viewModelSetupService.SetupViewModel(Request.GetLanguageIdFromCookie(), User.GetEmail(), subtask.ProgenyId);
            TodoViewModel model = new(baseModel);
            
            model.ProgenyList = await viewModelSetupService.GetProgenySelectList();
            model.SetProgenyList();
            model.FamilyList = await viewModelSetupService.GetFamilySelectList();
            model.SetFamilyList();

            model.SetPropertiesFromTodoItem(subtask);

            model.KanbanItems = await kanbanItemsHttpClient.GetKanbanItemsForTodoItem(subtask.TodoItemId);
            model.SetStatusList(model.TodoItem.Status);

            return PartialView("_EditSubtaskPartial", model);
        }
        
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditSubtask(TodoViewModel model)
        {
            TodoItem existingSubtask = await subtasksHttpClient.GetSubtask(model.TodoItem.TodoItemId);
            if (existingSubtask == null || existingSubtask.TodoItemId == 0)
            {
                return PartialView("_NotFoundPartial");
            }
            if (existingSubtask.ItemPerMission.PermissionLevel < PermissionLevel.Edit)
            {
                return PartialView("_AccessDeniedPartial");
            }
            BaseItemsViewModel baseModel = await viewModelSetupService.SetupViewModel(Request.GetLanguageIdFromCookie(), User.GetEmail(), model.TodoItem.ProgenyId, model.TodoItem.FamilyId, false);
            model.SetBaseProperties(baseModel);
            
            TodoItem editedSubtask = model.CreateTodoItem();
            TodoItem updatedSubtask = await subtasksHttpClient.UpdateSubtask(editedSubtask);

            model.TodoItem = await todoItemsHttpClient.GetTodoItem(updatedSubtask.ParentTodoItemId);
            model.TodoItem.CreatedTime = TimeZoneInfo.ConvertTimeFromUtc(model.TodoItem.CreatedTime, TimeZoneInfo.FindSystemTimeZoneById(model.CurrentUser.Timezone));
            if (model.TodoItem.CompletedDate.HasValue)
            {
                model.TodoItem.CompletedDate = TimeZoneInfo.ConvertTimeFromUtc(model.TodoItem.CompletedDate.Value, TimeZoneInfo.FindSystemTimeZoneById(model.CurrentUser.Timezone));
            }
            if (model.TodoItem.StartDate.HasValue)
            {
                model.TodoItem.StartDate = TimeZoneInfo.ConvertTimeFromUtc(model.TodoItem.StartDate.Value, TimeZoneInfo.FindSystemTimeZoneById(model.CurrentUser.Timezone));
            }
            if (model.TodoItem.DueDate.HasValue)
            {
                model.TodoItem.DueDate = TimeZoneInfo.ConvertTimeFromUtc(model.TodoItem.DueDate.Value, TimeZoneInfo.FindSystemTimeZoneById(model.CurrentUser.Timezone));
            }

            if (model.TodoItem.ProgenyId > 0)
            {
                model.TodoItem.Progeny = await progenyHttpClient.GetProgeny(model.TodoItem.ProgenyId);
            }
            if (model.TodoItem.FamilyId > 0)
            {
                model.TodoItem.Family = await familiesHttpClient.GetFamily(model.TodoItem.FamilyId);
            }

            return PartialView("../Todos/_TodoDetailsPartial", model);
        }
        
        [HttpGet]
        public async Task<IActionResult> DeleteSubtask(int itemId)
        {
            TodoItem subtask = await subtasksHttpClient.GetSubtask(itemId);
            if (subtask == null || subtask.TodoItemId == 0)
            {
                return PartialView("_NotFoundPartial");
            }
            if (subtask.ItemPerMission.PermissionLevel < PermissionLevel.Admin)
            {
                return PartialView("_AccessDeniedPartial");
            }
            BaseItemsViewModel baseModel = await viewModelSetupService.SetupViewModel(Request.GetLanguageIdFromCookie(), User.GetEmail(), subtask.ProgenyId, subtask.FamilyId, false);
            TodoViewModel model = new(baseModel);
            
            model.TodoItem = subtask;
            model.SetStatusList(model.TodoItem.Status);
            if (model.TodoItem.ProgenyId > 0)
            {
                model.TodoItem.Progeny = model.CurrentProgeny;
                model.TodoItem.Progeny.PictureLink = model.TodoItem.Progeny.GetProfilePictureUrl();
            }
            if (model.TodoItem.FamilyId > 0)
            {
                model.TodoItem.Family = model.CurrentFamily;
                model.TodoItem.Family.PictureLink = model.TodoItem.Family.GetProfilePictureUrl();
            }

            return PartialView("_DeleteSubtaskPartial", model);
        }
        
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteSubtask(TodoViewModel model)
        {
            TodoItem subtask = await subtasksHttpClient.GetSubtask(model.TodoItem.TodoItemId);
            if (subtask == null || subtask.TodoItemId == 0)
            {
                return NotFound();
            }
            if (subtask.ItemPerMission.PermissionLevel < PermissionLevel.Admin)
            {
                return Unauthorized();
            }

            BaseItemsViewModel baseModel = await viewModelSetupService.SetupViewModel(Request.GetLanguageIdFromCookie(), User.GetEmail(), subtask.ProgenyId, subtask.FamilyId, false);
            model.SetBaseProperties(baseModel);
            
            _ = await subtasksHttpClient.DeleteSubtask(subtask.TodoItemId);
            
            model.TodoItem = await todoItemsHttpClient.GetTodoItem(subtask.ParentTodoItemId);
            model.TodoItem.CreatedTime = TimeZoneInfo.ConvertTimeFromUtc(model.TodoItem.CreatedTime, TimeZoneInfo.FindSystemTimeZoneById(model.CurrentUser.Timezone));
            if (model.TodoItem.CompletedDate.HasValue)
            {
                model.TodoItem.CompletedDate = TimeZoneInfo.ConvertTimeFromUtc(model.TodoItem.CompletedDate.Value, TimeZoneInfo.FindSystemTimeZoneById(model.CurrentUser.Timezone));
            }

            if (model.TodoItem.StartDate.HasValue)
            {
                model.TodoItem.StartDate = TimeZoneInfo.ConvertTimeFromUtc(model.TodoItem.StartDate.Value, TimeZoneInfo.FindSystemTimeZoneById(model.CurrentUser.Timezone));
            }

            if (model.TodoItem.DueDate.HasValue)
            {
                model.TodoItem.DueDate = TimeZoneInfo.ConvertTimeFromUtc(model.TodoItem.DueDate.Value, TimeZoneInfo.FindSystemTimeZoneById(model.CurrentUser.Timezone));
            }
            
            return Json(model.TodoItem);
        }
        
        public async Task<IActionResult> AddSubtaskToKanbanBoard(int subtaskId)
        {
            TodoItem subtask = await subtasksHttpClient.GetSubtask(subtaskId);
            if (subtask == null || subtask.TodoItemId == 0)
            {
                return PartialView("_NotFoundPartial");
            }
            // Todo: Check permission for Kanban board too?
            if (subtask.ItemPerMission.PermissionLevel < PermissionLevel.Edit)
            {
                return PartialView("_AccessDeniedPartial");
            }

            BaseItemsViewModel baseModel = await viewModelSetupService.SetupViewModel(Request.GetLanguageIdFromCookie(), User.GetEmail(), subtask.ProgenyId, subtask.FamilyId, false);
            KanbanItemViewModel model = new(baseModel)
            {
                KanbanItem = new KanbanItem
                {
                    TodoItem = subtask,
                }
            };

            model.ProgenyList = await viewModelSetupService.GetProgenySelectList();
            model.SetProgenyList();
            model.FamilyList = await viewModelSetupService.GetFamilySelectList();
            model.SetFamilyList();

            model.SetStatusList(model.KanbanItem.TodoItem.Status);
            
            List<int> progenyIds = [];
            List<int> familyIds = [];
            progenyIds.AddRange(model.ProgenyList.ConvertAll(p => int.Parse(p.Value)));
            familyIds.AddRange(model.FamilyList.ConvertAll(f => int.Parse(f.Value)));
            KanbanBoardsRequest kanbanBoardsRequest = new()
            {
                ProgenyIds = progenyIds,
                FamilyIds = familyIds,
                IncludeDeleted = false,
                Skip = 0,
                NumberOfItems = 0 // Get all.
            };
            
            KanbanBoardsResponse kanbanBoardsResponse = await kanbanBoardsHttpClient.GetKanbanBoardsList(kanbanBoardsRequest);
            model.KanbanBoards = kanbanBoardsResponse.KanbanBoards;
            model.KanbanBoardsList = new List<SelectListItem>();

            foreach (KanbanBoard kanbanBoard in model.KanbanBoards)
            {
                List<KanbanItem> kanbanItems = await kanbanItemsHttpClient.GetKanbanItemsForBoard(kanbanBoard.KanbanBoardId);
                // If there is a KanbanItem with the TodoItemId for this subtask already, don't include it.
                if (kanbanItems.Exists(k => k.TodoItemId == subtaskId))
                {
                    continue;
                }

                if (kanbanBoard.ProgenyId > 0)
                {
                    kanbanBoard.Progeny = await progenyHttpClient.GetProgeny(kanbanBoard.ProgenyId);
                    SelectListItem item = new()
                    {
                        Text = kanbanBoard.Title + " (" + kanbanBoard.Progeny.NickName + ")",
                        Value = kanbanBoard.KanbanBoardId.ToString()
                    };
                    model.KanbanBoardsList.Add(item);
                }

                if (kanbanBoard.FamilyId > 0)
                {
                    kanbanBoard.Family = await familiesHttpClient.GetFamily(kanbanBoard.FamilyId);
                    SelectListItem item = new()
                    {
                        Text = kanbanBoard.Title + " (" + kanbanBoard.Family.Name + ")",
                        Value = kanbanBoard.KanbanBoardId.ToString()
                    };
                    model.KanbanBoardsList.Add(item);
                }

            }

            return PartialView("_AddSubtaskToKanbanBoardPartial", model);
        }

        [ValidateAntiForgeryToken]
        [HttpPost]
        public async Task<IActionResult> AddSubtaskToKanbanBoard([FromForm] KanbanItemViewModel model)
        {
            int kanbanBoardId = model.KanbanItem.KanbanBoardId;
            TodoItem subtask = await subtasksHttpClient.GetSubtask(model.KanbanItem.TodoItem.TodoItemId);
            if (subtask == null || subtask.TodoItemId == 0)
            {
                return NotFound();
            }

            // Check that user has edit permissions for the subtask.
            if (subtask.ItemPerMission.PermissionLevel < PermissionLevel.Edit)
            {
                return Unauthorized();
            }
            
            BaseItemsViewModel baseModel = await viewModelSetupService.SetupViewModel(Request.GetLanguageIdFromCookie(), User.GetEmail(), subtask.ProgenyId, subtask.FamilyId, false);
            model.SetBaseProperties(baseModel);
            
            
            KanbanBoard kanbanBoard = await kanbanBoardsHttpClient.GetKanbanBoard(kanbanBoardId);
            if (kanbanBoard == null || kanbanBoard.KanbanBoardId == 0)
            {
                return NotFound();
            }

            // Check that user has permission to add for the kanban board.
            if (kanbanBoard.ItemPerMission.PermissionLevel < PermissionLevel.Add)
            {
                return Unauthorized();
            }

            kanbanBoard.SetColumnsListFromColumns();
            KanbanBoardColumn column = kanbanBoard.ColumnsList.SingleOrDefault(k => k.ColumnIndex == 0);
            if (column != null)
            {
                model.KanbanItem.ColumnId = column.Id;

            }

            foreach (KanbanBoardColumn columnItem in kanbanBoard.ColumnsList)
            {
                if (columnItem.SetStatus != subtask.Status) continue;
                model.KanbanItem.ColumnId = columnItem.Id;
                break;
            }

            KanbanItem newKanbanItem = new()
            {
                KanbanBoardId = kanbanBoard.KanbanBoardId,
                ColumnId = model.KanbanItem.ColumnId,
                RowIndex = -1,
                TodoItemId = subtask.TodoItemId,
                CreatedBy = model.CurrentUser.UserEmail,
                CreatedTime = DateTime.UtcNow
            };

            KanbanItem addedKanbanItem = await kanbanItemsHttpClient.AddKanbanItem(newKanbanItem);

            return Json(addedKanbanItem);
        }

        [Authorize]
        [HttpGet]
        public async Task<IActionResult> SetSubtaskAsNotStarted(int todoId)
        {
            TodoItem subtask = await subtasksHttpClient.GetSubtask(todoId);
            if (subtask == null || subtask.TodoItemId == 0)
            {
                return NotFound();
            }
            if (subtask.ItemPerMission.PermissionLevel < PermissionLevel.Edit)
            {
                return Unauthorized();
            }
            
            subtask.CompletedDate = null;
            subtask.Status = (int)KinaUnaTypes.TodoStatusType.NotStarted;
            TodoItem result = await subtasksHttpClient.UpdateSubtask(subtask);

            // If there is a KanbanItem for this subtask, update its column too.
            await UpdateKanbanItemsStatus(subtask);

            return Json(result);
        }
        
        [Authorize]
        [HttpGet]
        public async Task<IActionResult> SetSubtaskAsInProgress(int todoId)
        {
            TodoItem subtask = await subtasksHttpClient.GetSubtask(todoId);
            if (subtask == null || subtask.TodoItemId == 0)
            {
                return NotFound();
            }

            if (subtask.ItemPerMission.PermissionLevel < PermissionLevel.Edit)
            {
                return Unauthorized();
            }
            
            subtask.CompletedDate = null;
            subtask.Status = (int)KinaUnaTypes.TodoStatusType.InProgress;
            TodoItem result = await subtasksHttpClient.UpdateSubtask(subtask);

            // If there is a KanbanItem for this subtask, update its column too.
            await UpdateKanbanItemsStatus(subtask);

            return Json(result);
        }
        
        [Authorize]
        [HttpGet]
        public async Task<IActionResult> SetSubtaskAsCompleted(int todoId)
        {
            TodoItem subtask = await subtasksHttpClient.GetSubtask(todoId);
            if (subtask == null || subtask.TodoItemId == 0)
            {
                return NotFound();
            }

            if (subtask.ItemPerMission.PermissionLevel < PermissionLevel.Edit)
            {
                return Unauthorized();
            }
            
            subtask.CompletedDate = DateTime.UtcNow;
            subtask.Status = (int)KinaUnaTypes.TodoStatusType.Completed;
            TodoItem result = await subtasksHttpClient.UpdateSubtask(subtask);

            // If there is a KanbanItem for this subtask, update its column too.
            await UpdateKanbanItemsStatus(subtask);

            return Json(result);
        }

        [Authorize]
        [HttpGet]
        public async Task<IActionResult> SetSubtaskAsCancelled(int todoId)
        {
            TodoItem subtask = await subtasksHttpClient.GetSubtask(todoId);
            if (subtask == null || subtask.TodoItemId == 0)
            {
                return NotFound();
            }

            if (subtask.ItemPerMission.PermissionLevel < PermissionLevel.Edit)
            {
                return Unauthorized();
            }

            subtask.CompletedDate = null;
            subtask.Status = (int)KinaUnaTypes.TodoStatusType.Cancelled;
            TodoItem result = await subtasksHttpClient.UpdateSubtask(subtask);

            // If there is a KanbanItem for this subtask, update its column too.
            await UpdateKanbanItemsStatus(subtask);

            return Json(result);
        }

        private async Task UpdateKanbanItemsStatus(TodoItem subtask)
        {
            List<KanbanItem> kanbanItems = await kanbanItemsHttpClient.GetKanbanItemsForTodoItem(subtask.TodoItemId);
            if (kanbanItems.Count > 0)
            {
                foreach (KanbanItem kanbanItem in kanbanItems)
                {
                    KanbanBoard kanbanBoard = await kanbanBoardsHttpClient.GetKanbanBoard(kanbanItem.KanbanBoardId);
                    if (kanbanBoard == null || kanbanBoard.KanbanBoardId == 0 || kanbanBoard.ItemPerMission.PermissionLevel < PermissionLevel.Edit)
                    {
                        continue;
                    }
                    kanbanBoard.SetColumnsListFromColumns();
                    foreach (KanbanBoardColumn kanbanBoardColumn in kanbanBoard.ColumnsList)
                    {
                        if (kanbanBoardColumn.SetStatus > -1 && subtask.Status == kanbanBoardColumn.SetStatus)
                        {
                            kanbanItem.ColumnId = kanbanBoardColumn.Id;
                            kanbanItem.RowIndex = -1;
                            kanbanItem.TodoItem = subtask;
                            _ = await kanbanItemsHttpClient.UpdateKanbanItem(kanbanItem);
                            break;
                        }
                    }
                }
            }
        }
    }
}
