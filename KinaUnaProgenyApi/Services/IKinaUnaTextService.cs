using System.Collections.Generic;
using System.Threading.Tasks;
using KinaUna.Data.Models;

namespace KinaUnaProgenyApi.Services
{
    public interface IKinaUnaTextService
    {
        Task<KinaUnaText> GetTextByTitle(string title, string page, int languageId);
        Task<KinaUnaText> GetTextById(int id);
        Task<KinaUnaText> GetTextByTextId(int textId, int languageId);
        Task<List<KinaUnaText>> GetPageTextsList(string page, int languageId);
        Task<List<KinaUnaText>> GetAllPageTextsList(int languageId);
        Task CheckLanguages();
        Task<KinaUnaText> AddText(KinaUnaText text);
        Task<KinaUnaText> UpdateText(int id, KinaUnaText text);
        Task<KinaUnaText> DeleteText(int id);
        Task<KinaUnaText> DeleteSingleText(int id);
    }
}
