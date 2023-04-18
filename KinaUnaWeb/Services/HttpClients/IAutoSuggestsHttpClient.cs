using System.Collections.Generic;
using System.Threading.Tasks;

namespace KinaUnaWeb.Services.HttpClients
{
    public interface IAutoSuggestsHttpClient
    {
        Task<List<string>> GetTagsList(int progenyId, int accessLevel);
        Task<List<string>> GetContextsList(int progenyId, int accessLevel);
        Task<List<string>> GetLocationsList(int progenyId, int accessLevel);
        Task<List<string>> GetCategoriesList(int progenyId, int accessLevel);
    }
}
