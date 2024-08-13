using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using KinaUna.Data;
using KinaUna.Data.Contexts;
using KinaUna.Data.Extensions;
using KinaUna.Data.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Newtonsoft.Json;

namespace KinaUnaProgenyApi.Services
{
    public class MeasurementService : IMeasurementService
    {
        private readonly ProgenyDbContext _context;
        private readonly IDistributedCache _cache;
        private readonly DistributedCacheEntryOptions _cacheOptions = new();
        private readonly DistributedCacheEntryOptions _cacheOptionsSliding = new();

        public MeasurementService(ProgenyDbContext context, IDistributedCache cache)
        {
            _context = context;
            _cache = cache;
            _cacheOptions.SetAbsoluteExpiration(new System.TimeSpan(0, 5, 0)); // Expire after 5 minutes.
            _cacheOptionsSliding.SetSlidingExpiration(new System.TimeSpan(7, 0, 0, 0)); // Expire after a week.
        }

        /// <summary>
        /// Gets a Measurement by MeasurementId.
        /// First tries to get the Measurement from the cache, then from the database if it's not in the cache.
        /// </summary>
        /// <param name="id">The MeasurementId of the Measurement entity to get.</param>
        /// <returns>The Measurement object with the given MeasurementId. Null if the Measurement doesn't exist.</returns>
        public async Task<Measurement> GetMeasurement(int id)
        {
            Measurement measurement = await GetMeasurementFromCache(id);
            if (measurement == null || measurement.MeasurementId == 0)
            {
                measurement = await SetMeasurementInCache(id);
            }

            return measurement;
        }

        /// <summary>
        /// Gets a Measurement by MeasurementId from the cache.
        /// </summary>
        /// <param name="id">MeasurementId of the Measurement to get.</param>
        /// <returns>Measurement with the given MeasurementId. Null if it doesn't exist in the cache.</returns>
        private async Task<Measurement> GetMeasurementFromCache(int id)
        {
            
            string cachedMeasurement = await _cache.GetStringAsync(Constants.AppName + Constants.ApiVersion + "measurement" + id);
            if (string.IsNullOrEmpty(cachedMeasurement))
            {
                return null;
            }

            Measurement measurement = JsonConvert.DeserializeObject<Measurement>(cachedMeasurement);
            return measurement;
        }

        /// <summary>
        /// Gets a Measurement by MeasurementId from the database and adds it to the cache.
        /// </summary>
        /// <param name="id">The MeasurementId of the Measurement entity to get.</param>
        /// <returns>The Measurement object with the given MeasurementId. Null if the Measurement doesn't exist.</returns>
        private async Task<Measurement> SetMeasurementInCache(int id)
        {
            Measurement measurement = await _context.MeasurementsDb.AsNoTracking().SingleOrDefaultAsync(m => m.MeasurementId == id);
            if (measurement == null) return null;

            await _cache.SetStringAsync(Constants.AppName + Constants.ApiVersion + "measurement" + id, JsonConvert.SerializeObject(measurement), _cacheOptionsSliding);

            _ = await SetMeasurementsListInCache(measurement.ProgenyId);

            return measurement;
        }

        /// <summary>
        /// Adds a new Measurement to the database and the cache.
        /// </summary>
        /// <param name="measurement">The Measurement object to add.</param>
        /// <returns>The added Measurement object.</returns>
        public async Task<Measurement> AddMeasurement(Measurement measurement)
        {
            Measurement measurementToAdd = new();
            measurementToAdd.CopyPropertiesForAdd(measurement);

            _ = _context.MeasurementsDb.Add(measurementToAdd);
            _ = await _context.SaveChangesAsync();
            _ = await SetMeasurementInCache(measurementToAdd.MeasurementId);

            return measurementToAdd;
        }

