using System;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Collections.Generic;

namespace KinaUnaWeb.Models.ItemViewModels
{
    public class KanbanItemViewModel : BaseItemsViewModel
    {
        public KanbanItem KanbanItem { get; set; } = new();
        public KanbanBoard KanbanBoard { get; set; } = new();
        public List<SelectListItem> ProgenyList { get; set; }
        public List<SelectListItem> StatusList { get; set; }
        public List<SelectListItem> AccessLevelListEn { get; set; }
        public List<KanbanBoard> KanbanBoards { get; set; } = [];
        public List<SelectListItem> KanbanBoardsList { get; set; } = [];
        public TodoItem ParentTodoItem { get; set; } = null;
        public int TodoItemReference { get; set; } = 0;
        public KanbanItemViewModel()
        {
            ProgenyList = [];
            KanbanItem.TodoItem = new TodoItem();
        }

        public KanbanItemViewModel(BaseItemsViewModel baseItemsViewModel)
        {
            SetBaseProperties(baseItemsViewModel);
            SetAccessLevelList();
            ProgenyList = [];
            KanbanItem.TodoItem = new TodoItem();
        }

        public void SetAccessLevelList()
        {
            AccessLevelList accessLevelList = new();
            AccessLevelListEn = accessLevelList.AccessLevelListEn;
            AccessLevelListEn[KanbanItem.TodoItem?.AccessLevel ?? 0].Selected = true;
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

        public void SetProgenyList()
        {
            KanbanItem.TodoItem.ProgenyId = CurrentProgenyId;
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
                TodoItemId = KanbanItem.TodoItem.TodoItemId,
                ParentTodoItemId = KanbanItem.TodoItem.ParentTodoItemId,
                ProgenyId = KanbanItem.TodoItem.ProgenyId,
                Description = KanbanItem.TodoItem.Description,
                AccessLevel = KanbanItem.TodoItem.AccessLevel,
                Context = KanbanItem.TodoItem.Context,
                Title = KanbanItem.TodoItem.Title,
                CreatedTime = TimeZoneInfo.ConvertTimeToUtc(KanbanItem.TodoItem.CreatedTime, TimeZoneInfo.FindSystemTimeZoneById(CurrentUser.Timezone)),
                CreatedBy = CurrentUser.UserId,
                StartDate = KanbanItem.TodoItem.StartDate.HasValue ? TimeZoneInfo.ConvertTimeToUtc(KanbanItem.TodoItem.StartDate.Value.Date + TimeSpan.FromHours(12), TimeZoneInfo.FindSystemTimeZoneById(CurrentUser.Timezone)) : null,
                DueDate = KanbanItem.TodoItem.DueDate.HasValue ? TimeZoneInfo.ConvertTimeToUtc(KanbanItem.TodoItem.DueDate.Value.Date + TimeSpan.FromHours(12), TimeZoneInfo.FindSystemTimeZoneById(CurrentUser.Timezone)) : null,
                CompletedDate = KanbanItem.TodoItem.CompletedDate.HasValue
                    ? TimeZoneInfo.ConvertTimeToUtc(KanbanItem.TodoItem.CompletedDate.Value.Date + TimeSpan.FromHours(12), TimeZoneInfo.FindSystemTimeZoneById(CurrentUser.Timezone))
                    : null,
                Notes = KanbanItem.TodoItem.Notes,
                Status = KanbanItem.TodoItem.Status,
                Tags = KanbanItem.TodoItem.Tags,
                Location = KanbanItem.TodoItem.Location,
                UId = KanbanItem.TodoItem.UId,
            };

            return todoItem;
        }
    }
}
