using System.Collections.Generic;
using System.Threading.Tasks;

namespace KinaUnaWeb.Services.HttpClients
{
    /// <summary>
    /// Provides methods for interacting with the AutoSuggests API.
    /// </summary>
    public interface IAutoSuggestsHttpClient
    {
        /// <summary>
        /// Gets the list of all unique tags for a Progeny, including only items that the user has access to.
        /// </summary>
        /// <param name="progenyId">The ProgenyId of the Progeny to get tags for.</param>
        /// <param name="accessLevel">The user's access level for the Progeny</param>
        /// <returns>List of strings.</returns>
        Task<List<string>> GetTagsList(int progenyId, int accessLevel);

        /// <summary>
        /// Gets the list of all unique contexts for a Progeny, including only items that the user has access to.
        /// </summary>
        /// <param name="progenyId">The ProgenyId of the Progeny to get tags for.</param>
        /// <param name="accessLevel">The user's access level for the Progeny</param>
        /// <returns>List of strings.</returns>
        Task<List<string>> GetContextsList(int progenyId, int accessLevel);

        /// <summary>
        /// Gets the list of all unique location names for a Progeny, including only items that the user has access to.
        /// </summary>
        /// <param name="progenyId">The ProgenyId of the Progeny to get tags for.</param>
        /// <param name="accessLevel">The user's access level for the Progeny</param>
        /// <returns>List of strings.</returns>
        Task<List<string>> GetLocationsList(int progenyId, int accessLevel);

        /// <summary>
        /// Gets the list of all unique categories for a Progeny, including only items that the user has access to.
        /// </summary>
        /// <param name="progenyId">The ProgenyId of the Progeny to get tags for.</param>
        /// <param name="accessLevel">The user's access level for the Progeny</param>
        /// <returns>List of strings.</returns>
        Task<List<string>> GetCategoriesList(int progenyId, int accessLevel);
    }
}
