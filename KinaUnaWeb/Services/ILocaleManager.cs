using System.Threading.Tasks;
using KinaUnaWeb.Models.HomeViewModels;

namespace KinaUnaWeb.Services
{
    public interface ILocaleManager
    {
        Task<SetLanguageIdViewModel> GetLanguageModel(int currentLanguageId);
        
        Task<string> GetTranslation(string word, string page, int languageId);
    }
}
