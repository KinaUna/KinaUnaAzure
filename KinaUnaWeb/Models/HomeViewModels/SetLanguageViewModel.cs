using System.Collections.Generic;
using KinaUna.Data.Models;

namespace KinaUnaWeb.Models.HomeViewModels
{
    public class SetLanguageIdViewModel
    {
        public List<KinaUnaLanguage> LanguageList { get; set; }
        public int SelectedId { get; set; }
        public string ReturnUrl { get; set; } = string.Empty;
    }

    public class UserMenuViewModel
    {
        public int LanguageId { get; set; }
        public UserInfo UserInfo { get; set; }
    }
}
