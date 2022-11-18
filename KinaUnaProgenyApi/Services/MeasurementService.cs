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
            Measurement measurement = await GetMeasurementFromCache(id);
            if (measurement == null || measurement.MeasurementId == 0)
            {
                measurement = await SetMeasurementInCache(id);
            }

            return measurement;
        }

        private async Task<Measurement> GetMeasurementFromCache(int id)
        {
            Measurement measurement = new Measurement();
            string cachedMeasurement = await _cache.GetStringAsync(Constants.AppName + Constants.ApiVersion + "measurement" + id);
            if (!string.IsNullOrEmpty(cachedMeasurement))
            {
                measurement = JsonConvert.DeserializeObject<Measurement>(cachedMeasurement);
            }

            return measurement;
        }

        private async Task<Measurement> SetMeasurementInCache(int id)
        {
            Measurement measurement = await _context.MeasurementsDb.AsNoTracking().SingleOrDefaultAsync(m => m.MeasurementId == id);
            if (measurement != null)
            {
                await _cache.SetStringAsync(Constants.AppName + Constants.ApiVersion + "measurement" + id, JsonConvert.SerializeObject(measurement), _cacheOptionsSliding);

                _ = await SetMeasurementsListInCache(measurement.ProgenyId);
            }

            return measurement;
        }

        public async Task<Measurement> AddMeasurement(Measurement measurement)
        {
            _ = _context.MeasurementsDb.Add(measurement);
            _ = await _context.SaveChangesAsync();
            _ = await SetMeasurementInCache(measurement.MeasurementId);

            return measurement;
        }
        

        public async Task<Measurement> UpdateMeasurement(Measurement measurement)
        {
            Measurement measurementToUpdate = await _context.MeasurementsDb.SingleOrDefaultAsync(m => m.MeasurementId == measurement.MeasurementId);
            if (measurementToUpdate != null)
            {
                measurementToUpdate.AccessLevel = measurement.AccessLevel;
                measurementToUpdate.Author = measurement.Author;
                measurementToUpdate.Circumference = measurement.Circumference;
                measurementToUpdate.CreatedDate = measurement.CreatedDate;
                measurementToUpdate.Date = measurement.Date;
                measurementToUpdate.EyeColor = measurement.EyeColor;
                measurementToUpdate.HairColor = measurement.HairColor;
                measurementToUpdate.Height = measurement.Height;
                measurementToUpdate.MeasurementNumber = measurement.MeasurementNumber;
                measurementToUpdate.Weight = measurement.Weight;
                measurementToUpdate.Progeny = measurement.Progeny;
                _ = _context.MeasurementsDb.Update(measurementToUpdate);
                _ = await _context.SaveChangesAsync();
                _ = await SetMeasurementInCache(measurement.MeasurementId);
            }
            
            return measurementToUpdate;
        }

        public async Task<Measurement> DeleteMeasurement(Measurement measurement)
        {
            Measurement measurementToDelete = await _context.MeasurementsDb.SingleOrDefaultAsync(m => m.MeasurementId == measurement.MeasurementId);
            if (measurementToDelete != null)
            {
                _ = _context.MeasurementsDb.Remove(measurementToDelete);
                _ = await _context.SaveChangesAsync();

                await RemoveMeasurementFromCache(measurement.MeasurementId, measurement.ProgenyId);
            }
            

            return measurement;
        }
        private async Task RemoveMeasurementFromCache(int id, int progenyId)
        {
            await _cache.RemoveAsync(Constants.AppName + Constants.ApiVersion + "measurement" + id);

            _ = await SetMeasurementsListInCache(progenyId);
        }

        public async Task<List<Measurement>> GetMeasurementsList(int progenyId)
        {
            List<Measurement> measurementsList = await GetMeasurementsListFromCache(progenyId);
            if (!measurementsList.Any())
            {
                measurementsList = await SetMeasurementsListInCache(progenyId);
            }

            return measurementsList;
        }

        private async Task<List<Measurement>> GetMeasurementsListFromCache(int progenyId)
        {
            List<Measurement> measurementsList = new List<Measurement>();
            string cachedMeasurementsList = await _cache.GetStringAsync(Constants.AppName + Constants.ApiVersion + "measurementslist" + progenyId);
            if (!string.IsNullOrEmpty(cachedMeasurementsList))
            {
                measurementsList = JsonConvert.DeserializeObject<List<Measurement>>(cachedMeasurementsList);
            }

            return measurementsList;
        }

        private async Task<List<Measurement>> SetMeasurementsListInCache(int progenyId)
        {
            List<Measurement> measurementsList = await _context.MeasurementsDb.AsNoTracking().Where(m => m.ProgenyId == progenyId).ToListAsync();
            await _cache.SetStringAsync(Constants.AppName + Constants.ApiVersion + "measurementslist" + progenyId, JsonConvert.SerializeObject(measurementsList), _cacheOptionsSliding);
            return measurementsList;
        }
    }
}
