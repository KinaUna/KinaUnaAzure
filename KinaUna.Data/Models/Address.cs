using System.ComponentModel.DataAnnotations;

namespace KinaUna.Data.Models
{
    /// <summary>
    /// Entity Framework Entity for address data.
    /// Used for Contacts and ProgenyInfo.
    /// </summary>
    public class Address
    {
        public int AddressId { get; set; }

        [MaxLength(256)]
        public string AddressLine1 { get; set; } = string.Empty;

        [MaxLength(256)]
        public string AddressLine2 { get; set; } = string.Empty;

        [MaxLength(256)]
        public string City { get; set; } = string.Empty;

        [MaxLength(256)]
        public string State { get; set; } = string.Empty;

        [MaxLength(256)]
        public string PostalCode { get; set; } = string.Empty;

        [MaxLength(256)]
        public string Country { get; set; } = string.Empty;
    }
}
