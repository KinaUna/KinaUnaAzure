using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations;
using KinaUna.Data.Models;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace KinaUna.OpenIddict.Models.AccountViewModels
{
    public class RegisterViewModel
    {
        [Required]
        [EmailAddress]
        [Display(Name = "Email")]
        public string? Email { get; set; }

        [Required]
        [StringLength(100, ErrorMessage = "The {0} must be at least {2} and at max {1} characters long.", MinimumLength = 8)]
        [DataType(DataType.Password)]
        [Display(Name = "Password")]
        public string? Password { get; set; }

        [DataType(DataType.Password)]
        [Display(Name = "Confirm password")]
        [Compare("Password", ErrorMessage = "The password and confirmation password do not match.")]
        public string? ConfirmPassword { get; set; }

        public string? ReturnUrl { get; set; }
        public string? TimeZone { get; set; }
        public SelectListItem[] TimezoneList { get; set; }
        public ApplicationUser? User { get; set; }
        public string Language { get; set; } = "English";
        public int LanguageId { get; set; } = 1;

        /// <summary>
        /// Honeypot field. Must remain empty for legitimate submissions.
        /// Bots tend to fill in fields that look like "website" or "url".
        /// </summary>
        public string? Website { get; set; }

        /// <summary>
        /// Cloudflare Turnstile site key, populated by the controller for use in the view.
        /// </summary>
        public string TurnstileSiteKey { get; set; } = string.Empty;

        public RegisterViewModel()
        {
            ReadOnlyCollection<TimeZoneInfo> tzs = TimeZoneInfo.GetSystemTimeZones();
            TimezoneList = [.. tzs.Select(tz => new SelectListItem()
            {
                Text = tz.DisplayName,
                Value = tz.Id
            })];
        }
    }
}
