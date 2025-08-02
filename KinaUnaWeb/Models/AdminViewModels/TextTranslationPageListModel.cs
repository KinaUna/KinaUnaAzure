using System.Collections.Generic;

namespace KinaUnaWeb.Models.AdminViewModels
{
    public class TextTranslationPageListModel
    {
        public string Page { get; set; } = "Layout";
        public List<TextTranslation> Translations { get; set; } = [];
        public List<KinaUnaLanguage> LanguagesList { get; set; } = [];
    }
}
