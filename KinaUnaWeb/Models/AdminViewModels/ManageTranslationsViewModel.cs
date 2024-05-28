using System.Collections.Generic;
using KinaUna.Data.Models;

namespace KinaUnaWeb.Models.AdminViewModels
{
    public class ManageTranslationsViewModel
    {
        public List<TextTranslation> Translations { get; init; } = [];
        public List<string> PagesList { get; init; } = [];
        public List<string> WordsList { get; init; } = [];
        public List<KinaUnaLanguage> LanguagesList { get; set; } = [];
    }
}
