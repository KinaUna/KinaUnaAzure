using KinaUna.Data.Models;

namespace KinaUnaWebBlazor.Models
{
    public class BaseViewModel
    {
        public UserInfo CurrentUser { get; set; } = new();
        public int LanguageId { get; set; } = 1;
        public string ReturnUrl { get; set; } = "";
        public string ErrorMessage { get; set; } = "";
    }
}
