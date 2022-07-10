using System.Collections.Generic;
using KinaUna.Data.Models;

namespace KinaUna.IDP.Models.HomeViewModels
{
    public class SetLanguageIdViewModel
    {
        public List<KinaUnaLanguage> LanguageList { get; set; }
        public int SelectedId { get; set; }
        public string ReturnUrl { get; set; } = string.Empty;
    }
}
