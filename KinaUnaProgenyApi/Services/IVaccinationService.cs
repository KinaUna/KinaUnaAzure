using System.Collections.Generic;
using System.Threading.Tasks;
using KinaUna.Data.Models;

namespace KinaUnaProgenyApi.Services
{
    public interface IVaccinationService
    {
        Task<Vaccination> GetVaccination(int id);
        Task<Vaccination> AddVaccination(Vaccination vaccination);
        Task<Vaccination> SetVaccination(int id);
        Task<Vaccination> UpdateVaccination(Vaccination vaccination);
        Task<Vaccination> DeleteVaccination(Vaccination vaccination);
        Task RemoveVaccination(int id, int progenyId);
        Task<List<Vaccination>> GetVaccinationsList(int progenyId);
    }
}
