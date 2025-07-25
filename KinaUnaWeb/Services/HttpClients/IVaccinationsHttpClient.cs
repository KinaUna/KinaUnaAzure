using System.Collections.Generic;
using System.Threading.Tasks;

namespace KinaUnaWeb.Services.HttpClients
{
    /// <summary>
    /// Provides methods to interact with the Vaccinations API Controller.
    /// </summary>
    public interface IVaccinationsHttpClient
    {
        /// <summary>
        /// Gets the Vaccination with the given VaccinationId.
        /// </summary>
        /// <param name="vaccinationId">The VaccinationId of the Vaccination to get.</param>
        /// <returns>The Vaccination object with the given VaccinationId. If not found, a new Vaccination object with VaccinationId=0 is returned.</returns>
        Task<Vaccination> GetVaccination(int vaccinationId);

        /// <summary>
        /// Adds a new Vaccination.
        /// </summary>
        /// <param name="vaccination">The new Vaccination to add.</param>
        /// <returns>The added Vaccination object</returns>
        Task<Vaccination> AddVaccination(Vaccination vaccination);

        /// <summary>
        /// Updates a Vaccination. The Vaccination with the same VaccinationId will be updated.
        /// </summary>
        /// <param name="vaccination">The Vaccination object with the updated properties.</param>
        /// <returns>The updated Vaccination. If not found, a new Vaccination object with VaccinationId=0 is returned.</returns>
        Task<Vaccination> UpdateVaccination(Vaccination vaccination);

        /// <summary>
        /// Removes the Vaccination with the given VaccinationId.
        /// </summary>
        /// <param name="vaccinationId">int: The VaccinationId of the Vaccination to remove.</param>
        /// <returns>bool: True if the Vaccination was successfully removed.</returns>
        Task<bool> DeleteVaccination(int vaccinationId);

        /// <summary>
        /// Gets the list of all Vaccinations for a Progeny that a user has access to.
        /// </summary>
        /// <param name="progenyId">The Id of the Progeny to get Vaccinations for.</param>
        /// <returns>List of Vaccination objects.</returns>
        Task<List<Vaccination>> GetVaccinationsList(int progenyId);
    }
}
