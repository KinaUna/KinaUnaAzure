using KinaUna.Data.Models;

namespace KinaUna.OpenIddict.Models.HomeViewModels
{
    /// <summary>
    /// Represents a view model for setting a language identifier.
    /// </summary>
    /// <remarks>This view model is used to manage the selection of a language from a list of available
    /// languages.</remarks>
    public class SetLanguageIdViewModel
    {
        /// <summary>
        /// Gets or sets the list of available languages.
        /// </summary>
        public required List<KinaUnaLanguage>? LanguageList { get; set; }

        /// <summary>
        /// Gets the identifier of the selected item.
        /// </summary>
        public int SelectedId { get; init; }

        /// <summary>
        /// Gets or sets the URL to which the user is redirected after a successful operation.
        /// </summary>
        public string ReturnUrl { get; set; } = string.Empty;
    }
}
