using System.Collections.Generic;
using System.Threading.Tasks;
using KinaUna.Data.Models;

namespace KinaUnaWeb.Services
{
    /// <summary>
    /// The Page Texts http client interface.
    /// Contains the methods for adding, retrieving and updating data relevant to page text functions.
    /// </summary>
    public interface IPageTextsHttpClient
    {
        Task<KinaUnaText> GetPageTextByTitle(string title, string page, int languageId, bool updateCache = false);
        Task<KinaUnaText> GetPageTextById(int id, bool updateCache = false);
        Task<KinaUnaText> UpdatePageText(KinaUnaText pivoqText);
        Task<List<KinaUnaText>> GetAllPivoqTexts(int languageId = 0, bool updateCache = false);
    }
}
