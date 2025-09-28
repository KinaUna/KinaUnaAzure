using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace KinaUna.Data.Models
{
    /// <summary>
    /// Entity Framework Entity for Vocabulary data.
    /// </summary>
    public class VocabularyItem
    {
        [Key]
        public int WordId { get; set; }

        [MaxLength(4096)] public string Word { get; set; } = string.Empty; // The vocabulary word.

        [MaxLength(4096)] public string Description { get; set; } = string.Empty; // Description or meaning of the word.

        [MaxLength(256)]
        public string Language { get; set; } = string.Empty; // Language of the word, e.g., English, Spanish.

        [MaxLength(4096)]
        public string SoundsLike { get; set; } = string.Empty; // Phonetic representation or pronunciation guide.
        public DateTime? Date { get; set; }
        public DateTime DateAdded { get; set; }

        [MaxLength(256)]
        public string Author { get; set; } = string.Empty; // Todo: Replace with CreatedBy?
        public int ProgenyId { get; set; }
        public int AccessLevel { get; set; } // 0 = Hidden/Parents only, 1=Family, 2= Friends, 3=DefaultUsers, 4= public.

        /// <summary>
        /// Gets or sets the identifier of the user or system that created the entity.
        /// </summary>
        [MaxLength(256)]
        public string CreatedBy { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the date and time when the entity was created.
        /// </summary>
        public DateTime CreatedTime { get; set; }

        /// <summary>
        /// Gets or sets the identifier of the user or system that last modified the entity.
        /// </summary>
        [MaxLength(256)]
        public string ModifiedBy { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the date and time when the object was last modified.
        /// </summary>
        public DateTime ModifiedTime { get; set; }

        [NotMapped]
        public Progeny Progeny { get; set; }
        
        [NotMapped]
        public int VocabularyItemNumber { get; set; }
    }
}
