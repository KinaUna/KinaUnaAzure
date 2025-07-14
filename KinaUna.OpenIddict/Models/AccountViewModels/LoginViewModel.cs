using System.ComponentModel.DataAnnotations;

namespace KinaUna.OpenIddict.Models.AccountViewModels
{
    public class LoginViewModel
    {
        [Required] public required string Username { get; set; }
        [Required] public required string Password { get; set; }
        public string? ReturnUrl { get; set; }
    }
}
