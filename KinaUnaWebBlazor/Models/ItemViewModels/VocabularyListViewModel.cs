using KinaUna.Data.Models;

namespace KinaUnaWebBlazor.Models.ItemViewModels
{
    public class VocabularyListViewModel: BaseViewModel
    {
        public List<VocabularyItemViewModel> VocabularyList { get; set; } = [];
        public Progeny Progeny { get; set; } = new();
        public bool IsAdmin { get; set; } = false;
        
    }
}
