using KinaUna.Data.Models;

namespace KinaUnaWebBlazor.Models.HomeViewModels
{
    public class SetLanguageIdViewModel
    {
        public List<KinaUnaLanguage>? LanguageList { get; set; } = [];
        public int SelectedId { get; set; } = 1;
        public string ReturnUrl { get; set; } = string.Empty;
    }
}
