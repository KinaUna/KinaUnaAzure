using System.Collections.Generic;

namespace KinaUnaWeb.Models.ItemViewModels
{
    public class SkillsListViewModel: BaseItemsViewModel
    {
        public List<SkillViewModel> SkillsList { get; init; }
        public int SkillId { get; set; }

        public SkillsListViewModel()
        {
            SkillsList = [];
        }

        public SkillsListViewModel(BaseItemsViewModel baseItemsViewModel)
        {
            SkillsList = [];
            SetBaseProperties(baseItemsViewModel);
        }
    }
}
