using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace KinaUna.Data.Models
{
    /// <summary>
    /// Entity Framework Entity for ProgenyInfo data.
    /// </summary>
    public class ProgenyInfo
    {
        [Key]
        public int ProgenyInfoId { get; set; }
        public int ProgenyId { get; set; }
        public string Email { get; set; } = string.Empty;
        public string MobileNumber { get; set; } = string.Empty;
        public int AddressIdNumber { get; set; } = 0;
        public string Website { get; set; } = string.Empty;
        public string Notes { get; set; } = string.Empty;

        [NotMapped] public Address Address { get; set; } = new();
    }
}
