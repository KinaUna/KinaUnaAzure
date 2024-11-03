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

        [MaxLength(4096)]
        public string Word { get; set; }

        [MaxLength(4096)]
        public string Description { get; set; }

        [MaxLength(256)]
        public string Language { get; set; }

        [MaxLength(4096)]
        public string SoundsLike { get; set; }
        public DateTime? Date { get; set; }
        public DateTime DateAdded { get; set; }

        [MaxLength(256)]
        public string Author { get; set; }
        public int ProgenyId { get; set; }
        public int AccessLevel { get; set; } // 0 = Hidden/Parents only, 1=Family, 2= Friends, 3=DefaultUsers, 4= public.

        [NotMapped]
        public Progeny Progeny { get; set; }
        
        [NotMapped]
        public int VocabularyItemNumber { get; set; }
    }
}
