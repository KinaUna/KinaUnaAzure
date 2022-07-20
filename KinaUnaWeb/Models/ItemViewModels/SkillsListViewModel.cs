using System.Collections.Generic;
using KinaUna.Data.Models;

namespace KinaUnaWeb.Models.ItemViewModels
{
    public class SkillsListViewModel: BaseViewModel
    {
        public List<SkillViewModel> SkillsList { get; set; }
        public Progeny Progeny { get; set; }
        public bool IsAdmin { get; set; }

        public SkillsListViewModel()
        {
            SkillsList = new List<SkillViewModel>();
        }
    }
}
