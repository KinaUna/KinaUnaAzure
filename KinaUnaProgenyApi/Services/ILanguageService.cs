using System.Collections.Generic;
using System.Threading.Tasks;
using KinaUna.Data.Models;

namespace KinaUnaProgenyApi.Services
{
    public interface ILanguageService
    {
        Task<List<KinaUnaLanguage>> GetAllLanguages();
        Task<KinaUnaLanguage> GetLanguage(int languageId);
        Task<KinaUnaLanguage> AddLanguage(KinaUnaLanguage language);
        Task<KinaUnaLanguage> UpdateLanguage(KinaUnaLanguage language);
        Task<KinaUnaLanguage> DeleteLanguage(int languageId);
    }
}
