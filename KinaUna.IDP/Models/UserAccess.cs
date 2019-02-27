using System.ComponentModel.DataAnnotations;

namespace KinaUna.IDP.Models
{
    public class UserAccess
    {
        [Key]
        public int AccessId { get; set; }
        public int ProgenyId { get; set; }
        public string UserId { get; set; }
        public int AccessLevel { get; set; } // 0 = Hidden/Parents only, 1=Family, 2= Friends, 3=DefaultUSers, 4= public.
        public bool CanContribute { get; set; }
        
    }
}
