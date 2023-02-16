using System.Collections.Generic;

namespace KinaUnaWeb.Models.ItemViewModels
{
    public class ContactListViewModel: BaseItemsViewModel
    {
        public List<ContactViewModel> ContactsList { get; set; }
        public string Tags { get; set; }
        public string TagFilter { get; set; }

        public ContactListViewModel()
        {
            ContactsList = new List<ContactViewModel>();
        }

        public ContactListViewModel(BaseItemsViewModel baseItemsViewModel)
        {
            SetBaseProperties(baseItemsViewModel);
            ContactsList = new List<ContactViewModel>();
        }
    }
}
