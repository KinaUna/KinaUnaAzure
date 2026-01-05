using System.Collections.Generic;
using KinaUna.Data.Models.Support;

namespace KinaUnaWeb.Models.AdminViewModels
{
    public class ManageHelpPagesViewModel
    {
        public List<HelpContent> HelpPages { get; set; } = [];
        public List<string> Pages { get; set; } = new List<string>();

    }
}
