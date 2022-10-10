using KinaUna.Data.Models;

namespace KinaUnaWebBlazor.Models.HomeViewModels
{
    public class SetLanguageIdViewModel
    {
        public List<KinaUnaLanguage> LanguageList { get; set; }
        public int SelectedId { get; set; }
        public string ReturnUrl { get; set; }

        public SetLanguageIdViewModel()
        {
            LanguageList = new List<KinaUnaLanguage>();
            SelectedId = 1;
            ReturnUrl = string.Empty;
        }
    }
}
