namespace KinaUna.Data.Models
{
    public class TextTranslation
    {
        public int Id { get; set; }
        public string Page { get; set; } = string.Empty;
        public string Word { get; set; } = string.Empty;
        public int LanguageId { get; set; }
        public string Translation { get; set; } = string.Empty;
    }
}
