using System.Collections.Generic;

namespace KinaUnaWeb.Models.TypeScriptModels.Contacts
{
    public class ContactsPageResponse
    {
        public int PageNumber { get; set; }
        public int TotalPages { get; set; }
        public int TotalItems { get; set; }
        public List<int> ContactsList { get; set; } = [];
        public List<string> TagsList { get; set; } = [];
    }
}
