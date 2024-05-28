using KinaUna.Data.Models;

namespace KinaUnaWebBlazor.Models.AdminViewModels
{
    public class ManageKinaUnaTextsViewModel : BaseViewModel
    {

        public List<KinaUnaText> Texts { get; set; } = [];
        public List<string> PagesList { get; set; } = [];
        public List<string> TitlesList { get; set; } = [];
        public List<KinaUnaLanguage> LanguagesList { get; set; } = [];
        public int Language { get; set; } = 1;
        public KinaUnaText KinaUnaText { get; set; } = new();
        public int MessageId { get; set; } = 0;
    }
}
