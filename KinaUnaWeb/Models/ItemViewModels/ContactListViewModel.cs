using System.Collections.Generic;
using KinaUna.Data.Models;

namespace KinaUnaWeb.Models.ItemViewModels
{
    public class ContactListViewModel: BaseViewModel
    {
        public List<ContactViewModel> ContactsList { get; set; }
        public Progeny Progeny { get; set; }
        public bool IsAdmin { get; set; }
        public string Tags { get; set; }
        public string TagFilter { get; set; }
        public ContactListViewModel()
        {
            ContactsList = new List<ContactViewModel>();
        }
    }
}
