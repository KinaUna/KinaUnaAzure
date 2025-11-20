using KinaUna.Data.Extensions;
using KinaUna.Data.Models.AccessManagement;
using KinaUna.Data.Models.DTOs;
using KinaUna.Data.Models.Family;
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
    /// <summary>
    /// The TodosController handles the management of TodoItems.
    /// </summary>
    /// <param name="todoItemsHttpClient">Service for managing TodoItems.</param>
    /// <param name="viewModelSetupService">Service for setting up view models with common data.</param>
    /// <param name="userInfosHttpClient">Service for managing user information.</param>
    /// <param name="progenyHttpClient">Service for managing Progeny data.</param>
    public class TodosController(
        ITodoItemsHttpClient todoItemsHttpClient,
        IViewModelSetupService viewModelSetupService,
        IUserInfosHttpClient userInfosHttpClient,
        IProgenyHttpClient progenyHttpClient,
        IFamiliesHttpClient familiesHttpClient,
        ISubtasksHttpClient subtasksHttpClient,
        IKanbanItemsHttpClient kanbanItemsHttpClient,
        IKanbanBoardsHttpClient kanbanBoardsHttpClient) : Controller
    {
        /// <summary>
        /// The Index Page for Todos.
        /// Shows the list of TodoItems for the currently selected Progenies.
        /// If a TodoItemId is passed, it will be shown in a popup.
        /// </summary>
        /// <param name="todoItemId">The TodoItemId to show in a popup.</param>
        /// <param name="familyId"></param>
        /// <param name="childId"></param>
        /// <returns></returns>
        [AllowAnonymous]
        public async Task<IActionResult> Index(int? todoItemId, int childId = 0, int familyId = 0)
        {
            BaseItemsViewModel baseModel = await viewModelSetupService.SetupViewModel(Request.GetLanguageIdFromCookie(), User.GetEmail(), childId, familyId, false);
            TodoItemsListViewModel model = new(baseModel)
            {
                PopUpTodoItemId = todoItemId ?? 0
            };

            if (model.PopUpTodoItemId != 0)
            {
                model.TodoItemsList.Add(await todoItemsHttpClient.GetTodoItem(model.PopUpTodoItemId));
            }

            return View(model);
        }

        /// <summary>
        /// Post endpoint for retrieving a list of TodoItems based on the provided parameters.
        /// </summary>
        /// <param name="parameters">TodoItemsPageParameters containing the filtering and pagination parameters.</param>
        /// <returns>A JSON response containing the paginated list of TodoItems.</returns>
        [AllowAnonymous]
        [HttpPost]
        public async Task<IActionResult> GetTodoItemsList([FromBody] TodoItemsPageParameters parameters)
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

            TodoItemsRequest request = new()
            {
                ProgenyIds = parameters.Progenies,
                FamilyIds = parameters.Families,
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

            TodoItemsResponse todoItemsResponse = await todoItemsHttpClient.GetProgeniesTodoItemsList(request);

            foreach (TodoItem todoItem in todoItemsResponse.TodoItems)
            {
                if (todoItem.DueDate.HasValue)
                {
                    todoItem.DueDate = TimeZoneInfo.ConvertTimeFromUtc(todoItem.DueDate.Value,
                        TimeZoneInfo.FindSystemTimeZoneById(currentUserInfo.Timezone));
                }

                if (todoItem.StartDate.HasValue)
                {
                    todoItem.StartDate = TimeZoneInfo.ConvertTimeFromUtc(todoItem.StartDate.Value,
                        TimeZoneInfo.FindSystemTimeZoneById(currentUserInfo.Timezone));
                }

                if (todoItem.CompletedDate.HasValue)
                {
                    todoItem.CompletedDate = TimeZoneInfo.ConvertTimeFromUtc(todoItem.CompletedDate.Value,
                        TimeZoneInfo.FindSystemTimeZoneById(currentUserInfo.Timezone));
                }

                todoItem.CreatedTime = TimeZoneInfo.ConvertTimeFromUtc(todoItem.CreatedTime,
                    TimeZoneInfo.FindSystemTimeZoneById(currentUserInfo.Timezone));

            }

            TodosPageResponse pageResponse = new(todoItemsResponse);

            return Json(pageResponse);
        }

        /// <summary>
        /// Returns a partial view for a TodoItem element based on the provided parameters.
        /// </summary>
        /// <param name="parameters">TodoItemParameters containing the TodoItemId and LanguageId.</param>
        /// <returns></returns>
        [AllowAnonymous]
        [HttpPost]
        public async Task<IActionResult> TodoElement([FromBody] TodoItemParameters parameters)
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
                todoItemResponse.TodoItem = await todoItemsHttpClient.GetTodoItem(parameters.TodoItemId);
                if (todoItemResponse.TodoItem == null)
                {
                    return PartialView("_NotFoundPartial");
                }

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


            return PartialView("_TodoItemElementPartial", todoItemResponse);
        }

        /// <summary>
        /// Renders the details of a TodoItem.
        /// If partialView is true, it returns a partial view.
        /// </summary>
        /// <param name="todoId">The TodoItemId of the TodoItem to view.</param>
        /// <param name="partialView">Flag indicating whether to return a partial view or a full view.</param>
        /// <returns>Partial view or full view with TodoViewModel.</returns>
        [AllowAnonymous]
        public async Task<IActionResult> ViewTodo(int todoId, bool partialView = false)
        {
            TodoItem todoItem = await todoItemsHttpClient.GetTodoItem(todoId);
            if (todoItem == null || todoItem.TodoItemId == 0)
            {
                return PartialView("_NotFoundPartial");
            }
            BaseItemsViewModel baseModel = await viewModelSetupService.SetupViewModel(Request.GetLanguageIdFromCookie(), User.GetEmail(), todoItem.ProgenyId, todoItem.FamilyId, false);
            TodoViewModel model = new(baseModel)
            {
                TodoItem = todoItem
            };

            if (model.TodoItem.ProgenyId > 0)
            {
                model.TodoItem.Progeny = model.CurrentProgeny;
            }

            if (model.TodoItem.FamilyId > 0)
            {
                model.TodoItem.Family = model.CurrentFamily;
            }

            UserInfo todoUserInfo = await userInfosHttpClient.GetUserInfoByUserId(model.TodoItem.CreatedBy);
            model.TodoItem.CreatedBy = todoUserInfo.FullName();
            model.SetStatusList(model.TodoItem.Status);
            model.KanbanItems = await kanbanItemsHttpClient.GetKanbanItemsForTodoItem(todoId);
            foreach (KanbanItem kanbanItem in model.KanbanItems)
            {
                kanbanItem.KanbanBoard = await kanbanBoardsHttpClient.GetKanbanBoard(kanbanItem.KanbanBoardId);
                if (kanbanItem.KanbanBoard.ProgenyId > 0)
                {
                    kanbanItem.KanbanBoard.Progeny = await progenyHttpClient.GetProgeny(kanbanItem.KanbanBoard.ProgenyId);
                }

                if (kanbanItem.KanbanBoard.FamilyId > 0)
                {
                    kanbanItem.KanbanBoard.Family = await familiesHttpClient.GetFamily(kanbanItem.KanbanBoard.FamilyId);
                }
            }

            model.ProgenyList = await viewModelSetupService.GetProgenySelectList();
            model.SetProgenyList();
            model.FamilyList = await viewModelSetupService.GetFamilySelectList();
            model.SetFamilyList();

            List<int> progenyIds = [];
            progenyIds.AddRange(model.ProgenyList.ConvertAll(p => int.Parse(p.Value)));
            List<int> familyIds = [];
            familyIds.AddRange(model.FamilyList.ConvertAll(p => int.Parse(p.Value)));
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
            model.KanbanBoardsList = [];
            foreach (KanbanBoard kanbanBoard in model.KanbanBoards)
            {
                if (kanbanBoard.ItemPerMission.PermissionLevel < PermissionLevel.Add)
                {
                    continue;
                }
                // If there is a KanbanItem with the KanbanBoardId for this TodoItem already, don't include it.
                if (model.KanbanItems.Exists(k => k.KanbanBoardId == kanbanBoard.KanbanBoardId))
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

            if (partialView)
            {
                return PartialView("_TodoDetailsPartial", model);
            }

            return View(model);
        }

        /// <summary>
        /// Page for adding a new TodoItem.
        /// </summary>
        /// <returns>View with TodoViewModel.</returns>
        [HttpGet]
        public async Task<IActionResult> AddTodo()
        {
            BaseItemsViewModel baseModel = await viewModelSetupService.SetupViewModel(Request.GetLanguageIdFromCookie(), User.GetEmail(), 0, 0, false);
            TodoViewModel model = new(baseModel);
            if (model.CurrentUser == null)
            {
                return PartialView("_NotFoundPartial");
            }

            model.ProgenyList = await viewModelSetupService.GetProgenySelectList();
            model.SetProgenyList();
            model.FamilyList = await viewModelSetupService.GetFamilySelectList();
            model.SetFamilyList();

            model.TodoItem.CreatedTime = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, TimeZoneInfo.FindSystemTimeZoneById(model.CurrentUser.Timezone));

            List<int> progenyIds = [];
            progenyIds.AddRange(model.ProgenyList.ConvertAll(p => int.Parse(p.Value)));
            List<int> familyIds = [];
            familyIds.AddRange(model.FamilyList.ConvertAll(p => int.Parse(p.Value)));
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
            SelectListItem noKanbanBoardSelected = new SelectListItem
            {
                Selected = true,
                Text = "Do not add to a Kanban board",
                Value = "0"
            };
            model.KanbanBoardsList.Add(noKanbanBoardSelected);
            
            foreach (KanbanBoard kanbanBoard in model.KanbanBoards)
            {
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

            return PartialView("_AddTodoPartial", model);
        }

        /// <summary>
        /// HttpPost endpoint for adding a new TodoItem.
        /// </summary>
        /// <param name="model">TodoViewModel with the properties for the TodoItem to add.</param>
        /// <returns>Redirects to Todos/Index page.</returns>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddTodo([FromForm] TodoViewModel model)
        {
            BaseItemsViewModel baseModel = await viewModelSetupService.SetupViewModel(Request.GetLanguageIdFromCookie(), User.GetEmail(), model.TodoItem.ProgenyId, model.TodoItem.FamilyId, false);
            model.SetBaseProperties(baseModel);
            bool canUserAdd = false;
            if (model.TodoItem.ProgenyId > 0)
            {
                List<Progeny> progenies = await progenyHttpClient.GetProgeniesUserCanAccess(PermissionLevel.Add);
                if (progenies.Exists(p => p.Id == model.TodoItem.ProgenyId))
                {
                    canUserAdd = true;
                }
            }

            if (model.TodoItem.FamilyId > 0)
            {
                List<Family> families = await familiesHttpClient.GetFamiliesUserCanAccess(PermissionLevel.Add);
                if (families.Exists(f => f.FamilyId == model.TodoItem.FamilyId))
                {
                    canUserAdd = true;
                }
            }

            if (!canUserAdd)
            {
                // Todo: Show that no entities are available to add TodoItems for.
                return PartialView("_NotFoundPartial");
            }
            
            TodoItem todoItem = model.CreateTodoItem();

            model.TodoItem = await todoItemsHttpClient.AddTodoItem(todoItem);
           
            if (model.TodoItem.TodoItemId != 0 && model.AddToKanbanBoardId != 0)
            {
                KanbanItem kanbanItem = new KanbanItem()
                {
                    KanbanBoardId = model.AddToKanbanBoardId,
                    TodoItemId = model.TodoItem.TodoItemId,
                    ColumnId = -1,
                    RowIndex = -1,
                    CreatedBy = model.CurrentUser.UserId,
                    CreatedTime = DateTime.UtcNow,
                    ModifiedBy = model.CurrentUser.UserId,
                    ModifiedTime = DateTime.UtcNow
                };

                await kanbanItemsHttpClient.AddKanbanItem(kanbanItem);
            }
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
            return PartialView("_TodoAddedPartial", model);
        }

        /// <summary>
        /// Edit TodoItem page.
        /// </summary>
        /// <param name="itemId">The TodoItemId of the TodoItem to edit.</param>
        /// <returns>View with TodoViewModel.</returns>
        [HttpGet]
        public async Task<IActionResult> EditTodo(int itemId)
        {
            TodoItem todoItem = await todoItemsHttpClient.GetTodoItem(itemId);
            if (todoItem == null || todoItem.TodoItemId == 0)
            {
                return PartialView("_NotFoundPartial");
            }
            if (todoItem.ItemPerMission.PermissionLevel < PermissionLevel.Edit)
            {
                return PartialView("_AccessDeniedPartial");
            }
            BaseItemsViewModel baseModel = await viewModelSetupService.SetupViewModel(Request.GetLanguageIdFromCookie(), User.GetEmail(), todoItem.ProgenyId, todoItem.FamilyId, false);
            TodoViewModel model = new(baseModel);
            
            model.ProgenyList = await viewModelSetupService.GetProgenySelectList();
            model.SetProgenyList();
            model.FamilyList = await viewModelSetupService.GetFamilySelectList();
            model.SetFamilyList();

            model.SetPropertiesFromTodoItem(todoItem); 
            model.SetStatusList(model.TodoItem.Status);

            model.KanbanItems = await kanbanItemsHttpClient.GetKanbanItemsForTodoItem(model.TodoItem.TodoItemId);

            return PartialView("_EditTodoPartial", model);
        }

        /// <summary>
        /// HttpPost endpoint for updating an edited TodoItem.
        /// </summary>
        /// <param name="model">TodoViewModel with the updated TodoItem properties.</param>
        /// <returns>TodoItem updated page.</returns>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditTodo(TodoViewModel model)
        {
            TodoItem existingTodoItem = await todoItemsHttpClient.GetTodoItem(model.TodoItem.TodoItemId);
            if (existingTodoItem == null || existingTodoItem.TodoItemId == 0)
            {
                return PartialView("_NotFoundPartial");
            }
            if (existingTodoItem.ItemPerMission.PermissionLevel < PermissionLevel.Edit)
            {
                return PartialView("_AccessDeniedPartial");
            }

            BaseItemsViewModel baseModel = await viewModelSetupService.SetupViewModel(Request.GetLanguageIdFromCookie(), User.GetEmail(), model.TodoItem.ProgenyId, model.TodoItem.FamilyId, false);
            model.SetBaseProperties(baseModel);
            
            TodoItem editedTodoItem = model.CreateTodoItem();

            model.TodoItem = await todoItemsHttpClient.UpdateTodoItem(editedTodoItem);

            // If there are any KanbanItems for this TodoItem, update the column for all of them.
            await UpdateKanbanItemsStatus(model.TodoItem);

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
            return PartialView("_TodoUpdatedPartial", model);
        }

        /// <summary>
        /// Page to delete a TodoItem.
        /// </summary>
        /// <param name="itemId">The TodoItemId of the TodoItem to delete.</param>
        /// <returns>View with TodoViewModel.</returns>
        [HttpGet]
        public async Task<IActionResult> DeleteTodo(int itemId)
        {
            TodoItem todoItem = await todoItemsHttpClient.GetTodoItem(itemId);
            if (todoItem == null || todoItem.TodoItemId == 0)
            {
                return PartialView("_NotFoundPartial");
            }
            if (todoItem.ItemPerMission.PermissionLevel < PermissionLevel.Admin)
            {
                return PartialView("_AccessDeniedPartial");
            }
            BaseItemsViewModel baseModel = await viewModelSetupService.SetupViewModel(Request.GetLanguageIdFromCookie(), User.GetEmail(), todoItem.ProgenyId, todoItem.FamilyId, false);
            TodoViewModel model = new(baseModel);
            
            model.TodoItem = todoItem;
            model.SetStatusList(model.TodoItem.Status);
            if (model.TodoItem.ProgenyId > 0)
            {
                model.TodoItem.Progeny = model.CurrentProgeny;
            }

            if (model.TodoItem.FamilyId > 0)
            {
                model.TodoItem.Family = model.CurrentFamily;
            }

            return PartialView("_DeleteTodoPartial", model);
        }

        /// <summary>
        /// HttpPost endpoint for deleting a TodoItem.
        /// </summary>
        /// <param name="model">TodoViewModel with properties of the TodoItem to delete.</param>
        /// <returns>Redirects to Todos/Index page.</returns>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteTodo(TodoViewModel model)
        {
            TodoItem todoItem = await todoItemsHttpClient.GetTodoItem(model.TodoItem.TodoItemId);
            if (todoItem == null || todoItem.TodoItemId == 0)
            {
                return PartialView("_NotFoundPartial");
            }

            if (todoItem.ItemPerMission.PermissionLevel < PermissionLevel.Admin)
            {
                return PartialView("_AccessDeniedPartial");
            }
            
            BaseItemsViewModel baseModel = await viewModelSetupService.SetupViewModel(Request.GetLanguageIdFromCookie(), User.GetEmail(), todoItem.ProgenyId, todoItem.FamilyId, false);
            model.SetBaseProperties(baseModel);
            
            _ = await todoItemsHttpClient.DeleteTodoItem(todoItem.TodoItemId);
            return Json(todoItem);
        }

        /// <summary>
        /// Copy TodoItem page.
        /// </summary>
        /// <param name="itemId">The TodoItemID of the TodoItem to copy.</param>
        /// <returns>View with TodoViewModel.</returns>
        [HttpGet]
        public async Task<IActionResult> CopyTodo(int itemId)
        {
            TodoItem todoItem = await todoItemsHttpClient.GetTodoItem(itemId);
            if (todoItem == null || todoItem.TodoItemId == 0)
            {
                return PartialView("_NotFoundPartial");
            }

            BaseItemsViewModel baseModel = await viewModelSetupService.SetupViewModel(Request.GetLanguageIdFromCookie(), User.GetEmail(), todoItem.ProgenyId, todoItem.FamilyId, false);
            TodoViewModel model = new(baseModel);

            model.ProgenyList = await viewModelSetupService.GetProgenySelectList();
            model.SetProgenyList();
            model.FamilyList = await viewModelSetupService.GetFamilySelectList();
            model.SetFamilyList();

            model.SetPropertiesFromTodoItem(todoItem);
            model.SetStatusList(model.TodoItem.Status);
            
            return PartialView("_CopyTodoPartial", model);
        }

        /// <summary>
        /// HttpPost endpoint for updating an edited TodoItem.
        /// </summary>
        /// <param name="model">TodoViewModel with the updated TodoItem properties.</param>
        /// <returns>TodoItem copied partial view</returns>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CopyTodo([FromForm] TodoViewModel model)
        {
            BaseItemsViewModel baseModel = await viewModelSetupService.SetupViewModel(Request.GetLanguageIdFromCookie(), User.GetEmail(), model.TodoItem.ProgenyId, model.TodoItem.FamilyId, false);
            model.SetBaseProperties(baseModel);

            bool canUserAdd = false;
            if (model.TodoItem.ProgenyId > 0)
            {
                List<Progeny> progenies = await progenyHttpClient.GetProgeniesUserCanAccess(PermissionLevel.Add);
                if (progenies.Exists(p => p.Id == model.TodoItem.ProgenyId))
                {
                    canUserAdd = true;
                }
            }

            if (model.TodoItem.FamilyId > 0)
            {
                List<Family> families = await familiesHttpClient.GetFamiliesUserCanAccess(PermissionLevel.Add);
                if (families.Exists(f => f.FamilyId == model.TodoItem.FamilyId))
                {
                    canUserAdd = true;
                }
            }

            if (!canUserAdd)
            {
                // Todo: Show that no entities are available to add TodoItems for.
                return PartialView("_AccessDeniedPartial");
            }


            TodoItem copiedTodoItem = model.CreateTodoItem();

            model.TodoItem = await todoItemsHttpClient.AddTodoItem(copiedTodoItem);

            if (model.CopyFromTodoId != 0 && model.CopySubtasks)
            {
                // Copy subtasks from original TodoItem.
                SubtasksRequest subtasksRequest = new()
                {
                    ParentTodoItemId = model.CopyFromTodoId,
                    ProgenyId = model.TodoItem.ProgenyId,
                    FamilyId = model.TodoItem.FamilyId,
                    Skip = 0,
                    NumberOfItems = 0 // Get all.
                };
                SubtasksResponse subtasksResponse = await subtasksHttpClient.GetSubtasksList(subtasksRequest);
                foreach (TodoItem subTask in subtasksResponse.Subtasks)
                {
                    subTask.TodoItemId = 0;
                    subTask.ParentTodoItemId = model.TodoItem.TodoItemId;

                    _ = await subtasksHttpClient.AddSubtask(subTask);
                }
            }

            if (model.TodoItem.StartDate.HasValue)
            {
                model.TodoItem.StartDate = TimeZoneInfo.ConvertTimeFromUtc(model.TodoItem.StartDate.Value, TimeZoneInfo.FindSystemTimeZoneById(model.CurrentUser.Timezone));
            }
            if (model.TodoItem.DueDate.HasValue)
            {
                model.TodoItem.DueDate = TimeZoneInfo.ConvertTimeFromUtc(model.TodoItem.DueDate.Value, TimeZoneInfo.FindSystemTimeZoneById(model.CurrentUser.Timezone));
            }
            if (model.TodoItem.CompletedDate.HasValue)
            {
                model.TodoItem.CompletedDate = TimeZoneInfo.ConvertTimeFromUtc(model.TodoItem.CompletedDate.Value, TimeZoneInfo.FindSystemTimeZoneById(model.CurrentUser.Timezone));
            }

            model.TodoItem.CreatedTime = TimeZoneInfo.ConvertTimeFromUtc(model.TodoItem.CreatedTime, TimeZoneInfo.FindSystemTimeZoneById(model.CurrentUser.Timezone));
            model.SetStatusList(model.TodoItem.Status);

            return PartialView("_TodoCopiedPartial", model);
        }

        /// <summary>
        /// Sets the status of a specified to-do item to "Not Started."
        /// </summary>
        /// <remarks>This method requires the user to be authorized and ensures that the user has
        /// administrative access to the associated progeny before updating the to-do item.</remarks>
        /// <param name="todoId">The unique identifier of the to-do item to update.</param>
        /// <returns>An <see cref="IActionResult"/> containing the updated to-do item in JSON format if the operation succeeds.
        /// Returns <see cref="UnauthorizedResult"/> if the user does not have the necessary permissions.</returns>
        [Authorize]
        [HttpGet]
        public async Task<IActionResult> SetTodoAsNotStarted(int todoId)
        {
            TodoItem todoItem = await todoItemsHttpClient.GetTodoItem(todoId);
            if (todoItem == null || todoItem.TodoItemId == 0 || todoItem.ItemPerMission.PermissionLevel < PermissionLevel.Edit && todoItem.Progeny?.UserId != User.GetUserId())
            {
                return Json(new TodoItem());
            }

            todoItem.CompletedDate = null;
            todoItem.Status = (int)KinaUnaTypes.TodoStatusType.NotStarted;
            TodoItem result = await todoItemsHttpClient.UpdateTodoItem(todoItem);

            // If there are any KanbanItems for this TodoItem, update the column for all of them.
            await UpdateKanbanItemsStatus(result);

            return Json(result);
        }

        /// <summary>
        /// Marks the specified to-do item as "In Progress" and updates its status.
        /// </summary>
        /// <remarks>This method requires the user to be authorized and ensures that the user has
        /// administrative access to the associated progeny before updating the to-do item's status.</remarks>
        /// <param name="todoId">The unique identifier of the to-do item to update.</param>
        /// <returns>An <see cref="IActionResult"/> containing the updated to-do item in JSON format if the operation succeeds.
        /// Returns <see cref="UnauthorizedResult"/> if the user does not have the necessary permissions.</returns>
        [Authorize]
        [HttpGet]
        public async Task<IActionResult> SetTodoAsInProgress(int todoId)
        {
            TodoItem todoItem = await todoItemsHttpClient.GetTodoItem(todoId);
            if (todoItem == null || todoItem.TodoItemId == 0 || todoItem.ItemPerMission.PermissionLevel < PermissionLevel.Edit && todoItem.Progeny?.UserId != User.GetUserId())
            {
                return Json(new TodoItem());
            }

            todoItem.CompletedDate = null;
            todoItem.Status = (int)KinaUnaTypes.TodoStatusType.InProgress;
            TodoItem result = await todoItemsHttpClient.UpdateTodoItem(todoItem);
            // If there are any KanbanItems for this TodoItem, update the column for all of them.
            await UpdateKanbanItemsStatus(result);

            return Json(result);
        }

        /// <summary>
        /// Marks the specified to-do item as completed and updates its status.
        /// </summary>
        /// <remarks>This method requires the user to be authorized and ensures that the user has
        /// administrative access to the progeny associated with the to-do item. If the user is not authorized, the
        /// method returns an HTTP 401 Unauthorized response.</remarks>
        /// <param name="todoId">The unique identifier of the to-do item to be marked as completed.</param>
        /// <returns>An <see cref="IActionResult"/> containing the updated to-do item in JSON format if the operation succeeds.
        /// Returns <see cref="UnauthorizedResult"/> if the user does not have administrative access to the associated
        /// progeny.</returns>
        [Authorize]
        [HttpGet]
        public async Task<IActionResult> SetTodoAsCompleted(int todoId)
        {
            TodoItem todoItem = await todoItemsHttpClient.GetTodoItem(todoId);
            if (todoItem == null || todoItem.TodoItemId == 0 || todoItem.ItemPerMission.PermissionLevel < PermissionLevel.Edit && todoItem.Progeny?.UserId != User.GetUserId())
            {
                return Json(new TodoItem());
            }

            todoItem.CompletedDate = DateTime.UtcNow;
            todoItem.Status = (int)KinaUnaTypes.TodoStatusType.Completed;
            TodoItem result = await todoItemsHttpClient.UpdateTodoItem(todoItem);
            // If there are any KanbanItems for this TodoItem, update the column for all of them.
            await UpdateKanbanItemsStatus(result);

            return Json(result);
        }

        /// <summary>
        /// Marks the specified to-do item as cancelled.
        /// </summary>
        /// <remarks>This method requires the user to be authorized and ensures that the user has
        /// administrative access to the progeny associated with the to-do item. The to-do item's status is updated to
        /// "Cancelled," and its completion date is cleared.</remarks>
        /// <param name="todoId">The unique identifier of the to-do item to be marked as cancelled.</param>
        /// <returns>An <see cref="IActionResult"/> containing the updated to-do item in JSON format if the operation succeeds.
        /// Returns <see cref="UnauthorizedResult"/> if the current user does not have administrative access to the
        /// associated progeny.</returns>
        [Authorize]
        [HttpGet]
        public async Task<IActionResult> SetTodoAsCancelled(int todoId)
        {
            TodoItem todoItem = await todoItemsHttpClient.GetTodoItem(todoId);
            if (todoItem == null || todoItem.TodoItemId == 0 || todoItem.ItemPerMission.PermissionLevel < PermissionLevel.Edit && todoItem.Progeny?.UserId != User.GetUserId())
            {
                return Json(new TodoItem());
            }

            todoItem.CompletedDate = null;
            todoItem.Status = (int)KinaUnaTypes.TodoStatusType.Cancelled;
            TodoItem result = await todoItemsHttpClient.UpdateTodoItem(todoItem);
            // If there are any KanbanItems for this TodoItem, update the column for all of them.
            await UpdateKanbanItemsStatus(result);

            return Json(result);
        }

        /// <summary>
        /// Prepares and returns a partial view for adding a to-do item to a Kanban board.
        /// </summary>
        /// <remarks>This method retrieves the specified to-do item and prepares a view model containing
        /// the relevant Kanban boards  and associated data. The view model includes the current user's permissions,
        /// progeny, and family information,  as well as the list of Kanban boards where the to-do item can be added.
        /// Boards where the to-do item already exists  or where the user lacks sufficient permissions are
        /// excluded.</remarks>
        /// <param name="todoItemId">The unique identifier of the to-do item to be added to a Kanban board. Must be a positive integer.</param>
        /// <returns>A <see cref="Task{IActionResult}"/> representing the asynchronous operation.  The result is a partial view
        /// containing the necessary data to add the specified to-do item to a Kanban board. Returns <see
        /// cref="NotFoundResult"/> if the to-do item does not exist.</returns>
        public async Task<IActionResult> AddTodoItemToKanbanBoard(int todoItemId)
        {
            TodoItem todoItem = await todoItemsHttpClient.GetTodoItem(todoItemId);
            if (todoItem == null || todoItem.TodoItemId == 0)
            {
                return NotFound();
            }
            
            BaseItemsViewModel baseModel = await viewModelSetupService.SetupViewModel(Request.GetLanguageIdFromCookie(), User.GetEmail(), todoItem.ProgenyId, todoItem.FamilyId, false);
            KanbanItemViewModel model = new(baseModel)
            {
                KanbanItem = new KanbanItem
                {
                    TodoItem = todoItem,
                }
            };

            model.ProgenyList = await viewModelSetupService.GetProgenySelectList();
            model.SetProgenyList();
            model.FamilyList = await viewModelSetupService.GetFamilySelectList();
            model.SetFamilyList();
            if (model.KanbanItem.TodoItem.ProgenyId > 0)
            {
                model.KanbanItem.TodoItem.Progeny = model.CurrentProgeny;
            }

            if (model.KanbanItem.TodoItem.FamilyId > 0)
            {
                model.KanbanItem.TodoItem.Family = model.CurrentFamily;
            }

            model.SetStatusList(model.KanbanItem.TodoItem.Status);
            
            List<int> progenyIds = [];
            progenyIds.AddRange(model.ProgenyList.ConvertAll(p => int.Parse(p.Value)));
            List<int> familyIds = [];
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
            model.KanbanBoardsList = [];

            foreach (KanbanBoard kanbanBoard in model.KanbanBoards)
            {
                List<KanbanItem> kanbanItems = await kanbanItemsHttpClient.GetKanbanItemsForBoard(kanbanBoard.KanbanBoardId);
                // If there is a KanbanItem with the TodoItemId for this subtask already, don't include it.
                if (kanbanItems.Exists(k => k.TodoItemId == todoItemId))
                {
                    continue;
                }

                if (kanbanBoard.ItemPerMission.PermissionLevel < PermissionLevel.Add)
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

            return PartialView("_AddTodoItemToKanbanBoardPartial", model);
        }

        /// <summary>
        /// Adds the specified to-do item to a Kanban board based on the provided model.
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        [ValidateAntiForgeryToken]
        [HttpPost]
        public async Task<IActionResult> AddTodoItemToKanbanBoard([FromForm] KanbanItemViewModel model)
        {
            int kanbanBoardId = model.KanbanItem.KanbanBoardId;
            TodoItem todoItem = await todoItemsHttpClient.GetTodoItem(model.KanbanItem.TodoItem.TodoItemId);
            if (todoItem == null || todoItem.TodoItemId == 0)
            {
                return NotFound();
            }

            BaseItemsViewModel baseModel = await viewModelSetupService.SetupViewModel(Request.GetLanguageIdFromCookie(), User.GetEmail(), todoItem.ProgenyId, todoItem.FamilyId, false);
            model.SetBaseProperties(baseModel);
            
            KanbanBoard kanbanBoard = await kanbanBoardsHttpClient.GetKanbanBoard(kanbanBoardId);
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
                if (columnItem.SetStatus != todoItem.Status) continue;
                model.KanbanItem.ColumnId = columnItem.Id;
                break;
            }

            KanbanItem newKanbanItem = new()
            {
                KanbanBoardId = kanbanBoard.KanbanBoardId,
                ColumnId = model.KanbanItem.ColumnId,
                RowIndex = -1,
                TodoItemId = todoItem.TodoItemId,
                CreatedBy = model.CurrentUser.UserEmail,
                CreatedTime = DateTime.UtcNow
            };

            KanbanItem addedKanbanItem = await kanbanItemsHttpClient.AddKanbanItem(newKanbanItem);

            return Json(addedKanbanItem);
        }

        /// <summary>
        /// Gets the page for assigning the specified to-do item to a different progeny or family.
        /// </summary>
        /// <param name="todoItemId"></param>
        /// <returns></returns>
        public async Task<IActionResult> AssignTodoItemTo(int todoItemId)
        {
            TodoItem todoItem = await todoItemsHttpClient.GetTodoItem(todoItemId);
            if (todoItem == null || todoItem.TodoItemId == 0)
            {
                return NotFound();
            }

            if (todoItem.ItemPerMission.PermissionLevel < PermissionLevel.Edit && todoItem.Progeny?.UserId != User.GetUserId())
            {
                return Unauthorized();
            }

            BaseItemsViewModel baseModel = await viewModelSetupService.SetupViewModel(Request.GetLanguageIdFromCookie(), User.GetEmail(), todoItem.ProgenyId, todoItem.FamilyId, false);
            TodoViewModel model = new(baseModel)
            {
                TodoItem = todoItem
            };
            
            if (model.TodoItem.ProgenyId > 0)
            {
                model.TodoItem.Progeny = model.CurrentProgeny;
            }
            if (model.TodoItem.FamilyId > 0)
            {
                model.TodoItem.Family = model.CurrentFamily;
            }

            model.ProgenyList = await viewModelSetupService.GetProgenySelectList();
            model.FamilyList = await viewModelSetupService.GetFamilySelectList();
            // model.SetFamilyList();
            if (model.FamilyList.Count > 0)
            {
                foreach (SelectListItem familySelectListItem in model.FamilyList)
                {
                    familySelectListItem.Value = "-" + familySelectListItem.Value;
                    model.ProgenyList.Add(familySelectListItem);
                }
            }
            model.SetProgenyList();
            
            model.SetStatusList(model.TodoItem.Status);
            
            return PartialView("_AssignTodoItemToPartial", model);
        }

        /// <summary>
        /// Assigns the specified to-do item to a different progeny or family based on the provided model.
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        [ValidateAntiForgeryToken]
        [HttpPost]
        public async Task<IActionResult> AssignTodoItemTo([FromForm] TodoViewModel model)
        {
            TodoItem todoItem = await todoItemsHttpClient.GetTodoItem(model.TodoItem.TodoItemId);
            if (todoItem == null || todoItem.TodoItemId == 0)
            {
                return NotFound();
            }
            if (todoItem.ItemPerMission.PermissionLevel < PermissionLevel.Edit && todoItem.Progeny?.UserId != User.GetUserId())
            {
                return Unauthorized();
            }

            if (model.TodoItem.ProgenyId < 0)
            {
                model.TodoItem.FamilyId = Math.Abs(model.TodoItem.ProgenyId);
                model.TodoItem.ProgenyId = 0;
            }

            BaseItemsViewModel baseModel = await viewModelSetupService.SetupViewModel(Request.GetLanguageIdFromCookie(), User.GetEmail(), model.TodoItem.ProgenyId, model.TodoItem.FamilyId, false);
            model.SetBaseProperties(baseModel);
            
            // Check if user is allowed to assign to the new progeny/family id.
            if (model.TodoItem.ProgenyId > 0)
            {
                if (model.CurrentProgeny.ProgenyPerMission.PermissionLevel < PermissionLevel.Add)
                {
                    return Unauthorized();
                }
            }

            if (model.TodoItem.FamilyId > 0)
            {
                if (model.CurrentFamily.FamilyPermission.PermissionLevel < PermissionLevel.Add)
                {
                    return Unauthorized();
                }
            }

            todoItem.ProgenyId = model.TodoItem.ProgenyId;
            todoItem.FamilyId = model.TodoItem.FamilyId;

            TodoItem updatedTodoItem = await todoItemsHttpClient.UpdateTodoItem(todoItem);

            return Json(updatedTodoItem);
        }

        /// <summary>
        /// Updates the status of Kanban items associated with the specified to-do item.
        /// </summary>
        /// <remarks>This method retrieves all Kanban items linked to the specified to-do item and updates
        /// their column assignment based on the status of the to-do item. The column assignment is determined by
        /// matching the to-do item's status with the column's configured status in the associated Kanban board. If a
        /// match is found, the Kanban item's column ID is updated, and the item is sent to the Kanban items service for
        /// persistence.</remarks>
        /// <param name="todoItem">The to-do item whose associated Kanban items will be updated. The status of the to-do item determines the
        /// column assignment for the Kanban items.</param>
        /// <returns></returns>
        private async Task UpdateKanbanItemsStatus(TodoItem todoItem)
        {
            List<KanbanItem> kanbanItems = await kanbanItemsHttpClient.GetKanbanItemsForTodoItem(todoItem.TodoItemId);
            if (kanbanItems.Count > 0)
            {
                foreach (KanbanItem kanbanItem in kanbanItems)
                {
                    KanbanBoard kanbanBoard = await kanbanBoardsHttpClient.GetKanbanBoard(kanbanItem.KanbanBoardId);
                    kanbanBoard.SetColumnsListFromColumns();
                    foreach (KanbanBoardColumn kanbanBoardColumn in kanbanBoard.ColumnsList)
                    {
                        if (kanbanBoardColumn.SetStatus > -1 && todoItem.Status == kanbanBoardColumn.SetStatus)
                        {
                            kanbanItem.ColumnId = kanbanBoardColumn.Id;
                            kanbanItem.RowIndex = -1;
                            kanbanItem.TodoItem = todoItem;
                            _ = await kanbanItemsHttpClient.UpdateKanbanItem(kanbanItem);
                            break;
                        }
                    }
                }
            }
        }
    }
}
