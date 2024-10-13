using System.Collections.Generic;
using System.Threading.Tasks;
using KinaUna.Data.Models;

namespace KinaUnaProgenyApi.Services
{
    public interface IVaccinationService
    {
        /// <summary>
        /// Gets a Vaccination entity with the specified VaccinationId.
        /// First checks the cache, if not found, gets the Vaccination from the database and adds it to the cache.
        /// </summary>
        /// <param name="id">The VaccinationId of the Vaccination entity to get.</param>
        /// <returns>The Vaccination object with the given VaccinationId. Null if the Vaccination item doesn't exist.</returns>
        Task<Vaccination> GetVaccination(int id);

        /// <summary>
        /// Adds a new Vaccination entity to the database and adds it to the cache.
        /// </summary>
        /// <param name="vaccination">The Vaccination object to add.</param>
        /// <returns>The added Vaccination object.</returns>
        Task<Vaccination> AddVaccination(Vaccination vaccination);

        /// <summary>
        /// Updates a Vaccination entity in the database and the cache.
        /// </summary>
        /// <param name="vaccination">The Vaccination object with the updated properties.</param>
        /// <returns>The updated Vaccination object.</returns>
        Task<Vaccination> UpdateVaccination(Vaccination vaccination);

        /// <summary>
        /// Deletes a Vaccination entity from the database and the cache.
        /// </summary>
        /// <param name="vaccination">The Vaccination object to delete.</param>
        /// <returns></returns>
        Task<Vaccination> DeleteVaccination(Vaccination vaccination);

        /// <summary>
        /// Gets a list of all Vaccinations for a Progeny.
        /// First checks the cache, if not found, gets the list from the database and adds it to the cache.
        /// </summary>
        /// <param name="progenyId">The ProgenyId of the Progeny to get the list for.</param>
        /// <param name="accessLevel">The access level for the current user.</param>
        /// <returns>List of Vaccination objects.</returns>
        Task<List<Vaccination>> GetVaccinationsList(int progenyId, int accessLevel);
    }
}
