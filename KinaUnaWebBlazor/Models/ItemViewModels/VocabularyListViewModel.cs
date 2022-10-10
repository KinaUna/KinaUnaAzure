using KinaUna.Data.Models;

namespace KinaUnaWebBlazor.Models.ItemViewModels
{
    public class VocabularyListViewModel: BaseViewModel
    {
        public List<VocabularyItemViewModel> VocabularyList { get; set; } = new List<VocabularyItemViewModel>();
        public Progeny Progeny { get; set; } = new Progeny();
        public bool IsAdmin { get; set; } = false;
        
    }
}
