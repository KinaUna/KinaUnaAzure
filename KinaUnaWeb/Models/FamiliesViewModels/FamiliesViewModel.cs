using System.Collections.Generic;
using KinaUna.Data.Models.Family;

namespace KinaUnaWeb.Models.FamiliesViewModels
{
    public class FamiliesViewModel : BaseItemsViewModel
    {
        public FamiliesViewModel(BaseItemsViewModel baseItemsViewModel)
        {
            SetBaseProperties(baseItemsViewModel);
        }

        public List<Family> Families { get; set; } = new List<Family>();

    }
}
