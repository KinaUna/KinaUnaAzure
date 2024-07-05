using System.Collections.Generic;
using KinaUnaWeb.Models.TypeScriptModels.Contacts;

namespace KinaUnaWeb.Models.ItemViewModels
{
    public class ContactListViewModel: BaseItemsViewModel
    {
        public List<ContactViewModel> ContactsList { get; set; }
        public string TagFilter { get; set; }
        public ContactsPageParameters ContactsPageParameters { get; set; }

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
