using System.Collections.Generic;

namespace KinaUnaWeb.Models.ItemViewModels
{
    public class ContactListViewModel: BaseItemsViewModel
    {
        public List<ContactViewModel> ContactsList { get; set; }
        public string TagFilter { get; set; }

        public ContactListViewModel()
        {
            ContactsList = [];
        }

        public ContactListViewModel(BaseItemsViewModel baseItemsViewModel)
        {
            SetBaseProperties(baseItemsViewModel);
            ContactsList = [];
        }
    }
}
