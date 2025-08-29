using KinaUna.Data.Extensions;
using KinaUna.Data.Models.DTOs;
using KinaUnaWeb.Models;
using KinaUnaWeb.Models.ItemViewModels;
using KinaUnaWeb.Services;
using KinaUnaWeb.Services.HttpClients;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using KinaUnaWeb.Models.TypeScriptModels.TodoItems;

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
        ISubtasksHttpClient subtasksHttpClient) : Controller
    {
        /// <summary>
        /// The Index Page for Todos.
        /// Shows the list of TodoItems for the currently selected Progenies.
        /// If a TodoItemId is passed, it will be shown in a popup.
        /// </summary>
        /// <param name="todoItemId">The TodoItemId to show in a popup.</param>
        /// <param name="childId"></param>
        /// <returns></returns>
        [AllowAnonymous]
        public async Task<IActionResult> Index(int? todoItemId, int childId = 0)
        {
            BaseItemsViewModel baseModel = await viewModelSetupService.SetupViewModel(Request.GetLanguageIdFromCookie(), User.GetEmail(), childId);
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
                todoItemResponse.TodoItem.Progeny = await progenyHttpClient.GetProgeny(todoItemResponse.TodoItem.ProgenyId);
                todoItemResponse.TodoItemId = todoItemResponse.TodoItem.TodoItemId;

                BaseItemsViewModel baseModel = await viewModelSetupService.SetupViewModel(parameters.LanguageId, User.GetEmail(), todoItemResponse.TodoItem.ProgenyId);
                todoItemResponse.IsCurrentUserProgenyAdmin = baseModel.IsCurrentUserProgenyAdmin;
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
            BaseItemsViewModel baseModel = await viewModelSetupService.SetupViewModel(Request.GetLanguageIdFromCookie(), User.GetEmail(), todoItem.ProgenyId);
            TodoViewModel model = new(baseModel)
            {
                TodoItem = todoItem
            };

            model.TodoItem.Progeny = model.CurrentProgeny;
            model.TodoItem.Progeny.PictureLink = model.TodoItem.Progeny.GetProfilePictureUrl();
            UserInfo todoUserInfo = await userInfosHttpClient.GetUserInfoByUserId(model.TodoItem.CreatedBy);
            model.TodoItem.CreatedBy = todoUserInfo.FullName();
            model.SetStatusList(model.TodoItem.Status);
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
            BaseItemsViewModel baseModel = await viewModelSetupService.SetupViewModel(Request.GetLanguageIdFromCookie(), User.GetEmail(), 0);
            TodoViewModel model = new(baseModel);
            if (model.CurrentUser == null)
            {
                return PartialView("_NotFoundPartial");
            }

            if (User.Identity != null && User.Identity.IsAuthenticated && model.CurrentUser.UserId != null)
            {
                model.ProgenyList = await viewModelSetupService.GetProgenySelectList(model.CurrentUser);
                model.SetProgenyList();
            }

            model.TodoItem.CreatedTime = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, TimeZoneInfo.FindSystemTimeZoneById(model.CurrentUser.Timezone));

            model.SetAccessLevelList();

            return PartialView("_AddTodoPartial", model);
        }

        /// <summary>
        /// HttpPost endpoint for adding a new TodoItem.
        /// </summary>
        /// <param name="model">TodoViewModel with the properties for the TodoItem to add.</param>
        /// <returns>Redirects to Todos/Index page.</returns>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddTodo(TodoViewModel model)
        {
            BaseItemsViewModel baseModel = await viewModelSetupService.SetupViewModel(Request.GetLanguageIdFromCookie(), User.GetEmail(), model.TodoItem.ProgenyId);
            model.SetBaseProperties(baseModel);

            List<Progeny> progAdminList = await progenyHttpClient.GetProgenyAdminList(model.CurrentUser.UserEmail);
            if (progAdminList.Count == 0)
            {
                // Todo: Show that no children are available to add TodoItem for.
                return RedirectToAction("Index");
            }

            TodoItem todoItem = model.CreateTodoItem();

            model.TodoItem = await todoItemsHttpClient.AddTodoItem(todoItem);
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
            BaseItemsViewModel baseModel = await viewModelSetupService.SetupViewModel(Request.GetLanguageIdFromCookie(), User.GetEmail(), todoItem.ProgenyId);
            TodoViewModel model = new(baseModel);

            if (!model.CurrentProgeny.IsInAdminList(model.CurrentUser.UserEmail))
            {
                return PartialView("_AccessDeniedPartial");
            }

            if (User.Identity != null && User.Identity.IsAuthenticated && model.CurrentUser.UserId != null)
            {
                model.ProgenyList = await viewModelSetupService.GetProgenySelectList(model.CurrentUser);
                model.SetProgenyList();
            }

            model.SetPropertiesFromTodoItem(todoItem);

            model.SetAccessLevelList();
            model.SetStatusList(model.TodoItem.Status);

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
            BaseItemsViewModel baseModel = await viewModelSetupService.SetupViewModel(Request.GetLanguageIdFromCookie(), User.GetEmail(), model.TodoItem.ProgenyId);
            model.SetBaseProperties(baseModel);

            if (!model.CurrentProgeny.IsInAdminList(model.CurrentUser.UserEmail))
            {
                return PartialView("_AccessDeniedPartial");
            }

            TodoItem editedTodoItem = model.CreateTodoItem();

            model.TodoItem = await todoItemsHttpClient.UpdateTodoItem(editedTodoItem);
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
            BaseItemsViewModel baseModel = await viewModelSetupService.SetupViewModel(Request.GetLanguageIdFromCookie(), User.GetEmail(), todoItem.ProgenyId);
            TodoViewModel model = new(baseModel);

            if (!model.CurrentProgeny.IsInAdminList(model.CurrentUser.UserEmail))
            {
                // Todo: Show no access info.
                return RedirectToAction("Index");
            }

            model.TodoItem = todoItem;
            model.SetStatusList(model.TodoItem.Status);
            model.TodoItem.Progeny = model.CurrentProgeny;
            model.TodoItem.Progeny.PictureLink = model.TodoItem.Progeny.GetProfilePictureUrl();

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
            BaseItemsViewModel baseModel = await viewModelSetupService.SetupViewModel(Request.GetLanguageIdFromCookie(), User.GetEmail(), todoItem.ProgenyId);
            model.SetBaseProperties(baseModel);

            if (!model.CurrentProgeny.IsInAdminList(model.CurrentUser.UserEmail))
            {
                // Todo: Show no access info.
                return RedirectToAction("Index");
            }

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
            BaseItemsViewModel baseModel = await viewModelSetupService.SetupViewModel(Request.GetLanguageIdFromCookie(), User.GetEmail(), todoItem.ProgenyId);
            TodoViewModel model = new(baseModel);

            if (model.CurrentAccessLevel > todoItem.AccessLevel)
            {
                return PartialView("_AccessDeniedPartial");
            }

            model.SetPropertiesFromTodoItem(todoItem);

            if (User.Identity != null && User.Identity.IsAuthenticated && model.CurrentUser.UserId != null)
            {
                model.ProgenyList = await viewModelSetupService.GetProgenySelectList(model.CurrentUser);
                model.SetProgenyList();
            }

            model.SetAccessLevelList();
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
        public async Task<IActionResult> CopyTodo(TodoViewModel model)
        {
            BaseItemsViewModel baseModel = await viewModelSetupService.SetupViewModel(Request.GetLanguageIdFromCookie(), User.GetEmail(), model.TodoItem.ProgenyId);
            model.SetBaseProperties(baseModel);

            if (!model.CurrentProgeny.IsInAdminList(model.CurrentUser.UserEmail))
            {
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
            BaseItemsViewModel baseModel = await viewModelSetupService.SetupViewModel(Request.GetLanguageIdFromCookie(), User.GetEmail(), todoItem.ProgenyId);
            TodoViewModel model = new(baseModel);
            if (!model.CurrentProgeny.IsInAdminList(model.CurrentUser.UserEmail))
            {
                return Unauthorized("Access denied.");
            }

            todoItem.CompletedDate = null;
            todoItem.Status = (int)KinaUnaTypes.TodoStatusType.NotStarted;
            TodoItem result = await todoItemsHttpClient.UpdateTodoItem(todoItem);

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
            BaseItemsViewModel baseModel = await viewModelSetupService.SetupViewModel(Request.GetLanguageIdFromCookie(), User.GetEmail(), todoItem.ProgenyId);
            TodoViewModel model = new(baseModel);
            if (!model.CurrentProgeny.IsInAdminList(model.CurrentUser.UserEmail))
            {
                return Unauthorized("Access denied.");
            }

            todoItem.CompletedDate = null;
            todoItem.Status = (int)KinaUnaTypes.TodoStatusType.InProgress;
            TodoItem result = await todoItemsHttpClient.UpdateTodoItem(todoItem);

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
            BaseItemsViewModel baseModel = await viewModelSetupService.SetupViewModel(Request.GetLanguageIdFromCookie(), User.GetEmail(), todoItem.ProgenyId);
            TodoViewModel model = new(baseModel);
            if (!model.CurrentProgeny.IsInAdminList(model.CurrentUser.UserEmail))
            {
                return Unauthorized("Access denied.");
            }

            todoItem.CompletedDate = DateTime.UtcNow;
            todoItem.Status = (int)KinaUnaTypes.TodoStatusType.Completed;
            TodoItem result = await todoItemsHttpClient.UpdateTodoItem(todoItem);

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
            BaseItemsViewModel baseModel = await viewModelSetupService.SetupViewModel(Request.GetLanguageIdFromCookie(), User.GetEmail(), todoItem.ProgenyId);
            TodoViewModel model = new(baseModel);
            if (!model.CurrentProgeny.IsInAdminList(model.CurrentUser.UserEmail))
            {
                return Unauthorized("Access denied.");
            }

            todoItem.CompletedDate = null;
            todoItem.Status = (int)KinaUnaTypes.TodoStatusType.Cancelled;
            TodoItem result = await todoItemsHttpClient.UpdateTodoItem(todoItem);

            return Json(result);
        }
    }
}
