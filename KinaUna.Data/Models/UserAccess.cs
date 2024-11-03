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

        [MaxLength(256)]
        public string UserId { get; set; } // This is actually the email address of the user.
        public int AccessLevel { get; set; }
        public bool CanContribute { get; set; }
        [NotMapped]
        public Progeny Progeny { get; set; }
        [NotMapped]
        public UserInfo User { get; set; }
        [NotMapped]
        public string AccessLevelString { get; set; }
    }
}
