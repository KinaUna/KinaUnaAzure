using System.Threading.Tasks;
using KinaUna.Data.Models;

namespace KinaUnaProgenyApi.Services
{
    public interface IProgenyService
    {
        Task<Progeny> GetProgeny(int id);
        Task<Progeny> AddProgeny(Progeny progeny);
        Task<Progeny> UpdateProgeny(Progeny progeny);
        Task<Progeny> DeleteProgeny(Progeny progeny);
        Task<Progeny> SetProgenyCache(int id);
        Task RemoveProgenyCache(int id);
    }
}
