using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace KinaUna.Data.Models
{
    /// <summary>
    /// Entity Framework Entity for text data.
    /// TextId is the same for all translations of the same text.
    /// </summary>
    public class KinaUnaText
    {
        public int Id { get; init; }
        public int TextId { get; set; }
        public int LanguageId { get; set; }
        public string Page { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Text { get; set; } = string.Empty;
        public DateTime Created { get; set; }
        public DateTime Updated { get; set; }

        [NotMapped]
        public string ReturnUrl { get; set; }
    }

    /// <summary>
    /// Entity Framework Entity for TextIds used by KinaUnaText entities.
    /// </summary>

    public class KinaUnaTextNumber
    {
        [Key] public int Id { get; init; }
        public int DefaultLanguage { get; set; }
    }
}

