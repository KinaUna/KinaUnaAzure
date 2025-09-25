using KinaUna.Data.Models;
using KinaUna.Data.Models.AccessManagement;
using System.Threading.Tasks;

namespace KinaUnaProgenyApi.Services.AccessManagementService
{
    public interface IUserGroupService
    {
        /// <summary>
        /// Gets a user group by its unique identifier, including its members, if the current user has the necessary permissions.
        /// </summary>
        /// <param name="groupId">The unique identifier of the user group to retrieve.</param>
        /// <param name="currentUserInfo">The information about the current user, used to verify access permissions.</param>
        /// <returns>The <see cref="UserGroup"/> object representing the requested user group, including its members, if the user has access.</returns>
        Task<UserGroup> GetUserGroup(int groupId, UserInfo currentUserInfo);

        /// <summary>
        /// Adds a new user group to the database.
        /// </summary>
        /// <param name="userGroup">The <see cref="UserGroup"/> object to be added. This object must contain the necessary details about the user group.</param>
        /// <param name="currentUserInfo">The information about the current user, used to verify access permissions.</param>
        /// <returns>The added <see cref="UserGroup"/> object if the operation is successful; otherwise, <see langword="null"/> if the current user lacks the required access rights.</returns>
        Task<UserGroup> AddUserGroup(UserGroup userGroup, UserInfo currentUserInfo);

        /// <summary>
        /// Updates the details of an existing user group in the database.
        /// </summary>
        /// <param name="userGroup">The <see cref="UserGroup"/> object containing the updated details of the user group. The <see cref="UserGroup.UserGroupId"/> property must correspond to an existing user group.</param>
        /// <param name="currentUserInfo">The information about the current user, used to verify access permissions.</param>
        /// <returns>The updated <see cref="UserGroup"/> object if the operation is successful; otherwise, <see langword="null"/> if the user group does not exist or the current user lacks the required access rights.</returns>
        Task<UserGroup> UpdateUserGroup(UserGroup userGroup, UserInfo currentUserInfo);

        /// <summary>
        /// Removes a user group from the database, including all its members.
        /// </summary>
        /// <param name="groupId">The unique identifier of the user group to be removed.</param>
        /// <param name="currentUserInfo">The information about the current user, used to verify access permissions.</param>
        /// <returns>Boolean value indicating whether the user group was successfully removed. Returns <see langword="true"/>
        /// if the operation is successful; otherwise, <see langword="false"/>
        /// if the user group does not exist or the current user lacks the required access rights.</returns>
        Task<bool> RemoveUserGroup(int groupId, UserInfo currentUserInfo);

        /// <summary>
        /// Adds a new member to a user group.
        /// </summary>
        /// <param name="userGroupMember">The <see cref="UserGroupMember"/> object representing the member to add. The <see cref="UserGroupMember.UserGroupId"/> property must be set to the ID of the target user group.</param>
        /// <param name="currentUserInfo">The <see cref="UserInfo"/> object representing the user performing the operation. This user must have sufficient permissions to add members to the specified user group.</param>
        /// <returns>UserGroupMember object representing the newly added member, or <see langword="null"/> if the operation fails due to insufficient permissions or an invalid user group ID.</returns>
        Task<UserGroupMember> AddUserGroupMember(UserGroupMember userGroupMember, UserInfo currentUserInfo);

        /// <summary>
        /// Updates a user group member.
        /// </summary>
        /// <param name="userGroupMember">The <see cref="UserGroupMember"/> object containing the updated details of the user group member. The <see cref="UserGroupMember.UserGroupMemberId"/> property must correspond to an existing user group member.</param>
        /// <param name="currentUserInfo">The information about the current user, used to verify access permissions.</param>
        /// <returns>The updated <see cref="UserGroupMember"/> object if the operation is successful; otherwise, <see langword="null"/> if the user group member does not exist or the current user lacks the required access rights.</returns>
        Task<UserGroupMember> UpdateUserGroupMember(UserGroupMember userGroupMember, UserInfo currentUserInfo);

        /// <summary>
        /// Removes a user group member from the database.
        /// </summary>
        /// <param name="userGroupMemberId">The unique identifier of the user group member to be removed.</param>
        /// <param name="currentUserInfo">The information about the current user, used to verify access permissions.</param>
        /// <returns>True if the user group member was successfully removed; otherwise, false if the user group member does not exist or the current user lacks the required access rights.</returns>
        Task<bool> RemoveUserGroupMember(int userGroupMemberId, UserInfo currentUserInfo);
    }
}
