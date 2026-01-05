using KinaUna.Data.Models.Support;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace KinaUnaWeb.Services.HttpClients.Support
{
    public interface IHelpHttpClient
    {
        /// <summary>
        /// Retrieves help content for a specified page element and language.
        /// </summary>
        /// <param name="page">The name of the page for which to retrieve help content. Cannot be null or empty.</param>
        /// <param name="element">The identifier of the element on the page for which help content is requested. Cannot be null or empty.</param>
        /// <param name="languageId">The identifier of the language in which the help content should be returned.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains a <see cref="HelpContent"/>
        /// object with the requested help information. Returns an empty <see cref="HelpContent"/> object if no content
        /// is found.</returns>
        Task<HelpContent> GetHelpContent(string page, string element, int languageId);

        /// <summary>
        /// Adds a new help content entry by sending it to the remote API.
        /// </summary>
        /// <remarks>The method requires the user to be authenticated. The returned HelpContent object
        /// reflects the data as stored by the remote API, which may include additional fields or
        /// modifications.</remarks>
        /// <param name="helpContent">The help content to add. Must not be null.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the added help content as
        /// returned by the API. If the operation fails, returns an empty HelpContent object.</returns>
        Task<HelpContent> AddHelpContent(HelpContent helpContent);

        /// <summary>
        /// Updates the specified help content by sending it to the server and returns the updated version.
        /// </summary>
        /// <remarks>The method requires the user to be authenticated. The returned HelpContent instance
        /// may be empty if the update operation is unsuccessful.</remarks>
        /// <param name="helpContent">The help content to update. Must not be null.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the updated help content
        /// returned by the server. If the update fails, returns a new, empty HelpContent instance.</returns>
        Task<HelpContent> UpdateHelpContent(HelpContent helpContent);

        /// <summary>
        /// Deletes the help content item with the specified identifier.
        /// </summary>
        /// <remarks>The method requires the caller to be authenticated. If the specified help content
        /// does not exist or the deletion fails, an empty <see cref="HelpContent"/> object is returned.</remarks>
        /// <param name="helpContentId">The unique identifier of the help content to delete.</param>
        /// <returns>A <see cref="HelpContent"/> object representing the deleted help content if the operation succeeds;
        /// otherwise, an empty <see cref="HelpContent"/> object.</returns>
        Task<HelpContent> DeleteHelpContent(int helpContentId);

        /// <summary>
        /// Asynchronously retrieves the help content associated with the specified identifier.
        /// </summary>
        /// <param name="helpContentId">The unique identifier of the help content to retrieve. Must be a positive integer.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the <see cref="HelpContent"/>
        /// object if found; otherwise, <c>null</c>.</returns>
        Task<HelpContent> GetHelpContentById(int helpContentId);

        /// <summary>
        /// Retrieves a list of available help content page identifiers from the remote help API.
        /// </summary>
        /// <remarks>This method requires the caller to be authenticated. The returned list may be empty
        /// if the user does not have access to any help content or if the remote service is unavailable.</remarks>
        /// <returns>A list of strings representing the identifiers of available help content pages. Returns an empty list if no
        /// pages are found or if the request fails.</returns>
        Task<List<string>> GetHelpContentPages();

        /// <summary>
        /// Retrieves all available help content entries asynchronously.
        /// </summary>
        /// <returns>A list of <see cref="HelpContent"/> objects representing all help content entries. Returns an empty list if
        /// no help content is available.</returns>
        Task<List<HelpContent>> GetAllHelpContents();
    }
}
