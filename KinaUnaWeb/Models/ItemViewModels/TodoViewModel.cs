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
        public List<SelectListItem> AccessLevelListDa { get; set; }
        public List<SelectListItem> AccessLevelListDe { get; set; }
        public List<SelectListItem> StatusList { get; set; }

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
            AccessLevelListDa = accessLevelList.AccessLevelListDa;
            AccessLevelListDe = accessLevelList.AccessLevelListDe;

            AccessLevelListEn[TodoItem.AccessLevel].Selected = true;
            AccessLevelListDa[TodoItem.AccessLevel].Selected = true;
            AccessLevelListDe[TodoItem.AccessLevel].Selected = true;

            if (LanguageId == 2)
            {
                AccessLevelListEn = AccessLevelListDe;
            }

            if (LanguageId == 3)
            {
                AccessLevelListEn = AccessLevelListDa;
            }
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
            TodoItem.ProgenyId = todoItem.ProgenyId;
            TodoItem.Description = todoItem.Description;
            TodoItem.AccessLevel = todoItem.AccessLevel;
            TodoItem.Context = todoItem.Context;
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

        public TodoItem CreateTodoItem()
        {
            TodoItem todoItem = new()
            {
                ProgenyId = CurrentProgenyId,
                Description = TodoItem.Description,
                AccessLevel = TodoItem.AccessLevel,
                Context = TodoItem.Context,
                Title = TodoItem.Title,
                CreatedTime = TimeZoneInfo.ConvertTimeToUtc(TodoItem.CreatedTime, TimeZoneInfo.FindSystemTimeZoneById(CurrentUser.Timezone)),
                CreatedBy = CurrentUser.UserId,
                StartDate = TodoItem.StartDate.HasValue ? TimeZoneInfo.ConvertTimeToUtc(TodoItem.StartDate.Value, TimeZoneInfo.FindSystemTimeZoneById(CurrentUser.Timezone)) : null,
                DueDate = TodoItem.DueDate.HasValue ? TimeZoneInfo.ConvertTimeToUtc(TodoItem.DueDate.Value, TimeZoneInfo.FindSystemTimeZoneById(CurrentUser.Timezone)) : null,
                CompletedDate = TodoItem.CompletedDate.HasValue ? TimeZoneInfo.ConvertTimeToUtc(TodoItem.CompletedDate.Value, TimeZoneInfo.FindSystemTimeZoneById(CurrentUser.Timezone)) : null,
                Notes = TodoItem.Notes,
                Status = TodoItem.Status,
                Tags = TodoItem.Tags,
                UId = TodoItem.UId,
                Progeny = TodoItem.Progeny
            };

            return todoItem;
        }
    }
}
