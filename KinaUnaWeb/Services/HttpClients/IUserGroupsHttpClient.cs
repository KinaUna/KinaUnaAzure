using KinaUna.Data.Models.AccessManagement;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace KinaUnaWeb.Services.HttpClients
{
    public interface IUserGroupsHttpClient
    {
        /// <summary>
        /// Gets the list of user groups for a specific family.
        /// </summary>
        /// <param name="familyId">The unique identifier of the family.</param>
        /// <returns>A list of <see cref="UserGroup"/> objects associated with the specified family. If no groups are found or an error occurs, an empty list is returned.</returns>
        Task<List<UserGroup>> GetUserGroupsForFamily(int familyId);

        /// <summary>
        /// Gets the list of user groups for a specific progeny.
        /// </summary>
        /// <param name="progenyId">The unique identifier of the progeny.</param>
        /// <returns>A list of <see cref="UserGroup"/> objects associated with the specified progeny. If no groups are found or an error occurs, an empty list is returned.</returns>
        Task<List<UserGroup>> GetUserGroupsForProgeny(int progenyId);


    }
}
