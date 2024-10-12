using KinaUna.Data.Models.ItemInterfaces;
using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace KinaUna.Data.Models
{
    /// <summary>
    /// Entity Framework Entity for Note data.
    /// </summary>
    public class Note: ICategorical
    {
        public int NoteId { get; set; }
        public string Title { get; set; }
        public string Content { get; set; }
        public string Category { get; set; }
        public DateTime CreatedDate { get; set; }
        public int AccessLevel { get; set; }
        public int ProgenyId { get; set; }
        public string Owner { get; set; }

        [NotMapped]
        public Progeny Progeny { get; set; }

        [NotMapped]
        public int NoteNumber { get; set; }
    }
}
