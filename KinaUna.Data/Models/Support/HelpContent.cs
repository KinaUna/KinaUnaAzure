using System;
using System.ComponentModel.DataAnnotations;

namespace KinaUna.Data.Models.Support
{
    /// <summary>
    /// Represents localized help content associated with a specific page element in the application.
    /// </summary>
    /// <remarks>Use this class to store and retrieve help information, such as titles and descriptive
    /// content, for user interface elements. Each instance is linked to a particular page, element, and language,
    /// enabling support for multilingual help documentation. The class also tracks creation and last update timestamps
    /// for content management purposes.</remarks>
    public class HelpContent
    {
        /// <summary>
        /// Gets or sets the unique identifier for the entity.
        /// </summary>
        public int Id { get; set; }
        /// <summary>
        /// KinaUnaTextNumber reference Id, same for all translations of the same text.
        /// </summary>
        public int TextId { get; set; }
        /// <summary>
        /// Gets or sets the name or identifier of the page.
        /// </summary>
        [MaxLength(1024)]
        public string Page { get; set; } = string.Empty;
        /// <summary>
        /// Gets or sets the name of the element.
        /// </summary>
        [MaxLength(1024)]
        public string Element { get; set; } = string.Empty;
        /// <summary>
        /// Gets or sets the identifier of the language associated with the entity.
        /// </summary>
        public int LanguageId { get; set; } = 1;
        /// <summary>
        /// Gets or sets the title associated with the object.
        /// </summary>
        [MaxLength(1024)]
        public string Title { get; set; } = string.Empty;
        /// <summary>
        /// Gets or sets the textual content associated with this instance.
        /// </summary>
        [MaxLength(1000000)]
        public string Content { get; set; } = string.Empty;
        /// <summary>
        /// Gets or sets the date and time when the object was created.
        /// </summary>
        public DateTime CreatedTime { get; set; }
        /// <summary>
        /// Gets or sets the date and time when the entity was last updated.
        /// </summary>
        public DateTime UpdatedTime { get; set; }
    }
}
