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
    }
}
