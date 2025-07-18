using System.ComponentModel.DataAnnotations;

namespace KinaUna.OpenIddict.Models.AccountViewModels
{
    public class ForgotPasswordViewModel
    {
        [Required] [EmailAddress] public required string Email { get; set; }
        public string Language { get; set; } = "English";
        public int LanguageId { get; set; } = 1;
    }
}
