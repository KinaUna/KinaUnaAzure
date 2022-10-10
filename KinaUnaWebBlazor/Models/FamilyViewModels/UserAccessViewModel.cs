using KinaUna.Data.Models;

namespace KinaUnaWebBlazor.Models.FamilyViewModels
{
    public class UserAccessViewModel: BaseViewModel
    {
        public int AccessId { get; set; }
        public int ProgenyId { get; set; }
        public Progeny? Progeny { get; set; }
        public string ProgenyName { get; set; } = "";
        public string UserId { get; set; } = "";
        public string UserName { get; set; } = "";
        public string FirstName { get; set; } = "";
        public string MiddleName { get; set; } = "";
        public string LastName { get; set; } = "";
        public string Email { get; set; } = "";
        public int AccessLevel { get; set; } = 5;
    }
}
