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

namespace KinaUnaWeb.Controllers
{
    public class TodosController(IViewModelSetupService viewModelSetupService, IUserInfosHttpClient userInfosHttpClient, IProgenyHttpClient progenyHttpClient) : Controller
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
                model.TodoItemsList.Add(await todoItemsHttpClient.GetCalendarItem(model.PopUpTodoItemId));
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
    }
}
