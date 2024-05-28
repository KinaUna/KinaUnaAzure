using System.Collections.Generic;
using KinaUna.Data.Models;

namespace KinaUnaWeb.Models.HomeViewModels
{
    public class SetLanguageIdViewModel
    {
        public List<KinaUnaLanguage> LanguageList { get; init; }
        public int SelectedId { get; init; }
        public string ReturnUrl { get; set; } = string.Empty;
    }

    public class UserMenuViewModel
    {
        public int LanguageId { get; init; }
        public UserInfo UserInfo { get; init; }
    }
}
