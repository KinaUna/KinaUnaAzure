using System.Threading.Tasks;
using KinaUna.IDP.Models.HomeViewModels;

namespace KinaUna.IDP.Services
{
    public interface ILocaleManager
    {
        Task<SetLanguageIdViewModel> GetLanguageModel(int currentLanguageId);
        Task<string> GetTranslation(string word, string page, int languageId, bool updateCache = false);
    }
}
