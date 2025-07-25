using System.Collections.Generic;

namespace KinaUnaWeb.Models.AdminViewModels
{
    public class ManageKinaUnaTextsViewModel : BaseViewModel
    {

        public List<KinaUnaText> Texts { get; init; } = [];
        public List<string> PagesList { get; init; } = [];
        public List<string> TitlesList { get; init; } = [];
        public List<KinaUnaLanguage> LanguagesList { get; set; } = [];
        public int Language { get; set; }
        public KinaUnaText KinaUnaText { get; init; }
        public int MessageId { get; init; }
        public string Text { get; init; }
    }
}
