using System.Collections.Generic;
using System.Threading.Tasks;
using KinaUna.Data.Models;

namespace KinaUnaProgenyApi.Services
{
    public interface IVocabularyService
    {
        /// <summary>
        /// Gets a VocabularyItem entity with the specified WordId.
        /// First checks the cache, if not found, gets the VocabularyItem from the database and adds it to the cache.
        /// </summary>
        /// <param name="id">The WordId of the VocabularyItem.</param>
        /// <returns>The VocabularyItem with the given WordId. Null if the VocabularyItem doesn't exist.</returns>
        Task<VocabularyItem> GetVocabularyItem(int id);

        /// <summary>
        /// Adds a new VocabularyItem entity to the database and adds it to the cache.
        /// </summary>
        /// <param name="vocabularyItem">The VocabularyItem to add.</param>
        /// <returns>The added VocabularyItem.</returns>
        Task<VocabularyItem> AddVocabularyItem(VocabularyItem vocabularyItem);

        /// <summary>
        /// Updates a VocabularyItem entity in the database and the cache.
        /// </summary>
        /// <param name="vocabularyItem">The VocabularyItem with the updated properties.</param>
        /// <returns>The updated VocabularyItem object.</returns>
        Task<VocabularyItem> UpdateVocabularyItem(VocabularyItem vocabularyItem);

        /// <summary>
        /// Deletes a VocabularyItem entity from the database and the cache.
        /// </summary>
        /// <param name="vocabularyItem">The VocabularyItem to delete.</param>
        /// <returns>The deleted VocabularyItem object.</returns>
        Task<VocabularyItem> DeleteVocabularyItem(VocabularyItem vocabularyItem);

        /// <summary>
        /// Gets a list of all VocabularyItems for a Progeny.
        /// First checks the cache, if not found, gets the list from the database and adds it to the cache.
        /// </summary>
        /// <param name="progenyId">The ProgenyId of the Progeny to get the list for.</param>
        /// <returns>List of VocabularyItem objects.</returns>
        Task<List<VocabularyItem>> GetVocabularyList(int progenyId);
    }
}
