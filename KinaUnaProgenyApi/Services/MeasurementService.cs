using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using KinaUna.Data;
using KinaUna.Data.Contexts;
using KinaUna.Data.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Newtonsoft.Json;

namespace KinaUnaProgenyApi.Services
{
    public class MeasurementService: IMeasurementService
    {
        private readonly ProgenyDbContext _context;
        private readonly IDistributedCache _cache;
        private readonly DistributedCacheEntryOptions _cacheOptions = new DistributedCacheEntryOptions();
        private readonly DistributedCacheEntryOptions _cacheOptionsSliding = new DistributedCacheEntryOptions();

        public MeasurementService(ProgenyDbContext context, IDistributedCache cache)
        {
            _context = context;
            _cache = cache;
            _cacheOptions.SetAbsoluteExpiration(new System.TimeSpan(0, 5, 0)); // Expire after 5 minutes.
            _cacheOptionsSliding.SetSlidingExpiration(new System.TimeSpan(7, 0, 0, 0)); // Expire after a week.
        }

        public async Task<Measurement> GetMeasurement(int id)
        {
            Measurement measurement;
            string cachedMeasurement = await _cache.GetStringAsync(Constants.AppName + Constants.ApiVersion + "measurement" + id);
            if (!string.IsNullOrEmpty(cachedMeasurement))
            {
                measurement = JsonConvert.DeserializeObject<Measurement>(cachedMeasurement);
            }
            else
            {
                measurement = await _context.MeasurementsDb.AsNoTracking().SingleOrDefaultAsync(m => m.MeasurementId == id);
                await _cache.SetStringAsync(Constants.AppName + Constants.ApiVersion + "measurement" + id, JsonConvert.SerializeObject(measurement), _cacheOptionsSliding);
            }

            return measurement;
        }

        public async Task<Measurement> AddMeasurement(Measurement measurement)
        {
            _ = _context.MeasurementsDb.Add(measurement);
            _ = await _context.SaveChangesAsync();
            _ = await SetMeasurement(measurement.MeasurementId);

            return measurement;
        }
        public async Task<Measurement> SetMeasurement(int id)
        {
            Measurement measurement = await _context.MeasurementsDb.AsNoTracking().SingleOrDefaultAsync(m => m.MeasurementId == id);
            if (measurement != null)
            {
                await _cache.SetStringAsync(Constants.AppName + Constants.ApiVersion + "measurement" + id, JsonConvert.SerializeObject(measurement), _cacheOptionsSliding);

                List<Measurement> measurementsList = await _context.MeasurementsDb.AsNoTracking().Where(m => m.ProgenyId == measurement.ProgenyId).ToListAsync();
                await _cache.SetStringAsync(Constants.AppName + Constants.ApiVersion + "measurementslist" + measurement.ProgenyId, JsonConvert.SerializeObject(measurementsList), _cacheOptionsSliding);
            }
           
            return measurement;
        }

        public async Task<Measurement> UpdateMeasurement(Measurement measurement)
        {
            _ = _context.MeasurementsDb.Update(measurement);
            _ = await _context.SaveChangesAsync();
            _ = await SetMeasurement(measurement.MeasurementId);
            return measurement;
        }

        public async Task<Measurement> DeleteMeasurement(Measurement measurement)
        {
            await RemoveMeasurement(measurement.MeasurementId, measurement.ProgenyId);

            _ = _context.MeasurementsDb.Remove(measurement);
            _ = await _context.SaveChangesAsync();
           

            return measurement;
        }
        public async Task RemoveMeasurement(int id, int progenyId)
        {
            await _cache.RemoveAsync(Constants.AppName + Constants.ApiVersion + "measurement" + id);

            List<Measurement> measurementsList = await _context.MeasurementsDb.AsNoTracking().Where(m => m.ProgenyId == progenyId).ToListAsync();
            await _cache.SetStringAsync(Constants.AppName + Constants.ApiVersion + "measurementslist" + progenyId, JsonConvert.SerializeObject(measurementsList), _cacheOptionsSliding);
        }

        public async Task<List<Measurement>> GetMeasurementsList(int progenyId)
        {
            List<Measurement> measurementsList;
            string cachedMeasurementsList = await _cache.GetStringAsync(Constants.AppName + Constants.ApiVersion + "measurementslist" + progenyId);
            if (!string.IsNullOrEmpty(cachedMeasurementsList))
            {
                measurementsList = JsonConvert.DeserializeObject<List<Measurement>>(cachedMeasurementsList);
            }
            else
            {
                measurementsList = await _context.MeasurementsDb.AsNoTracking().Where(m => m.ProgenyId == progenyId).ToListAsync();
                await _cache.SetStringAsync(Constants.AppName + Constants.ApiVersion + "measurementslist" + progenyId, JsonConvert.SerializeObject(measurementsList), _cacheOptionsSliding);
            }

            return measurementsList;
        }
    }
}
