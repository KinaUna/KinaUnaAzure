using KinaUna.Data.Models.ItemInterfaces;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace KinaUna.Data.Models
{
    /// <summary>
    /// Entity Framework Entity for Skill data.
    /// </summary>
    public class Skill: ICategorical
    {
        [Key]
        public int SkillId { get; set; }

        public string Name { get; set; }

        [MaxLength(256)]
        public string Description { get; set; }

        [MaxLength(256)]
        public string Category { get; set; }
        public DateTime? SkillFirstObservation { get; set; }
        public DateTime SkillAddedDate { get; set; }

        [MaxLength(256)]
        public string Author { get; set; }
        public int ProgenyId { get; set; }
        [NotMapped]
        public Progeny Progeny { get; set; }

        public int AccessLevel { get; set; } // 0 = Hidden/Parents only, 1=Family, 2= Friends, 3=DefaultUsers, 4= public.

        [NotMapped]
        public int SkillNumber { get; set; }
    }
}
