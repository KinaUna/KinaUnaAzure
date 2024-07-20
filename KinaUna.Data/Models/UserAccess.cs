using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace KinaUna.Data.Models
{
    /// <summary>
    /// Entity Framework Entity for UserAccess data.
    /// Defines a user's access level to a Progeny's data
    /// </summary>
    public class UserAccess
    {
        [Key]
        public int AccessId { get; init; }
        public int ProgenyId { get; set; }
        public string UserId { get; set; }
        public int AccessLevel { get; set; }
        public bool CanContribute { get; set; }
        [NotMapped]
        public Progeny Progeny { get; set; }
        [NotMapped]
        public ApplicationUser User { get; set; }
        [NotMapped]
        public string AccessLevelString { get; set; }
    }
}
