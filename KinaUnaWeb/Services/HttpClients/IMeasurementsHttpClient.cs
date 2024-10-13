using System.Collections.Generic;
using System.Threading.Tasks;
using KinaUna.Data.Models;

namespace KinaUnaWeb.Services.HttpClients
{
    /// <summary>
    /// Provides methods for interacting with the Measurements API.
    /// </summary>
    public interface IMeasurementsHttpClient
    {
        /// <summary>
        /// Gets the Measurement with the given MeasurementId.
        /// </summary>
        /// <param name="measurementId">The MeasurementId of the Measurement to get.</param>
        /// <returns>The Measurement object with the given MeasurementId.</returns>
        Task<Measurement> GetMeasurement(int measurementId);

        /// <summary>
        /// Adds a new Measurement. 
        /// </summary>
        /// <param name="measurement">The Measurement object to be added.</param>
        /// <returns>The Measurement object that was added.</returns>
        Task<Measurement> AddMeasurement(Measurement measurement);

        /// <summary>
        /// Updates a Measurement. The Measurement with the same MeasurementId will be updated.
        /// </summary>
        /// <param name="measurement">The Measurement with the updated properties.</param>
        /// <returns>The updated Measurement object.</returns>
        Task<Measurement> UpdateMeasurement(Measurement measurement);

        /// <summary>
        /// Removes the Measurement with a given MeasurementId.
        /// </summary>
        /// <param name="measurementId">The MeasurementId of the Measurement to remove.</param>
        /// <returns>bool: True if the Measurement was successfully removed.</returns>
        Task<bool> DeleteMeasurement(int measurementId);

        /// <summary>
        /// Gets the list of Measurements for a progeny that a user has access to.
        /// </summary>
        /// <param name="progenyId">The Id of the Progeny to get Measurements for.</param>
        /// <returns>List of Measurement objects.</returns>
        Task<List<Measurement>> GetMeasurementsList(int progenyId);
    }
}
