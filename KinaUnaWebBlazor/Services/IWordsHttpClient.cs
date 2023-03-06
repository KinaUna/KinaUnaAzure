using KinaUna.Data.Models;

namespace KinaUnaWebBlazor.Services
{
    public interface IWordsHttpClient
    {
        /// <summary>
        /// Gets the VocabularyItem with the given WordId.
        /// </summary>
        /// <param name="wordId">int: The Id of the VocabularyItem (VocabularyItem.WordId).</param>
        /// <returns>VocabularyItem.</returns>
        Task<VocabularyItem?> GetWord(int wordId);

        /// <summary>
        /// Adds a new VocabularyItem.
        /// </summary>
        /// <param name="word">VocabularyItem: The new VocabularyItem to add.</param>
        /// <returns>VocabularyItem</returns>
        Task<VocabularyItem?> AddWord(VocabularyItem? word);

        /// <summary>
        /// Updates a VocabularyItem. The VocabularyItem with the same WordId will be updated.
        /// </summary>
        /// <param name="word">VocabularyItem: The VocabularyItem to update.</param>
        /// <returns>VocabularyItem: The updated VocabularyItem object.</returns>
        Task<VocabularyItem?> UpdateWord(VocabularyItem? word);

        /// <summary>
        /// Removes the VocabularyItem with the given WordId.
        /// </summary>
        /// <param name="wordId">int: The Id of the VocabularyItem to remove (VocabularyItem.WordId).</param>
        /// <returns>bool: True if the VocabularyItem was successfully removed.</returns>
        Task<bool> DeleteWord(int wordId);

        /// <summary>
        /// Gets a progeny's list of VocabularyItems that a user has access to.
        /// </summary>
        /// <param name="progenyId">int: The Id of the progeny (Progeny.Id).</param>
        /// <param name="accessLevel">int: The user's access level.</param>
        /// <returns>List of VocabularyItem objects.</returns>
        Task<List<VocabularyItem>?> GetWordsList(int progenyId, int accessLevel);
    }
}
