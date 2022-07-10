using KinaUna.Data.Models;

namespace KinaUnaWeb.Models
{
    public class BaseViewModel
    {
        public UserInfo CurrentUser { get; set; }
        public int LanguageId { get; set; }
        public string ReturnUrl { get; set; } = string.Empty;
        public string ErrorMessage { get; set; } = string.Empty;
    }
}
