using System.Collections.Generic;
using System.Threading.Tasks;
using KinaUna.Data.Models;

namespace KinaUnaProgenyApi.Services
{
    public interface IMeasurementService
    {
        /// <summary>
        /// Gets a Measurement by MeasurementId.
        /// First tries to get the Measurement from the cache, then from the database if it's not in the cache.
        /// </summary>
        /// <param name="id">The MeasurementId of the Measurement entity to get.</param>
        /// <param name="currentUserInfo">The UserInfo object for the current user, to check permissions.</param>
        /// <returns>The Measurement object with the given MeasurementId. Null if the Measurement doesn't exist.</returns>
        Task<Measurement> GetMeasurement(int id, UserInfo currentUserInfo);

        /// <summary>
        /// Adds a new Measurement to the database and the cache.
        /// </summary>
        /// <param name="measurement">The Measurement object to add.</param>
        /// <param name="currentUserInfo"></param>
        /// <returns>The added Measurement object.</returns>
        Task<Measurement> AddMeasurement(Measurement measurement, UserInfo currentUserInfo);

        /// <summary>
        /// Updates a Measurement in the database and the cache.
        /// </summary>
        /// <param name="measurement">The Measurement with the updated properties.</param>
        /// <param name="currentUserInfo"></param>
        /// <returns>The updated Measurement.</returns>
        Task<Measurement> UpdateMeasurement(Measurement measurement, UserInfo currentUserInfo);

        /// <summary>
        /// Deletes a Measurement from the database and the cache.
        /// </summary>
        /// <param name="measurement">The Measurement to delete.</param>
        /// <param name="currentUserInfo"></param>
        /// <returns>The deleted Measurement object.</returns>
        Task<Measurement> DeleteMeasurement(Measurement measurement, UserInfo currentUserInfo);

        /// <summary>
        /// Gets a list of all Measurements for a Progeny.
        /// First tries to get the list from the cache, then from the database if it's not in the cache.
        /// </summary>
        /// <param name="progenyId">The ProgenyId of the Progeny to get Measurements for.</param>
        /// <param name="currentUserInfo">The UserInfo object for the current user, to check permissions.</param>
        /// <returns>List of Measurements.</returns>
        Task<List<Measurement>> GetMeasurementsList(int progenyId, UserInfo currentUserInfo);
    }
}
