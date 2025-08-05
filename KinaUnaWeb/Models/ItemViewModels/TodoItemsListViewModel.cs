using System;
using System.Collections.Generic;
using System.Linq;
using KinaUnaWeb.Models.TypeScriptModels.TodoItems;

namespace KinaUnaWeb.Models.ItemViewModels
{
    public class TodoItemsListViewModel: BaseItemsViewModel
    {
        public List<TodoItem> TodoItemsList { get; set; }
        public TodoItemsPageParameters TodoItemsPageParameters { get; init; }
        
        public int PopUpTodoItemId = 0;

        public TodoItemsListViewModel(BaseItemsViewModel baseItemsViewModel)
        {
            SetBaseProperties(baseItemsViewModel);

            TodoItemsList = [];

            TodoItemsPageParameters = new TodoItemsPageParameters
            {
                ProgenyId = CurrentProgenyId,
                CurrentPageNumber = 0,
                ItemsPerPage = 10,
                TotalPages = 0,
                TotalItems = 0,
                LanguageId = LanguageId,
                TagFilter = "",
                Sort = 1
            };
        }
        
        public void SetTodoItemsList(List<TodoItem> todoItemsList)
        {
            todoItemsList = [.. todoItemsList.OrderBy(t => t.DueDate).ThenBy(t => t.CreatedTime)];
            TodoItemsList = [];

            foreach (TodoItem todoItem in todoItemsList)
            {
                if (todoItem.AccessLevel != (int)AccessLevel.Public && todoItem.AccessLevel < CurrentAccessLevel) continue;

                if (todoItem.DueDate.HasValue)
                {
                    todoItem.DueDate = TimeZoneInfo.ConvertTimeFromUtc(todoItem.DueDate.Value,
                        TimeZoneInfo.FindSystemTimeZoneById(CurrentUser.Timezone));
                }

                if (todoItem.StartDate.HasValue)
                {
                    todoItem.StartDate = TimeZoneInfo.ConvertTimeFromUtc(todoItem.StartDate.Value,
                        TimeZoneInfo.FindSystemTimeZoneById(CurrentUser.Timezone));
                }

                if (todoItem.CompletedDate.HasValue)
                {
                    todoItem.CompletedDate = TimeZoneInfo.ConvertTimeFromUtc(todoItem.CompletedDate.Value,
                        TimeZoneInfo.FindSystemTimeZoneById(CurrentUser.Timezone));
                }

                todoItem.CreatedTime = TimeZoneInfo.ConvertTimeFromUtc(todoItem.CreatedTime,
                    TimeZoneInfo.FindSystemTimeZoneById(CurrentUser.Timezone));

                TodoItemsList.Add(todoItem);
            }
        }
    }
}
