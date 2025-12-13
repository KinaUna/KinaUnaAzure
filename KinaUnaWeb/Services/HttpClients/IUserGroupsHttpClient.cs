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

        /// <summary>
        /// Retrieves the details of a user group by its unique identifier.
        /// </summary>
        /// <remarks>This method requires the caller to be authenticated. The method retrieves a valid
        /// access token  for the signed-in user and includes it in the request to the user groups API.</remarks>
        /// <param name="userGroupId">The unique identifier of the user group to retrieve.</param>
        /// <returns>A <see cref="UserGroup"/> object containing the details of the specified user group.  If the user group is
        /// not found or the request fails, an empty <see cref="UserGroup"/> object is returned.</returns>
        Task<UserGroup> GetUserGroup(int userGroupId);

        /// <summary>
        /// Adds a new user group.
        /// </summary>
        /// <param name="userGroup">The user group to add</param>
        /// <returns>The added user group</returns>
        Task<UserGroup> AddUserGroup(UserGroup userGroup);

        /// <summary>
        /// Updates an existing user group.
        /// </summary>
        /// <param name="userGroup">The user group to update</param>
        /// <returns>The updated user group</returns>
        Task<UserGroup> UpdateUserGroup(UserGroup userGroup);

        /// <summary>
        /// Deletes a user group with the specified identifier.
        /// </summary>
        /// <remarks>This method sends an HTTP DELETE request to the user groups API to remove the
        /// specified user group. The caller must ensure that the <paramref name="userGroupId"/> corresponds to a valid
        /// user group.</remarks>
        /// <param name="userGroupId">The unique identifier of the user group to delete.</param>
        /// <returns><see langword="true"/> if the user group was successfully deleted; otherwise, <see langword="false"/>.</returns>
        Task<bool> DeleteUserGroup(int userGroupId);

        /// <summary>
        /// Gets a user group member by their unique identifier.
        /// </summary>
        /// <param name="userGroupMemberId">The unique identifier of the user group member to retrieve.</param>
        /// <returns>The <see cref="UserGroupMember"/> object containing the details of the specified user group member. If the user group member is not found or the request fails, an empty <see cref="UserGroupMember"/> object is returned.</returns>
        Task<UserGroupMember> GetUserGroupMember(int userGroupMemberId);

        /// <summary>
        /// Adds a new user to a user group.
        /// </summary>
        /// <param name="userGroupMember">The user group member to add</param>
        /// <returns>The added user group member</returns>
        Task<UserGroupMember> AddUserGroupMember(UserGroupMember userGroupMember);

        /// <summary>
        /// Updates the details of an existing user group member.
        /// </summary>
        /// <remarks>This method sends an HTTP PUT request to the User Groups API to update the specified
        /// user group member. The caller must ensure that the <paramref name="userGroupMember"/> parameter contains
        /// valid data.</remarks>
        /// <param name="userGroupMember">The <see cref="UserGroupMember"/> object containing the updated details of the user group member.</param>
        /// <returns>A <see cref="UserGroupMember"/> object representing the updated user group member.  If the update operation
        /// fails, an empty <see cref="UserGroupMember"/> object is returned.</returns>
        Task<UserGroupMember> UpdateUserGroupMember(UserGroupMember userGroupMember);

        /// <summary>
        /// Removes a user group member with the specified identifier.
        /// </summary>
        /// <remarks>This method sends a DELETE request to the User Groups API to remove the specified
        /// user group member. The operation requires a valid access token, which is retrieved for the currently
        /// signed-in user.</remarks>
        /// <param name="userGroupMemberId">The unique identifier of the user group member to be removed.</param>
        /// <returns><see langword="true"/> if the user group member was successfully removed; otherwise, <see
        /// langword="false"/>.</returns>
        Task<bool> RemoveUserGroupMember(int userGroupMemberId);
    }
}
