using KinaUna.Data.Models;

namespace KinaUnaWebBlazor.Models.ItemViewModels
{
    public class SkillsListViewModel: BaseViewModel
    {
        public List<SkillViewModel> SkillsList { get; set; } = [];
        public Progeny Progeny { get; set; } = new();
        public bool IsAdmin { get; set; } = false;
    }
}
