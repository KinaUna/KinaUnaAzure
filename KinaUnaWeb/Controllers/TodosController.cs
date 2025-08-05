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
using System.Linq;
using System.Threading.Tasks;
using KinaUnaWeb.Models.TypeScriptModels.TodoItems;

namespace KinaUnaWeb.Controllers
{
    public class TodosController(ITodoItemsHttpClient todoItemsHttpClient, IViewModelSetupService viewModelSetupService,
        IUserInfosHttpClient userInfosHttpClient, IProgenyHttpClient progenyHttpClient) : Controller
    {
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

        [AllowAnonymous]
        [HttpPost]
        public async Task<IActionResult> GetTodoItemsList([FromBody] TodoItemsRequest request)
        {
            UserInfo currentUserInfo = await userInfosHttpClient.GetUserInfo(User.GetEmail());
            request.StartDate = new DateTime(request.StartYear, request.StartMonth, request.StartDay);
            request.EndDate = new DateTime(request.EndYear, request.EndMonth, request.EndDay);

            List<TodoItem> todoItems = await todoItemsHttpClient.GetProgeniesTodoItemsList(request);

            todoItems = [.. todoItems.OrderBy(t => t.DueDate)];
            List<TodoItem> resultList = [];
            List<Progeny> progeniesList = [];

            foreach (TodoItem todoItem in todoItems)
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

                Progeny progeny = progeniesList.FirstOrDefault(p => p.Id == todoItem.ProgenyId);
                if (progeny == null)
                {
                    progeny = await progenyHttpClient.GetProgeny(todoItem.ProgenyId);
                    progeniesList.Add(progeny);
                }
                
                resultList.Add(todoItem);
            }

            return Json(resultList);
        }

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
                UserInfo noteUserInfo = await userInfosHttpClient.GetUserInfoByUserId(todoItemResponse.TodoItem.CreatedBy);
                todoItemResponse.TodoItem.CreatedBy = noteUserInfo.FullName();
            }


            return PartialView("_TodoItemElementPartial", todoItemResponse);
        }

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
        [Authorize]
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
                // Todo: Show that no children are available to add note for.
                return RedirectToAction("Index");
            }

            TodoItem todoItem = model.CreateTodoItem();

            model.TodoItem = await todoItemsHttpClient.AddTodoItem(todoItem);
            model.TodoItem.CreatedTime = TimeZoneInfo.ConvertTimeFromUtc(model.TodoItem.CreatedTime, TimeZoneInfo.FindSystemTimeZoneById(model.CurrentUser.Timezone));

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

            model.SetPropertiesFromTodoItem(todoItem);

            model.SetAccessLevelList();

            
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

            return View(model);
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
            return RedirectToAction("Index", "Todos");
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
            
            return PartialView("_CopyTodoPartial", model);
        }

        /// <summary>
        /// HttpPost endpoint for updating an edited TodoItem.
        /// </summary>
        /// <param name="model">NoteViewModel with the updated TodoItem properties.</param>
        /// <returns>TodoItem copied partial view</returns>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CopyNote(TodoViewModel model)
        {
            BaseItemsViewModel baseModel = await viewModelSetupService.SetupViewModel(Request.GetLanguageIdFromCookie(), User.GetEmail(), model.TodoItem.ProgenyId);
            model.SetBaseProperties(baseModel);

            if (!model.CurrentProgeny.IsInAdminList(model.CurrentUser.UserEmail))
            {
                return PartialView("_AccessDeniedPartial");
            }

            TodoItem copiedTodoItem = model.CreateTodoItem();

            model.TodoItem = await todoItemsHttpClient.AddTodoItem(copiedTodoItem);
            model.TodoItem.CreatedTime = TimeZoneInfo.ConvertTimeFromUtc(model.TodoItem.CreatedTime, TimeZoneInfo.FindSystemTimeZoneById(model.CurrentUser.Timezone));

            return PartialView("_TodoCopiedPartial", model);
        }
    }
}
