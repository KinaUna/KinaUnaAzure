namespace KinaUnaWeb.Models.TypeScriptModels.Contacts
{
    public class ContactItemResponse
    {
        public int ContactId { get; set; }
        public int LanguageId { get; init; }
        public bool IsCurrentUserProgenyAdmin { get; set; }
        public Contact Contact { get; set; }
    }
}
