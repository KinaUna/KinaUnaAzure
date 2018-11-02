using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;

namespace KinaUnaWeb.Models
{
    public class Note
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
    }
}
