namespace KinaUna.Data.Models
{
    /// <summary>
    /// Entity Framework Entity for address data.
    /// Used for Contacts.
    /// </summary>
    public class Address
    {
        public int AddressId { get; set; }
        public string AddressLine1 { get; set; }
        public string AddressLine2 { get; set; }
        public string City { get; set; }
        public string State { get; set; }
        public string PostalCode { get; set; }
        public string Country { get; set; }
    }
}
