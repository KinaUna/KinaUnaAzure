using System.Collections.Generic;
using System.Threading.Tasks;
using KinaUna.Data.Models;

namespace KinaUnaWeb.Services.HttpClients
{
    /// <summary>
    /// The translations http client interface.
    /// Contains the methods for adding, retrieving and updating data relevant to translation functions.
    /// </summary>
    public interface ITranslationsHttpClient
    {
        Task<string> GetTranslation(string word, string page, int languageId, bool updateCache = false);
        Task<TextTranslation> AddTranslation(TextTranslation translation);
        Task<TextTranslation> UpdateTranslation(TextTranslation translation);
        Task<List<TextTranslation>> GetAllTranslations(int languageId = 0, bool updateCache = false);
        Task<TextTranslation> GetTranslationById(int id, bool updateCache = false);
        Task<TextTranslation> DeleteTranslation(TextTranslation deleteTranslation);
        Task<TextTranslation> DeleteSingleItemTranslation(TextTranslation translation);
    }
}
