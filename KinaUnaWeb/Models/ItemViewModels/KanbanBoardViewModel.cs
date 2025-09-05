using Microsoft.AspNetCore.Mvc.Rendering;
using System;
using System.Collections.Generic;
using KinaUna.Data.Extensions;

namespace KinaUnaWeb.Models.ItemViewModels
{
    public class KanbanBoardViewModel: BaseItemsViewModel
    {
        public KanbanBoard KanbanBoard { get; set; } = new();
        public List<SelectListItem> ProgenyList { get; set; }
        public List<SelectListItem> AccessLevelListEn { get; set; }
        public KanbanBoardViewModel()
        {
            ProgenyList = [];
        }
        public KanbanBoardViewModel(BaseItemsViewModel baseItemsViewModel)
        {
            SetBaseProperties(baseItemsViewModel);
            SetAccessLevelList();
            ProgenyList = [];
        }

        public void SetAccessLevelList()
        {
            AccessLevelList accessLevelList = new();
            AccessLevelListEn = accessLevelList.AccessLevelListEn;
            AccessLevelListEn[KanbanBoard.AccessLevel].Selected = true;
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

        internal KanbanBoard CreateKanbanBoard()
        {
            KanbanBoard kanbanBoard = new()
            {
                KanbanBoardId = KanbanBoard.KanbanBoardId,
                UId = KanbanBoard.UId,
                ProgenyId = KanbanBoard.ProgenyId,
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
                IsDeleted = false
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
            KanbanBoard.Tags = kanbanBoard.Tags;
            KanbanBoard.Title = kanbanBoard.Title;
            KanbanBoard.UId = kanbanBoard.UId;
            KanbanBoard.Context = kanbanBoard.Context;
            KanbanBoard.IsDeleted = kanbanBoard.IsDeleted;
            KanbanBoard.ColumnsList = kanbanBoard.GetColumnsListFromColumns();
        }
    }
}
