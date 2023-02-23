using System.Collections.Generic;

namespace KinaUnaWeb.Models.ItemViewModels
{
    public class SkillsListViewModel: BaseItemsViewModel
    {
        public List<SkillViewModel> SkillsList { get; set; }
        
        public SkillsListViewModel()
        {
            SkillsList = new List<SkillViewModel>();
        }

        public SkillsListViewModel(BaseItemsViewModel baseItemsViewModel)
        {
            SkillsList = new List<SkillViewModel>();
            SetBaseProperties(baseItemsViewModel);
        }
    }
}
