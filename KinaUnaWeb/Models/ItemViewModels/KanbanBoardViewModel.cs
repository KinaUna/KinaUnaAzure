using Microsoft.AspNetCore.Mvc.Rendering;
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
    }
}
