using System.Collections.Generic;
using System.Threading.Tasks;
using KinaUna.Data.Models;

namespace KinaUnaProgenyApi.Services
{
    public interface IVocabularyService
    {
        Task<VocabularyItem> GetVocabularyItem(int id);
        Task<VocabularyItem> AddVocabularyItem(VocabularyItem vocabularyItem);
        Task<VocabularyItem> SetVocabularyItem(int id);
        Task<VocabularyItem> UpdateVocabularyItem(VocabularyItem vocabularyItem);
        Task<VocabularyItem> DeleteVocabularyItem(VocabularyItem vocabularyItem);
        Task RemoveVocabularyItem(int id, int progenyId);
        Task<List<VocabularyItem>> GetVocabularyList(int progenyId);
    }
}
