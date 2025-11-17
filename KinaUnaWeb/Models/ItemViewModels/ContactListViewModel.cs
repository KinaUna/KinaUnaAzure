using System.Collections.Generic;
using KinaUnaWeb.Models.TypeScriptModels.Contacts;

namespace KinaUnaWeb.Models.ItemViewModels
{
    public class ContactListViewModel: BaseItemsViewModel
    {
        public List<ContactViewModel> ContactsList { get; set; }
        public string TagFilter { get; set; }
        public ContactsPageParameters ContactsPageParameters { get; set; }
        public int ContactId {get; set; }

        /// <summary>
        /// Parameterless constructor. Needed for initialization of the view model when objects are created in Razor views/passed as parameters in POST methods.
        /// </summary>
        public ContactListViewModel()
        {
            ContactsList = [];
            ProgenyList = [];
            FamilyList = [];
        }

        public ContactListViewModel(BaseItemsViewModel baseItemsViewModel)
        {
            SetBaseProperties(baseItemsViewModel);
            ContactsList = [];
            ProgenyList = [];
            FamilyList = [];
        }
    }
}
