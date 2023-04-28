using System.Collections.Generic;
using System.Threading.Tasks;
using KinaUna.Data.Models;

namespace KinaUnaWeb.Services.HttpClients
{
    public interface IVaccinationsHttpClient
    {
        /// <summary>
        /// Gets the Vaccination with the given VaccinationId.
        /// </summary>
        /// <param name="vaccinationId">int: The Id of the Vaccination (Vaccination.VaccinationId).</param>
        /// <returns>Vaccination: The Vaccination object.</returns>
        Task<Vaccination> GetVaccination(int vaccinationId);

        /// <summary>
        /// Adds a new Vaccination.
        /// </summary>
        /// <param name="vaccination">Vaccination: The new Vaccination to add.</param>
        /// <returns>Vaccination</returns>
        Task<Vaccination> AddVaccination(Vaccination vaccination);

        /// <summary>
        /// Updates a Vaccination. The Vaccination with the same VaccinationId will be updated.
        /// </summary>
        /// <param name="vaccination">Vaccination: The Vaccination to update.</param>
        /// <returns>Vaccination: The updated Vaccination.</returns>
        Task<Vaccination> UpdateVaccination(Vaccination vaccination);

        /// <summary>
        /// Removes the Vaccination with the given VaccinationId.
        /// </summary>
        /// <param name="vaccinationId">int: The Id of the Vaccination to remove (Vaccination.VaccinationId).</param>
        /// <returns>bool: True if the Vaccination was successfully removed.</returns>
        Task<bool> DeleteVaccination(int vaccinationId);

        /// <summary>
        /// Gets a progeny's list of Vaccinations that a user has access to.
        /// </summary>
        /// <param name="progenyId">int: The Id of the progeny (Progeny.Id).</param>
        /// <param name="accessLevel">int: The user's access level.</param>
        /// <returns>List of Vaccination objects.</returns>
        Task<List<Vaccination>> GetVaccinationsList(int progenyId, int accessLevel);
    }
}
