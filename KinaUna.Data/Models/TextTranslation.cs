using System.ComponentModel.DataAnnotations;

namespace KinaUna.Data.Models
{
    /// <summary>
    /// Entity Framework Entity for TextTranslation.
    /// </summary>
    public class TextTranslation
    {
        public int Id { get; set; }

        [MaxLength(256)]
        public string Page { get; set; } = string.Empty;

        [MaxLength(4096)]
        public string Word { get; set; } = string.Empty;
        public int LanguageId { get; set; }

        [MaxLength(4096)]
        public string Translation { get; set; } = string.Empty;
    }
}
