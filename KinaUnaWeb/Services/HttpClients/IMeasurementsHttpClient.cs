using System.Collections.Generic;
using System.Threading.Tasks;
using KinaUna.Data.Models;

namespace KinaUnaWeb.Services.HttpClients
{
    public interface IMeasurementsHttpClient
    {
        /// <summary>
        /// Gets the Measurement with the given MeasurementId.
        /// </summary>
        /// <param name="measurementId">int: The Measurement Id (Measurement.MeasurementId).</param>
        /// <returns>Measurement: The Measurement object.</returns>
        Task<Measurement> GetMeasurement(int measurementId);

        /// <summary>
        /// Adds a new Measurement. 
        /// </summary>
        /// <param name="measurement">Measurement: The Measurement object to be added.</param>
        /// <returns>Measurement: The Measurement object that was added.</returns>
        Task<Measurement> AddMeasurement(Measurement measurement);

        /// <summary>
        /// Updates a Measurement. The Measurement with the same MeasurementId will be updated.
        /// </summary>
        /// <param name="measurement">Measurement: The Measurement to update.</param>
        /// <returns>Measurement: The updated Measurement object.</returns>
        Task<Measurement> UpdateMeasurement(Measurement measurement);

        /// <summary>
        /// Removes the Measurement with a given MeasurementId.
        /// </summary>
        /// <param name="measurementId">int: The Id of the Measurement to remove (Measurement.MeasurementId).</param>
        /// <returns>bool: True if the Measurement was successfully removed.</returns>
        Task<bool> DeleteMeasurement(int measurementId);

        /// <summary>
        /// Gets the list of Measurements for a progeny that a user has access to.
        /// </summary>
        /// <param name="progenyId">int: The Id of the progeny (Progeny.Id).</param>
        /// <param name="accessLevel">int: The user's access level.</param>
        /// <returns>List of Measurement objects.</returns>
        Task<List<Measurement>> GetMeasurementsList(int progenyId, int accessLevel);
    }
}
