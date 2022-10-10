using KinaUna.Data.Models;

namespace KinaUnaWebBlazor.Models.ItemViewModels
{
    public class ContactViewModel: BaseViewModel
    {
        public int ContactId { get; set; } = 0;
        public bool Active { get; set; } = false;
        public string FirstName { get; set; } = "";
        public string MiddleName { get; set; } = "";
        public string LastName { get; set; } = "";
        public string DisplayName { get; set; } = "";
        public int? AddressIdNumber { get; set; }
        public string Email1 { get; set; } = "";
        public string Email2 { get; set; } = "";
        public string PhoneNumber { get; set; } = "";
        public string MobileNumber { get; set; } = "";
        public string Notes { get; set; } = "";
        public string PictureLink { get; set; } = "";
        public string Website { get; set; } = "";
        public int AccessLevel { get; set; } = 5;
        public string Author { get; set; } = "";
        public int ProgenyId { get; set; } = 0;
        public bool IsAdmin { get; set; } = false;
        public string Context { get; set; } = "";
        public Address Address { get; set; } = new Address();
        public string AddressLine1 { get; set; } = "";
        public string AddressLine2 { get; set; } = "";
        public string City { get; set; } = "";
        public string State { get; set; } = "";
        public string PostalCode { get; set; } = "";
        public string Country { get; set; } = "";
        public string FileName { get; set; } = "";
        public IFormFile? File { get; set; }
        public string Tags { get; set; } = "";
        public string TagsList { get; set; } = "";
        public string TagFilter { get; set; } = "";
        public DateTime? DateAdded { get; set; }
        public Contact Contact { get; set; } = new Contact();
    }
}
