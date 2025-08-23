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
    public class SubtasksController(
        ISubtasksHttpClient subtasksHttpClient,
        ITodoItemsHttpClient todoItemsHttpClient,
        IViewModelSetupService viewModelSetupService,
        IUserInfosHttpClient userInfosHttpClient,
        IProgenyHttpClient progenyHttpClient) : Controller
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
                todoItemResponse.TodoItem.Progeny = await progenyHttpClient.GetProgeny(todoItemResponse.TodoItem.ProgenyId);
                todoItemResponse.TodoItemId = todoItemResponse.TodoItem.TodoItemId;

                BaseItemsViewModel baseModel = await viewModelSetupService.SetupViewModel(parameters.LanguageId, User.GetEmail(), todoItemResponse.TodoItem.ProgenyId);
                todoItemResponse.IsCurrentUserProgenyAdmin = baseModel.IsCurrentUserProgenyAdmin;
                UserInfo noteUserInfo = await userInfosHttpClient.GetUserInfoByUserId(todoItemResponse.TodoItem.CreatedBy);
                todoItemResponse.TodoItem.CreatedBy = noteUserInfo.FullName();
            }


            return PartialView("_SubtaskElementPartial", todoItemResponse);
        }

        [AllowAnonymous]
        public async Task<IActionResult> ViewSubtask(int todoId, bool partialView = false)
        {
            TodoItem subtask = await subtasksHttpClient.GetSubtask(todoId);
            BaseItemsViewModel baseModel = await viewModelSetupService.SetupViewModel(Request.GetLanguageIdFromCookie(), User.GetEmail(), subtask.ProgenyId);
            TodoViewModel model = new(baseModel)
            {
                TodoItem = subtask
            };

            model.TodoItem.Progeny = model.CurrentProgeny;
            model.TodoItem.Progeny.PictureLink = model.TodoItem.Progeny.GetProfilePictureUrl();
            UserInfo todoUserInfo = await userInfosHttpClient.GetUserInfoByUserId(model.TodoItem.CreatedBy);
            model.TodoItem.CreatedBy = todoUserInfo.FullName();
            model.SetStatusList(model.TodoItem.Status);
            if (partialView)
            {
                return PartialView("_SubtaskDetailsPartial", model);
            }

            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> AddSubtask()
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

            return PartialView("_AddSubtaskPartial", model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddSubtask(TodoViewModel model)
        {
            BaseItemsViewModel baseModel = await viewModelSetupService.SetupViewModel(Request.GetLanguageIdFromCookie(), User.GetEmail(), model.TodoItem.ProgenyId);
            model.SetBaseProperties(baseModel);

            List<Progeny> progAdminList = await progenyHttpClient.GetProgenyAdminList(model.CurrentUser.UserEmail);
            if (progAdminList.Count == 0)
            {
                // Todo: Show that no children are available to add subtask for.
                return RedirectToAction("Index", "Todos");
            }

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
            return PartialView("_SubtaskAddedPartial", model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddSubtaskInline(TodoItem todoItem)
        {
            TodoItem parentTodoItem = await todoItemsHttpClient.GetTodoItem(todoItem.ParentTodoItemId);
            BaseItemsViewModel baseModel = await viewModelSetupService.SetupViewModel(Request.GetLanguageIdFromCookie(), User.GetEmail(), parentTodoItem.ProgenyId);
            TodoViewModel model = new(baseModel);
            
            List<Progeny> progAdminList = await progenyHttpClient.GetProgenyAdminList(model.CurrentUser.UserEmail);
            if (progAdminList.Count == 0)
            {
                // Todo: Show that no children are available to add subtask for.
                return RedirectToAction("Index", "Todos");
            }
            todoItem.ProgenyId = parentTodoItem.ProgenyId;
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

            return Json(model.TodoItem);
        }

        [HttpGet]
        public async Task<IActionResult> EditSubtask(int itemId)
        {
            TodoItem subtask = await subtasksHttpClient.GetSubtask(itemId);
            BaseItemsViewModel baseModel = await viewModelSetupService.SetupViewModel(Request.GetLanguageIdFromCookie(), User.GetEmail(), subtask.ProgenyId);
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

            model.SetPropertiesFromTodoItem(subtask);

            model.SetAccessLevelList();
            model.SetStatusList(model.TodoItem.Status);

            return PartialView("_EditSubtaskPartial", model);
        }
        
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditSubtask(TodoViewModel model)
        {
            BaseItemsViewModel baseModel = await viewModelSetupService.SetupViewModel(Request.GetLanguageIdFromCookie(), User.GetEmail(), model.TodoItem.ProgenyId);
            model.SetBaseProperties(baseModel);

            if (!model.CurrentProgeny.IsInAdminList(model.CurrentUser.UserEmail))
            {
                return PartialView("_AccessDeniedPartial");
            }

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
            return PartialView("../Todos/_TodoDetailsPartial", model);
        }
        
        [HttpGet]
        public async Task<IActionResult> DeleteSubtask(int itemId)
        {
            TodoItem subtask = await subtasksHttpClient.GetSubtask(itemId);
            BaseItemsViewModel baseModel = await viewModelSetupService.SetupViewModel(Request.GetLanguageIdFromCookie(), User.GetEmail(), subtask.ProgenyId);
            TodoViewModel model = new(baseModel);

            if (!model.CurrentProgeny.IsInAdminList(model.CurrentUser.UserEmail))
            {
                // Todo: Show no access info.
                return RedirectToAction("Index", "Todos");
            }

            model.TodoItem = subtask;
            model.SetStatusList(model.TodoItem.Status);
            model.TodoItem.Progeny = model.CurrentProgeny;
            model.TodoItem.Progeny.PictureLink = model.TodoItem.Progeny.GetProfilePictureUrl();

            return PartialView("_DeleteSubtaskPartial", model);
        }
        
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteSubtask(TodoViewModel model)
        {
            TodoItem subtask = await subtasksHttpClient.GetSubtask(model.TodoItem.TodoItemId);
            BaseItemsViewModel baseModel = await viewModelSetupService.SetupViewModel(Request.GetLanguageIdFromCookie(), User.GetEmail(), subtask.ProgenyId);
            model.SetBaseProperties(baseModel);

            if (!model.CurrentProgeny.IsInAdminList(model.CurrentUser.UserEmail))
            {
                // Todo: Show no access info.
                return RedirectToAction("Index", "Todos");
            }

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

            
            return PartialView("../Todos/_TodoDetailsPartial", model);
        }

        [HttpGet]
        public async Task<IActionResult> CopySubtask(int itemId)
        {
            TodoItem subtask = await subtasksHttpClient.GetSubtask(itemId);
            BaseItemsViewModel baseModel = await viewModelSetupService.SetupViewModel(Request.GetLanguageIdFromCookie(), User.GetEmail(), subtask.ProgenyId);
            TodoViewModel model = new(baseModel);

            if (model.CurrentAccessLevel > subtask.AccessLevel)
            {
                return PartialView("_AccessDeniedPartial");
            }

            model.SetPropertiesFromTodoItem(subtask);

            if (User.Identity != null && User.Identity.IsAuthenticated && model.CurrentUser.UserId != null)
            {
                model.ProgenyList = await viewModelSetupService.GetProgenySelectList(model.CurrentUser);
                model.SetProgenyList();
            }

            model.SetAccessLevelList();
            model.SetStatusList(model.TodoItem.Status);

            return PartialView("_CopySubtaskPartial", model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CopySubtask(TodoViewModel model)
        {
            BaseItemsViewModel baseModel = await viewModelSetupService.SetupViewModel(Request.GetLanguageIdFromCookie(), User.GetEmail(), model.TodoItem.ProgenyId);
            model.SetBaseProperties(baseModel);

            if (!model.CurrentProgeny.IsInAdminList(model.CurrentUser.UserEmail))
            {
                return PartialView("_AccessDeniedPartial");
            }

            TodoItem copiedSubtask = model.CreateTodoItem();

            model.TodoItem = await subtasksHttpClient.AddSubtask(copiedSubtask);
            model.TodoItem.CreatedTime = TimeZoneInfo.ConvertTimeFromUtc(model.TodoItem.CreatedTime, TimeZoneInfo.FindSystemTimeZoneById(model.CurrentUser.Timezone));
            model.SetStatusList(model.TodoItem.Status);
            return PartialView("_SubtaskCopiedPartial", model);
        }

        [Authorize]
        [HttpGet]
        public async Task<IActionResult> SetSubtaskAsNotStarted(int todoId)
        {
            TodoItem subtask = await subtasksHttpClient.GetSubtask(todoId);
            BaseItemsViewModel baseModel = await viewModelSetupService.SetupViewModel(Request.GetLanguageIdFromCookie(), User.GetEmail(), subtask.ProgenyId);
            TodoViewModel model = new(baseModel);
            if (!model.CurrentProgeny.IsInAdminList(model.CurrentUser.UserEmail))
            {
                return Unauthorized("Access denied.");
            }

            subtask.CompletedDate = null;
            subtask.Status = (int)KinaUnaTypes.TodoStatusType.NotStarted;
            TodoItem result = await subtasksHttpClient.UpdateSubtask(subtask);

            return Json(result);
        }
        
        [Authorize]
        [HttpGet]
        public async Task<IActionResult> SetSubtaskAsInProgress(int todoId)
        {
            TodoItem subtask = await subtasksHttpClient.GetSubtask(todoId);
            BaseItemsViewModel baseModel = await viewModelSetupService.SetupViewModel(Request.GetLanguageIdFromCookie(), User.GetEmail(), subtask.ProgenyId);
            TodoViewModel model = new(baseModel);
            if (!model.CurrentProgeny.IsInAdminList(model.CurrentUser.UserEmail))
            {
                return Unauthorized("Access denied.");
            }

            subtask.CompletedDate = null;
            subtask.Status = (int)KinaUnaTypes.TodoStatusType.InProgress;
            TodoItem result = await subtasksHttpClient.UpdateSubtask(subtask);

            return Json(result);
        }
        
        [Authorize]
        [HttpGet]
        public async Task<IActionResult> SetSubtaskAsCompleted(int todoId)
        {
            TodoItem subtask = await subtasksHttpClient.GetSubtask(todoId);
            BaseItemsViewModel baseModel = await viewModelSetupService.SetupViewModel(Request.GetLanguageIdFromCookie(), User.GetEmail(), subtask.ProgenyId);
            TodoViewModel model = new(baseModel);
            if (!model.CurrentProgeny.IsInAdminList(model.CurrentUser.UserEmail))
            {
                return Unauthorized("Access denied.");
            }

            subtask.CompletedDate = DateTime.UtcNow;
            subtask.Status = (int)KinaUnaTypes.TodoStatusType.Completed;
            TodoItem result = await subtasksHttpClient.UpdateSubtask(subtask);

            return Json(result);
        }

        [Authorize]
        [HttpGet]
        public async Task<IActionResult> SetSubtaskAsCancelled(int todoId)
        {
            TodoItem subtask = await subtasksHttpClient.GetSubtask(todoId);
            BaseItemsViewModel baseModel = await viewModelSetupService.SetupViewModel(Request.GetLanguageIdFromCookie(), User.GetEmail(), subtask.ProgenyId);
            TodoViewModel model = new(baseModel);
            if (!model.CurrentProgeny.IsInAdminList(model.CurrentUser.UserEmail))
            {
                return Unauthorized("Access denied.");
            }

            subtask.CompletedDate = null;
            subtask.Status = (int)KinaUnaTypes.TodoStatusType.Cancelled;
            TodoItem result = await subtasksHttpClient.UpdateSubtask(subtask);

            return Json(result);
        }
    }
}
