using System.Collections.Generic;

namespace KinaUnaWeb.Models.ProgeniesViewModels
{
    public class ProgenySelectorViewModel: BaseItemsViewModel
    {
        public List<Progeny> Progenies { get; set; }

        public ProgenySelectorViewModel()
        {
            Progenies = [];
        }

        public ProgenySelectorViewModel(BaseItemsViewModel baseItemsViewModel)
        {
            SetBaseProperties(baseItemsViewModel);
            Progenies = [];
        }
    }
}
