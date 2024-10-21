namespace KinaUna.Data.Models
{
    /// <summary>
    /// Entity Framework Entity for address data.
    /// Used for Contacts and ProgenyInfo.
    /// </summary>
    public class Address
    {
        public int AddressId { get; set; }
        public string AddressLine1 { get; set; } = string.Empty;
        public string AddressLine2 { get; set; } = string.Empty;
        public string City { get; set; } = string.Empty;
        public string State { get; set; } = string.Empty;
        public string PostalCode { get; set; } = string.Empty;
        public string Country { get; set; } = string.Empty;
    }
}
