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
    }
}
