using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace KinaUna.Data.Models
{
    public class KinaUnaText
    {
        public int Id { get; set; }
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

    public class KinaUnaTextNumber
    {
        [Key] public int Id { get; set; }
        public int DefaultLanguage { get; set; }
    }
}

