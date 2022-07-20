using System.Collections.Generic;
using KinaUna.Data.Models;

namespace KinaUnaWeb.Models.ItemViewModels
{
    public class VocabularyListViewModel: BaseViewModel
    {
        public List<VocabularyItemViewModel> VocabularyList { get; set; }
        public Progeny Progeny { get; set; }
        public bool IsAdmin { get; set; }

        public VocabularyListViewModel()
        {
            VocabularyList = new List<VocabularyItemViewModel>();
        }
    }
}
