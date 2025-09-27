using KinaUna.Data.Models.Family;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace KinaUnaWeb.Services.HttpClients
{
    /// <summary>
    /// Provides methods for interacting with the Families API, including operations for managing families and family
    /// members. This client handles authentication and communicates with the API using HTTP requests.
    /// </summary>
    /// <remarks>This class is designed to be used in applications that require interaction with the Families
    /// API. It supports operations such as retrieving, adding, updating, and deleting families and family members. The
    /// client automatically manages authentication tokens and sets the appropriate headers for API requests.</remarks>
    public interface IFamiliesHttpClient
    {
        /// <summary>
        /// Retrieves the list of families associated with the currently signed-in user.
        /// </summary>
        /// <remarks>This method fetches the families for the currently authenticated user by making an
        /// HTTP request to the external API endpoint. If the request fails or the user is not authenticated, an empty
        /// list is returned.</remarks>
        /// <returns>A <see cref="Task{TResult}"/> representing the asynchronous operation. The task result contains a list of
        /// <see cref="Family"/> objects representing the families associated with the user. If no families are found or
        /// the request fails, the list will be empty.</returns>
        Task<List<Family>> GetMyFamilies();

        /// <summary>
        /// Retrieves the details of a family by its unique identifier.
        /// </summary>
        /// <remarks>This method requires the caller to be authenticated. The method retrieves a valid
        /// access token  for the signed-in user and uses it to authenticate the request to the external Families
        /// API.</remarks>
        /// <param name="familyId">The unique identifier of the family to retrieve.</param>
        /// <returns>A <see cref="Family"/> object containing the details of the specified family.  If the family is not found or
        /// an error occurs, an empty <see cref="Family"/> object is returned.</returns>
        Task<Family> GetFamily(int familyId);

        /// <summary>
        /// Adds a new family by sending the provided family data to the external API.
        /// </summary>
        /// <remarks>This method retrieves a valid access token for the signed-in user and includes it in
        /// the request to the external API. Ensure that the <paramref name="family"/> parameter contains valid data
        /// before calling this method.</remarks>
        /// <param name="family">The <see cref="Family"/> object containing the details of the family to be added.</param>
        /// <returns>A <see cref="Family"/> object representing the newly created family as returned by the external API.  If the
        /// operation fails, an empty <see cref="Family"/> object is returned.</returns>
        Task<Family> AddFamily(Family family);

        /// <summary>
        /// Updates the specified family entity in the remote system.
        /// </summary>
        /// <remarks>This method sends an HTTP PUT request to the remote system to update the family
        /// entity.  The caller must ensure that the <paramref name="family"/> parameter contains valid data. The method
        /// uses the signed-in user's credentials to authenticate the request.</remarks>
        /// <param name="family">The <see cref="Family"/> object containing the updated information to be sent to the remote system.</param>
        /// <returns>A <see cref="Family"/> object representing the updated family entity as returned by the remote system. If
        /// the update operation fails, an empty <see cref="Family"/> object is returned.</returns>
        Task<Family> UpdateFamily(Family family);

        /// <summary>
        /// Deletes a family with the specified identifier.
        /// </summary>
        /// <remarks>This method sends an HTTP DELETE request to the backend service to remove the family.
        /// The caller must ensure that the <paramref name="familyId"/> corresponds to a valid family.</remarks>
        /// <param name="familyId">The unique identifier of the family to delete.</param>
        /// <returns><see langword="true"/> if the family was successfully deleted; otherwise, <see langword="false"/>.</returns>
        Task<bool> DeleteFamily(int familyId);

        /// <summary>
        /// Adds a new family member to the family associated with the current user.
        /// </summary>
        /// <remarks>This method sends a request to the backend service to add the specified family
        /// member. The operation is performed on behalf of the currently signed-in user, as determined by the user's
        /// authentication context.</remarks>
        /// <param name="familyMember">The <see cref="FamilyMember"/> object containing the details of the family member to add.</param>
        /// <returns>A <see cref="FamilyMember"/> object representing the newly added family member, or a default <see
        /// cref="FamilyMember"/> object if the operation fails.</returns>
        Task<FamilyMember> AddFamilyMember(FamilyMember familyMember);

        /// <summary>
        /// Updates the details of a family member in the system.
        /// </summary>
        /// <remarks>This method sends an HTTP PUT request to the server to update the family member's
        /// details. The caller must ensure that the <paramref name="familyMember"/> parameter contains valid
        /// data.</remarks>
        /// <param name="familyMember">The <see cref="FamilyMember"/> object containing the updated details of the family member.</param>
        /// <returns>A <see cref="FamilyMember"/> object representing the updated family member details as returned by the
        /// server. If the update operation fails, an empty <see cref="FamilyMember"/> object is returned.</returns>
        Task<FamilyMember> UpdateFamilyMember(FamilyMember familyMember);

        /// <summary>
        /// Deletes a family member with the specified identifier.
        /// </summary>
        /// <remarks>This method sends an HTTP DELETE request to the backend service to remove the
        /// specified family member. The caller must ensure that the <paramref name="familyMemberId"/> corresponds to a
        /// valid family member.</remarks>
        /// <param name="familyMemberId">The unique identifier of the family member to delete.</param>
        /// <returns><see langword="true"/> if the family member was successfully deleted; otherwise, <see langword="false"/>.</returns>
        Task<bool> DeleteFamilyMember(int familyMemberId);

        /// <summary>
        /// Retrieves a family member by their unique identifier.
        /// </summary>
        /// <remarks>This method sends an HTTP request to the Families API to retrieve the family member's
        /// details. The caller must ensure that the signed-in user has the necessary permissions to access the
        /// requested family member.</remarks>
        /// <param name="familyMemberId">The unique identifier of the family member to retrieve.</param>
        /// <returns>A <see cref="FamilyMember"/> object representing the family member with the specified identifier. If the
        /// family member is not found or the request fails, an empty <see cref="FamilyMember"/> object is returned.</returns>
        Task<FamilyMember> GetFamilyMember(int familyMemberId);

        /// <summary>
        /// Retrieves the list of family members associated with a specific family.
        /// </summary>
        /// <param name="familyId">The unique identifier of the family whose members are to be retrieved.</param>
        /// <returns>List of <see cref="FamilyMember"/> objects representing the members of the specified family. If no members are found or the request fails, an empty list is returned.</returns>
        Task<List<FamilyMember>> GetFamilyMembersForFamily(int familyId);

    }
}
