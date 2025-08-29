using Microsoft.AspNetCore.Mvc.Rendering;
using System;
using System.Collections.Generic;

namespace KinaUnaWeb.Models.ItemViewModels
{
    public class KanbanBoardViewModel: BaseItemsViewModel
    {
        public KanbanBoard KanbanBoard { get; set; }
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

            return kanbanBoard;
        }

        internal void SetPropertiesFromKanbanBoard(KanbanBoard kanbanBoard)
        {
            throw new NotImplementedException();
        }
    }
}
