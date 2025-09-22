using Microsoft.AspNetCore.Mvc.Rendering;
using System;
using System.Collections.Generic;

namespace KinaUnaWeb.Models.ItemViewModels
{
    public class TodoViewModel: BaseItemsViewModel
    {
        public TodoItem TodoItem { get; set; } = new();
        public List<SelectListItem> ProgenyList { get; set; }
        public List<SelectListItem> AccessLevelListEn { get; set; }
        public List<SelectListItem> StatusList { get; set; }
        public int CopyFromTodoId { get; set; } = 0;
        public bool CopySubtasks { get; set; } = false;
        public List<KanbanItem> KanbanItems { get; set; } = [];
        public List<KanbanBoard> KanbanBoards { get; set; } = [];
        public List<SelectListItem> KanbanBoardsList { get; set; } = [];
        public int AddToKanbanBoardId { get; set; } = 0;
        public TodoViewModel()
        {
            ProgenyList = [];
        }

        public TodoViewModel(BaseItemsViewModel baseItemsViewModel)
        {
            SetBaseProperties(baseItemsViewModel);
            SetAccessLevelList();
            SetStatusList(0);
            ProgenyList = [];
        }

        public void SetStatusList(int selectedStatus)
        {
            SelectListItem notStartedStatus = new()
            {
                Text = "Not Started",
                Value = "0"
            };

            SelectListItem inProgressStatus = new()
            {
                Text = "In Progress",
                Value = "1"
            };

            SelectListItem completedStatus = new()
            {
                Text = "Completed",
                Value = "2"
            };

            SelectListItem cancelledStatus = new()
            {
                Text = "Cancelled",
                Value = "3"
            };

            SelectListItem overdueStatus = new()
            {
                Text = "Overdue",
                Value = "4"
            };

            StatusList =
            [
                notStartedStatus,
                inProgressStatus,
                completedStatus,
                cancelledStatus,
                overdueStatus
            ];

            foreach (SelectListItem item in StatusList)
            {
                if (item.Value == selectedStatus.ToString())
                {
                    item.Selected = true;
                }
                else
                {
                    item.Selected = false;
                }
            }
        }

        public void SetAccessLevelList()
        {
            AccessLevelList accessLevelList = new();
            AccessLevelListEn = accessLevelList.AccessLevelListEn;
            AccessLevelListEn[TodoItem.AccessLevel].Selected = true;
        }

        public void SetProgenyList()
        {
            TodoItem.ProgenyId = CurrentProgenyId;
            foreach (SelectListItem item in ProgenyList)
            {
                if (item.Value == CurrentProgenyId.ToString())
                {
                    item.Selected = true;
                }
                else
                {
                    item.Selected = false;
                }
            }
        }

        public void SetPropertiesFromTodoItem(TodoItem todoItem)
        {
            TodoItem.TodoItemId = todoItem.TodoItemId;
            TodoItem.ParentTodoItemId = todoItem.ParentTodoItemId;
            TodoItem.ProgenyId = todoItem.ProgenyId;
            TodoItem.Description = todoItem.Description;
            TodoItem.AccessLevel = todoItem.AccessLevel;
            TodoItem.Context = todoItem.Context;
            TodoItem.Location = todoItem.Location;
            TodoItem.Title = todoItem.Title;
            TodoItem.CreatedTime = TimeZoneInfo.ConvertTimeFromUtc(todoItem.CreatedTime, TimeZoneInfo.FindSystemTimeZoneById(CurrentUser.Timezone));
            TodoItem.CreatedBy = todoItem.CreatedBy;
            TodoItem.StartDate = todoItem.StartDate.HasValue ? TimeZoneInfo.ConvertTimeFromUtc(todoItem.StartDate.Value, TimeZoneInfo.FindSystemTimeZoneById(CurrentUser.Timezone)) : null;
            TodoItem.DueDate = todoItem.DueDate.HasValue ? TimeZoneInfo.ConvertTimeFromUtc(todoItem.DueDate.Value, TimeZoneInfo.FindSystemTimeZoneById(CurrentUser.Timezone)) : null;
            TodoItem.CompletedDate = todoItem.CompletedDate.HasValue ? TimeZoneInfo.ConvertTimeFromUtc(todoItem.CompletedDate.Value, TimeZoneInfo.FindSystemTimeZoneById(CurrentUser.Timezone)) : null;
            TodoItem.Notes = todoItem.Notes;
            TodoItem.Status = todoItem.Status;
            TodoItem.Tags = todoItem.Tags;
            TodoItem.UId = todoItem.UId;
            TodoItem.Progeny = todoItem.Progeny;
        }

        /// <summary>
        /// Creates a new <see cref="TodoItem"/> instance based on the current state of the application.
        /// </summary>
        /// <remarks>This method initializes a <see cref="TodoItem"/> object using the current user's
        /// timezone and other contextual data. The created <see cref="TodoItem"/> includes properties such as
        /// description, title, and timestamps converted to UTC.</remarks>
        /// <returns>A new <see cref="TodoItem"/> instance populated with the relevant data.</returns>
        public TodoItem CreateTodoItem()
        {
            TodoItem todoItem = new()
            {
                TodoItemId = TodoItem.TodoItemId,
                ParentTodoItemId = TodoItem.ParentTodoItemId,
                ProgenyId = CurrentProgenyId,
                Description = TodoItem.Description,
                AccessLevel = TodoItem.AccessLevel,
                Context = TodoItem.Context,
                Title = TodoItem.Title,
                CreatedTime = TimeZoneInfo.ConvertTimeToUtc(TodoItem.CreatedTime, TimeZoneInfo.FindSystemTimeZoneById(CurrentUser.Timezone)),
                CreatedBy = CurrentUser.UserId,
                StartDate = TodoItem.StartDate.HasValue ? TimeZoneInfo.ConvertTimeToUtc(TodoItem.StartDate.Value.Date + TimeSpan.FromHours(12), TimeZoneInfo.FindSystemTimeZoneById(CurrentUser.Timezone)) : null,
                DueDate = TodoItem.DueDate.HasValue ? TimeZoneInfo.ConvertTimeToUtc(TodoItem.DueDate.Value.Date + TimeSpan.FromHours(12), TimeZoneInfo.FindSystemTimeZoneById(CurrentUser.Timezone)) : null,
                CompletedDate = TodoItem.CompletedDate.HasValue ? TimeZoneInfo.ConvertTimeToUtc(TodoItem.CompletedDate.Value.Date + TimeSpan.FromHours(12), TimeZoneInfo.FindSystemTimeZoneById(CurrentUser.Timezone)) : null,
                Notes = TodoItem.Notes,
                Status = TodoItem.Status,
                Tags = TodoItem.Tags,
                Location = TodoItem.Location,
                UId = TodoItem.UId,
                Progeny = TodoItem.Progeny
            };

            return todoItem;
        }
    }
}
