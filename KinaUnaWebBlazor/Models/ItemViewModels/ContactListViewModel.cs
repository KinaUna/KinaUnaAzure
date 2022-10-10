using KinaUna.Data.Models;

namespace KinaUnaWebBlazor.Models.ItemViewModels
{
    public class ContactListViewModel: BaseViewModel
    {
        public List<ContactViewModel> ContactsList { get; set; } = new List<ContactViewModel>();
        public Progeny Progeny { get; set; } = new Progeny();
        public bool IsAdmin { get; set; } = false;
        public string Tags { get; set; } = "";
        public string TagFilter { get; set; } = "";
        
    }
}
