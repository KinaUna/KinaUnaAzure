using KinaUna.Data.Models;

namespace KinaUnaWebBlazor.Models.ItemViewModels
{
    public class SkillsListViewModel: BaseViewModel
    {
        public List<SkillViewModel> SkillsList { get; set; } = new List<SkillViewModel>();
        public Progeny Progeny { get; set; } = new Progeny();
        public bool IsAdmin { get; set; } = false;
    }
}
