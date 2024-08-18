using System.ComponentModel.DataAnnotations;

namespace KinaUna.IDP.Models.AccountViewModels
{
    public class ForgotPasswordViewModel
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; }
        public string Language { get; set; }
        public int LanguageId { get; set; }
    }
}
