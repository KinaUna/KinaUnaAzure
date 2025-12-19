using System;

namespace KinaUna.Data.Models.Support
{
    public class HelpContent
    {
        public int Id { get; set; }
        public string Page { get; set; } = string.Empty;
        public string Element { get; set; } = string.Empty;
        public int LanguageId { get; set; } = 1;
        public string Title { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public DateTime CreatedTime { get; set; }
        public DateTime UpdatedTime { get; set; }
    }
}
