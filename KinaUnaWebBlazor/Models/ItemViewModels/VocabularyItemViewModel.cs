using KinaUna.Data.Models;

namespace KinaUnaWebBlazor.Models.ItemViewModels
{
    public class VocabularyItemViewModel: BaseViewModel
    {
        public int WordId { get; set; } = 0;
        public string Word { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Language { get; set; } = string.Empty;
        public string SoundsLike { get; set; } = string.Empty;
        public DateTime? Date { get; set; }
        public DateTime DateAdded { get; set; } = DateTime.UtcNow;
        public string Author { get; set; } = string.Empty;
        public int ProgenyId { get; set; } = 0;
        public Progeny Progeny { get; set; } = new Progeny();
        public bool IsAdmin { get; set; } = false;
        public int AccessLevel { get; set; } = 5;
        public VocabularyItem VocabularyItem { get; set; } = new VocabularyItem();
    }

    public class WordDateCount
    {
        public DateTime WordDate { get; set; } = DateTime.UtcNow;
        public int WordCount { get; set; } = 0;
    }
}
