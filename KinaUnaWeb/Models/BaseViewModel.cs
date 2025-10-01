namespace KinaUnaWeb.Models
{
    /// <summary>
    /// Base ViewModel for all views.
    /// </summary>
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
                return LanguageId switch
                {
                    2 => "de",
                    3 => "da",
                    _ => "en"
                };
            }
        }
    }
}
