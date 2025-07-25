using System.Collections.Generic;
using System.Threading.Tasks;

namespace KinaUnaWeb.Services.HttpClients
{
    /// <summary>
    /// Provides methods to interact with the Vocabulary API.
    /// </summary>
    public interface IWordsHttpClient
    {
        /// <summary>
        /// Gets the VocabularyItem with the given WordId.
        /// </summary>
        /// <param name="wordId">The WordId of the VocabularyItem to get.</param>
        /// <returns>VocabularyItem object with the given WordId. If not found or an error occurs, a new VocabularyItem with WordId=0 is returned.</returns>
        Task<VocabularyItem> GetWord(int wordId);

        /// <summary>
        /// Adds a new VocabularyItem.
        /// </summary>
        /// <param name="word">The new VocabularyItem to add.</param>
        /// <returns>The added VocabularyItem object.</returns>
        Task<VocabularyItem> AddWord(VocabularyItem word);

        /// <summary>
        /// Updates a VocabularyItem. The VocabularyItem with the same WordId will be updated.
        /// </summary>
        /// <param name="word">The VocabularyItem with the updated properties.</param>
        /// <returns>The updated VocabularyItem object. If the item is not found or an error occurs a new VocabularyItem with WordId=0 is returned.</returns>
        Task<VocabularyItem> UpdateWord(VocabularyItem word);

        /// <summary>
        /// Deletes the VocabularyItem with the given WordId.
        /// </summary>
        /// <param name="wordId">The WordId of the VocabularyItem to delete.</param>
        /// <returns>bool: True if the VocabularyItem was successfully deleted.</returns>
        Task<bool> DeleteWord(int wordId);

        /// <summary>
        /// Gets the list of all VocabularyItems for a Progeny that a user has access to.
        /// </summary>
        /// <param name="progenyId">The Id of the progeny.</param>
        /// <returns>List of VocabularyItem objects.</returns>
        Task<List<VocabularyItem>> GetWordsList(int progenyId);
    }
}
