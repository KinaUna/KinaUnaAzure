using KinaUna.Data.Models;

namespace KinaUnaWebBlazor.Models.AdminViewModels
{
    public class ManageTranslationsViewModel
    {
        public List<TextTranslation> Translations { get; set; } = [];
        public List<string> PagesList { get; set; } = [];
        public List<string> WordsList { get; set; } = [];
        public List<KinaUnaLanguage> LanguagesList { get; set; } = [];
    }
}
