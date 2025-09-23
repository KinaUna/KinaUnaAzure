using KinaUna.Data.Models;
using KinaUna.Data.Models.AccessManagement;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace KinaUnaProgenyApi.Services.AccessManagementService
{
    /// <summary>
    /// Defines methods for managing and verifying user permissions for resources.
    /// </summary>
    /// <remarks>This interface provides functionality to check, grant, revoke, and update permissions for
    /// users on specific resources. It also allows retrieving permissions for a given resource or user. Implementations
    /// of this interface should ensure proper validation and enforcement of permission rules.</remarks>
    public interface IAccessManagementService
    {
        /// <summary>
        /// Determines whether the specified user has the required permission level for a given resource.
        /// </summary>
        /// <remarks>This method checks both the user's direct permissions and their group permissions for
        /// the specified resource. If the user has sufficient direct permissions, the method returns <see
        /// langword="true"/> immediately. Otherwise, it evaluates the highest permission level granted through the
        /// user's group memberships.</remarks>
        /// <param name="userId">The unique identifier of the user whose permissions are being checked. Cannot be <see langword="null"/> or
        /// empty.</param>
        /// <param name="permissionType">The type of permission to check (e.g., timeline item, family member, or family).</param>
        /// <param name="requiredLevel">The minimum permission level required to access the resource.</param>
        /// <param name="resourceId">The unique identifier of the resource for which permissions are being checked.</param>
        /// <param name="timelineType">An optional parameter specifying the timeline type associated with the resource. Can be <see
        /// langword="null"/> if not applicable.</param>
        /// <returns><see langword="true"/> if the user has the required permission level for the resource; otherwise, <see
        /// langword="false"/>.</returns>
        Task<bool> HasPermissionAsync(string userId, PermissionType permissionType, PermissionLevel requiredLevel, int resourceId, KinaUnaTypes.TimeLineType? timelineType);

        /// <summary>
        /// Grants a specified permission to a user or group for a resource, if the current user has the necessary
        /// access rights.
        /// </summary>
        /// <remarks>This method ensures that the current user has the necessary access rights to grant
        /// the specified permission. If the permission already exists,  or if the current user does not have sufficient
        /// privileges, the method returns <see langword="null"/>. The method also sets the creation and modification
        /// timestamps  for the new permission before saving it to the database.</remarks>
        /// <param name="entityId">The identifier of the entity associated with the resource. E.g. ProgenyId, FamilyId, FamilyMemberId</param>
        /// <param name="resourcePermission">The permission to be granted, including details such as the user or group, resource, and permission type.</param>
        /// <param name="currentUserInfo">Information about the current user attempting to grant the permission.</param>
        /// <returns>The granted <see cref="ResourcePermission"/> object if the operation is successful; otherwise, <see
        /// langword="null"/> if the current user lacks the required access rights  or if the specified permission
        /// already exists.</returns>
        Task<ResourcePermission> GrantPermission(int entityId, ResourcePermission resourcePermission, UserInfo currentUserInfo);

        /// <summary>
        /// Revokes a specific permission for a resource from a user or group.
        /// </summary>
        /// <remarks>The method checks whether the current user has sufficient access rights to revoke the
        /// specified permission. If the permission does not exist or the user lacks the necessary access rights, the
        /// method returns <see langword="false"/>.</remarks>
        /// <param name="entityId">The identifier of the entity (e.g., ProgenyId, FamilyId or FamilyMemberId) associated with the permission.</param>
        /// <param name="resourcePermission">The permission to be revoked, including details such as the user or group, permission type, and resource.</param>
        /// <param name="currentUserInfo">The information of the user attempting to revoke the permission.</param>
        /// <returns><see langword="true"/> if the permission was successfully revoked; otherwise, <see langword="false"/>.</returns>
        Task<bool> RevokePermission(int entityId, ResourcePermission resourcePermission, UserInfo currentUserInfo);

        /// <summary>
        /// Updates the permission level of an existing resource permission for a specified entity.
        /// </summary>
        /// <remarks>This method validates whether the current user has the necessary access rights to
        /// update the specified permission level. If the user does not have sufficient rights or the specified
        /// permission does not exist, the method returns <see langword="null"/>.</remarks>
        /// <param name="entityId">The identifier of the entity associated with the resource permission (e.g., ProgenyId, FamilyId or FamilyMemberId).</param>
        /// <param name="resourcePermission">The updated resource permission details, including the new permission level.</param>
        /// <param name="currentUserInfo">Information about the current user performing the update, used to validate access rights.</param>
        /// <returns>The updated <see cref="ResourcePermission"/> object if the operation is successful; otherwise, <see
        /// langword="null"/> if the user lacks sufficient access rights or the specified permission does not exist.</returns>
        Task<ResourcePermission> UpdatePermission(int entityId, ResourcePermission resourcePermission, UserInfo currentUserInfo);

        /// <summary>
        /// Retrieves a list of permissions for a specific resource based on the specified criteria.
        /// </summary>
        /// <remarks>This method performs a database query to retrieve permissions that match the provided
        /// <paramref name="permissionType"/>, <paramref name="resourceId"/>, and <paramref name="timelineType"/>. The
        /// query is executed with no tracking to improve performance for read-only operations.</remarks>
        /// <param name="permissionType">The type of permission to filter by (e.g. TimelineItem, FamilyMember, or Family). This determines the category of permissions to retrieve.</param>
        /// <param name="resourceId">The unique identifier of the resource for which permissions are being retrieved.</param>
        /// <param name="timelineType">An optional parameter specifying the timeline type associated with the resource. If null, only permissions
        /// with a null timeline type will be considered.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains a list of <see
        /// cref="ResourcePermission"/> objects matching the specified criteria. If no permissions are found, the list
        /// will be empty.</returns>
        Task<List<ResourcePermission>> GetPermissionsForResource(PermissionType permissionType, int resourceId, KinaUnaTypes.TimeLineType? timelineType);

        /// <summary>
        /// Retrieves a list of resource permissions associated with the specified user.
        /// </summary>
        /// <remarks>This method queries the database to retrieve permissions associated with the user.
        /// Permissions can be  assigned directly to the user or inherited through their membership in user groups.</remarks>
        /// <param name="userId">The unique identifier of the user whose permissions are to be retrieved. Cannot be null or empty.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains a list of  <see
        /// cref="ResourcePermission"/> objects representing the permissions assigned directly to the user  or through
        /// their membership in user groups. Returns an empty list if no permissions are found.</returns>
        Task<List<ResourcePermission>> GetPermissionsForUser(string userId);

        /// <summary>
        /// Retrieves the list of resource permissions associated with the specified group.
        /// </summary>
        /// <remarks>This method queries the database for resource permissions linked to the specified
        /// group. The results are retrieved without tracking changes to the entities.</remarks>
        /// <param name="groupId">The unique identifier of the group whose permissions are to be retrieved.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains a list of <see
        /// cref="ResourcePermission"/> objects associated with the specified group. If no permissions are found, an
        /// empty list is returned.</returns>
        Task<List<ResourcePermission>> GetPermissionsForGroup(int groupId);
    }
}
