using KinaUna.Data.Models.Support;
using System.Collections.Generic;
using System.Threading.Tasks;
using KinaUna.Data.Models;

namespace KinaUnaProgenyApi.Services.Support
{
    public interface IHelpContentService
    {
        /// <summary>
        /// Asynchronously retrieves help content for a specified page element and language.
        /// </summary>
        /// <param name="page">The name of the page for which to retrieve help content. Cannot be null or empty.</param>
        /// <param name="element">The identifier of the element on the page for which help content is requested. Empty for page level content.</param>
        /// <param name="languageId">The identifier of the language in which the help content should be returned.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains a <see cref="HelpContent"/>
        /// object with the requested help information.</returns>
        Task<HelpContent> GetHelpContent(string page, string element, int languageId);

        /// <summary>
        /// Adds a new help content entry to the data store asynchronously.
        /// </summary>
        /// <param name="helpContent">The help content to add. The <see cref="HelpContent.Page"/> and <see cref="HelpContent.Content"/> properties
        /// must not be null or empty.</param>
        /// <param name="currentUserInfo">The user information of the user adding the help content. Used for auditing purposes.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the added <see
        /// cref="HelpContent"/> if the operation succeeds; otherwise, <see langword="null"/> if required properties are
        /// missing.</returns>
        Task<HelpContent> AddHelpContent(HelpContent helpContent, UserInfo currentUserInfo);

        /// <summary>
        /// Updates the specified help content entry in the data store and returns the updated entity.
        /// </summary>
        /// <param name="helpContent">The help content entity to update. Must not be null and should represent an existing entry in the data
        /// store.</param>
        /// <param name="currentUserInfo">The user information of the user performing the update. Used for auditing purposes.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the updated help content entity.</returns>
        Task<HelpContent> UpdateHelpContent(HelpContent helpContent, UserInfo currentUserInfo);

        /// <summary>
        /// Deletes the help content entry with the specified identifier from the data store.
        /// </summary>
        /// <remarks>If no help content with the specified identifier exists, the method returns <see
        /// langword="null"/> and no action is taken.</remarks>
        /// <param name="helpContentId">The unique identifier of the help content to delete.</param>
        /// <param name="currentUserInfo">The user information of the user performing the deletion. Used for auditing purposes.</param>
        /// <returns>The deleted help content entry if found and removed; otherwise, <see langword="null"/>.</returns>
        Task<HelpContent> DeleteHelpContent(int helpContentId, UserInfo currentUserInfo);

        /// <summary>
        /// Retrieves all help content entries for a specified page and language.
        /// </summary>
        /// <param name="page">The name of the page for which to retrieve help content.</param>
        /// <param name="languageId">The identifier of the language in which the help content should be returned.</param>
        /// <returns>A List of <see cref="HelpContent"/> entries for the specified page and language.</returns>
        Task<List<HelpContent>> GetHelpContentForPage(string page, int languageId);

        /// <summary>
        /// Asynchronously retrieves the help content associated with the specified identifier.
        /// </summary>
        /// <param name="helpContentId">The unique identifier of the help content to retrieve. Must be a positive integer.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the <see cref="HelpContent"/>
        /// object if found; otherwise, <c>null</c>.</returns>
        Task<HelpContent> GetHelpContentById(int helpContentId);
    }
}
