using Microsoft.AspNetCore.Mvc.Rendering;
using System;
using System.Collections.Generic;
using System.Text.Json;
using KinaUna.Data.Extensions;
using KinaUna.Data.Models.DTOs;

namespace KinaUnaWeb.Models.ItemViewModels
{
    public class KanbanBoardViewModel: BaseItemsViewModel
    {
        public KanbanBoard KanbanBoard { get; set; } = new();
        public List<SelectListItem> ProgenyList { get; set; }
        public List<SelectListItem> FamilyList { get; set; }
        public List<SelectListItem> CopyTodoItemsOptions { get; set; } =
        [
            new() { Value = "0", Text = "Copy to new item", Selected = true },
            new() { Value = "1", Text = "Copy reference to existing item" },
            new() { Value = "2", Text = "Do not copy" }
        ];

        public int CopyTodoItemsOption { get; set; } = 0;
        public bool DeleteTodoItems { get; set; }
        
        public KanbanBoardViewModel()
        {
            ProgenyList = [];
        }
        public KanbanBoardViewModel(BaseItemsViewModel baseItemsViewModel)
        {
            SetBaseProperties(baseItemsViewModel);
            ProgenyList = [];
        }
        
        public void SetProgenyList()
        {
            KanbanBoard.ProgenyId = CurrentProgenyId;
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

        public void SetFamilyList()
        {
            KanbanBoard.FamilyId = CurrentFamilyId;
            foreach (SelectListItem item in FamilyList)
            {
                if (item.Value == CurrentFamilyId.ToString())
                {
                    item.Selected = true;
                }
                else
                {
                    item.Selected = false;
                }
            }
        }

        internal KanbanBoard CreateKanbanBoard()
        {
            KanbanBoard kanbanBoard = new()
            {
                KanbanBoardId = KanbanBoard.KanbanBoardId,
                UId = KanbanBoard.UId,
                ProgenyId = KanbanBoard.ProgenyId,
                FamilyId = KanbanBoard.FamilyId,
                Title = KanbanBoard.Title,
                Description = KanbanBoard.Description,
                Columns = KanbanBoard.Columns,
                CreatedTime = KanbanBoard.CreatedTime,
                ModifiedTime = DateTime.UtcNow,
                CreatedBy = CurrentUser.UserId,
                ModifiedBy = CurrentUser.UserId,
                AccessLevel = KanbanBoard.AccessLevel,
                Tags = KanbanBoard.Tags,
                Context = KanbanBoard.Context,
                IsDeleted = false,
                ItemPermissionsDtoList = JsonSerializer.Deserialize<List<ItemPermissionDto>>(ItemPermissionsListAsString)
            };

            kanbanBoard.SetColumnsListFromColumns();
            
            return kanbanBoard;
        }

        internal void SetPropertiesFromKanbanBoard(KanbanBoard kanbanBoard)
        {
            KanbanBoard.AccessLevel = kanbanBoard.AccessLevel;
            KanbanBoard.Columns = kanbanBoard.Columns;
            KanbanBoard.CreatedBy = kanbanBoard.CreatedBy;
            KanbanBoard.CreatedTime = kanbanBoard.CreatedTime;
            KanbanBoard.Description = kanbanBoard.Description;
            KanbanBoard.KanbanBoardId = kanbanBoard.KanbanBoardId;
            KanbanBoard.ModifiedTime = kanbanBoard.ModifiedTime;
            KanbanBoard.ProgenyId = kanbanBoard.ProgenyId;
            KanbanBoard.FamilyId = kanbanBoard.FamilyId;
            KanbanBoard.Tags = kanbanBoard.Tags;
            KanbanBoard.Title = kanbanBoard.Title;
            KanbanBoard.UId = kanbanBoard.UId;
            KanbanBoard.Context = kanbanBoard.Context;
            KanbanBoard.IsDeleted = kanbanBoard.IsDeleted;
            KanbanBoard.ColumnsList = kanbanBoard.GetColumnsListFromColumns();
        }
    }
}
