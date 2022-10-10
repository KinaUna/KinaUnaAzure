using KinaUna.Data.Models;

namespace KinaUnaWebBlazor.Models
{
    public class BaseViewModel
    {
        public UserInfo CurrentUser { get; set; }
        public int LanguageId { get; set; }
        public string ReturnUrl { get; set; }
        public string ErrorMessage { get; set; }

        public BaseViewModel()
        {
            CurrentUser = new UserInfo();
            LanguageId = 1;
            ReturnUrl = "";
            ErrorMessage = "";
        }
    }
}
