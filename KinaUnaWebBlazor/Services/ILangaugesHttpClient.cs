using KinaUna.Data.Models;

namespace KinaUnaWebBlazor.Services
{
    public interface ILanguagesHttpClient
    {
        Task<List<KinaUnaLanguage>> GetAllLanguages(bool updateCache = false);

        Task<KinaUnaLanguage> GetLanguage(int languageId, bool updateCache = false);
        Task<KinaUnaLanguage> AddLanguage(KinaUnaLanguage language);
        Task<KinaUnaLanguage> UpdateLanguage(KinaUnaLanguage language);
        Task<KinaUnaLanguage> DeleteLanguage(KinaUnaLanguage language);
    }
}
