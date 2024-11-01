using KinaUna.Data.Models.ItemInterfaces;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace KinaUna.Data.Models
{
    /// <summary>
    /// Entity Framework Entity for Friend data.
    /// </summary>
    public class Friend: IContexted, ITaggable
    {
        [Key]
        public int FriendId { get; set; }

        [MaxLength(256)]
        public string Name { get; set; }

        [MaxLength(4096)]
        public string Description { get; set; }
        public DateTime? FriendSince { get; set; }
        public DateTime FriendAddedDate { get; set; }

        [MaxLength(1024)]
        public string PictureLink { get; set; }
        public int ProgenyId { get; set; }
        [NotMapped]
        public Progeny Progeny { get; set; }
        public int Type { get; set; } // 0= Personal friend, 1= toy friend, 2= parent, 3= family, 4= caretaker

        [MaxLength(1024)]
        public string Context { get; set; }
        
        public string Notes { get; set; }

        [MaxLength(1024)]
        public string Tags { get; set; }
        public int AccessLevel { get; set; } // 0 = Hidden/Parents only, 1=Family, 2= Friends, 3=DefaultUSers, 4= public.

        [MaxLength(128)]
        public string Author { get; set; }
    }
}
