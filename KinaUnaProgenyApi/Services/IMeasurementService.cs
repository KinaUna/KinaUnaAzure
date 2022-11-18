using System.Collections.Generic;
using System.Threading.Tasks;
using KinaUna.Data.Models;

namespace KinaUnaProgenyApi.Services
{
    public interface IMeasurementService
    {
        Task<Measurement> GetMeasurement(int id);
        Task<Measurement> AddMeasurement(Measurement measurement);
        Task<Measurement> UpdateMeasurement(Measurement measurement);
        Task<Measurement> DeleteMeasurement(Measurement measurement);
        Task<List<Measurement>> GetMeasurementsList(int progenyId);
    }
}
