using System.Collections.Generic;

namespace KinaUnaWeb.Models.ItemViewModels
{
    public class SkillsListViewModel: BaseItemsViewModel
    {
        public List<SkillViewModel> SkillsList { get; init; }
        public int SkillId { get; set; }

        /// <summary>
        /// Parameterless constructor. Needed for initialization of the view model when objects are created in Razor views/passed as parameters in POST methods.
        /// </summary>
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