        /// <summary>
        /// Updates a Measurement in the database and the cache.
        /// </summary>
        /// <param name="measurement">The Measurement with the updated properties.</param>
        /// <returns>The updated Measurement.</returns>
        public async Task<Measurement> UpdateMeasurement(Measurement measurement)
        {
            Measurement measurementToUpdate = await _context.MeasurementsDb.SingleOrDefaultAsync(m => m.MeasurementId == measurement.MeasurementId);
            if (measurementToUpdate == null) return null;

            measurementToUpdate.CopyPropertiesForUpdate(measurement);

            _ = _context.MeasurementsDb.Update(measurementToUpdate);
            _ = await _context.SaveChangesAsync();

            _ = await SetMeasurementInCache(measurement.MeasurementId);

            return measurementToUpdate;
        }

        /// <summary>
        /// Deletes a Measurement from the database and the cache.
        /// </summary>
        /// <param name="measurement">The Measurement to delete.</param>
        /// <returns>The deleted Measurement object.</returns>
        public async Task<Measurement> DeleteMeasurement(Measurement measurement)
        {
            Measurement measurementToDelete = await _context.MeasurementsDb.SingleOrDefaultAsync(m => m.MeasurementId == measurement.MeasurementId);
            if (measurementToDelete == null) return null;

            _ = _context.MeasurementsDb.Remove(measurementToDelete);
            _ = await _context.SaveChangesAsync();

            await RemoveMeasurementFromCache(measurement.MeasurementId, measurement.ProgenyId);


            return measurement;
        }

        /// <summary>
        /// Removes a Measurement from the cache.
        /// </summary>
        /// <param name="id">The MeasurementId of the Measurement to remove.</param>
        /// <param name="progenyId">The ProgenyId of the Progeny that the Measurement belongs to.</param>
        /// <returns></returns>
        private async Task RemoveMeasurementFromCache(int id, int progenyId)
        {
            await _cache.RemoveAsync(Constants.AppName + Constants.ApiVersion + "measurement" + id);

            _ = await SetMeasurementsListInCache(progenyId);
        }

        /// <summary>
        /// Gets a list of all Measurements for a Progeny.
        /// First tries to get the list from the cache, then from the database if it's not in the cache.
        /// </summary>
        /// <param name="progenyId">The ProgenyId of the Progeny to get Measurements for.</param>
        /// <returns>List of Measurements.</returns>
        public async Task<List<Measurement>> GetMeasurementsList(int progenyId)
        {
            List<Measurement> measurementsList = await GetMeasurementsListFromCache(progenyId);
            if (measurementsList.Count == 0)
            {
                measurementsList = await SetMeasurementsListInCache(progenyId);
            }

            return measurementsList;
        }

        /// <summary>
        /// Gets a list of all Measurements for a Progeny from the cache.
        /// </summary>
        /// <param name="progenyId">The ProgenyId of the Progeny to get Measurements for.</param>
        /// <returns>List of Measurements.</returns>
        private async Task<List<Measurement>> GetMeasurementsListFromCache(int progenyId)
        {
            List<Measurement> measurementsList = [];
            string cachedMeasurementsList = await _cache.GetStringAsync(Constants.AppName + Constants.ApiVersion + "measurementslist" + progenyId);
            if (!string.IsNullOrEmpty(cachedMeasurementsList))
            {
                measurementsList = JsonConvert.DeserializeObject<List<Measurement>>(cachedMeasurementsList);
            }

            return measurementsList;
        }

        /// <summary>
        /// Gets a list of all Measurements for a Progeny from the database and adds it to the cache.
        /// </summary>
        /// <param name="progenyId">The ProgenyId of the Progeny to get Measurements for.</param>
        /// <returns>List of Measurements.</returns>
        private async Task<List<Measurement>> SetMeasurementsListInCache(int progenyId)
        {
            List<Measurement> measurementsList = await _context.MeasurementsDb.AsNoTracking().Where(m => m.ProgenyId == progenyId).ToListAsync();
            await _cache.SetStringAsync(Constants.AppName + Constants.ApiVersion + "measurementslist" + progenyId, JsonConvert.SerializeObject(measurementsList), _cacheOptionsSliding);
            return measurementsList;
        }
    }
}
