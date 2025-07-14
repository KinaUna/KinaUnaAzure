using KinaUna.Data.Models;

namespace KinaUna.OpenIddict.Models.HomeViewModels
{
    public class SetLanguageIdViewModel
    {
        public required List<KinaUnaLanguage>? LanguageList { get; init; }
        public int SelectedId { get; init; }
        public string ReturnUrl { get; set; } = string.Empty;
    }
}
