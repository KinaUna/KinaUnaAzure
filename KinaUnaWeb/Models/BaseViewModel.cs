using KinaUna.Data.Models;

namespace KinaUnaWeb.Models
{
    public class BaseViewModel
    {
        public UserInfo CurrentUser { get; set; }
        public int LanguageId { get; set; }
        public string ReturnUrl { get; set; } = string.Empty;
        public string ErrorMessage { get; set; } = string.Empty;
        public string LanguageIdString
        {
            get
            {
                if (LanguageId == 2)
                {
                    return "de";
                }

                if (LanguageId == 3)
                {
                    return "da";
                }

                return "en";
            }
        }
    }
}
