using KinaUna.Data.Models;
using KinaUna.Data.Models.AccessManagement;
using KinaUna.Data.Models.DTOs;
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
        /// Determines whether a user has the specified access level for a given timeline item (e.g., Note, TodoItem, Sleep, etc.).
        /// </summary>
        /// <param name="itemType">KinaUnaTypes.TimeLineType of the item whose access to is being checked.</param>
        /// <param name="itemId">The unique identifier of the item whose access to is being checked.</param>
        /// <param name="userInfo">The information of the user whose access is being verified.</param>
        /// <param name="requiredLevel">The minimum permission level required for access.</param>
        /// <returns>Boolean value indicating whether the user has the required access level for the timeline item.</returns>
        Task<bool> HasItemPermission(KinaUnaTypes.TimeLineType itemType, int itemId, UserInfo userInfo, PermissionLevel requiredLevel);

        /// <summary>
        /// Retrieves the permission level for a specific timeline item and user.
        /// </summary>
        /// <remarks>This method checks both direct user permissions and group-based permissions to
        /// determine the highest applicable permission level. Group permissions are considered only if the user is a
        /// member of the respective group.</remarks>
        /// <param name="itemType">The type of the timeline item, represented as a <see cref="KinaUnaTypes.TimeLineType"/>.</param>
        /// <param name="itemId">The unique identifier of the timeline item.</param>
        /// <param name="progenyId">The unique identifier of the progeny associated with the timeline item.</param>
        /// <param name="familyId">The unique identifier of the family associated with the timeline item.</param>
        /// <param name="userInfo">The user information, represented as a <see cref="UserInfo"/> object, for whom the permission is being
        /// retrieved.</param>
        /// <returns>A <see cref="TimelineItemPermission"/> object representing the highest permission level the specified user
        /// has for the given timeline item. If no permissions are found, the returned object will have a <see
        /// cref="PermissionLevel"/> of <see cref="PermissionLevel.None"/>.</returns>
        Task<TimelineItemPermission> GetItemPermissionForUser(KinaUnaTypes.TimeLineType itemType, int itemId, int progenyId, int familyId, UserInfo userInfo);

        /// <summary>
        /// Grants a specified permission to a user or group for a timeline item, if the current user has the necessary
        /// access rights.
        /// </summary>
        /// <remarks>This method ensures that the current user has the necessary access rights to grant
        /// the specified permission. If the permission already exists,  or if the current user does not have sufficient
        /// privileges, the method returns <see langword="null"/>. The method also sets the creation and modification
        /// timestamps  for the new permission before saving it to the database.</remarks>
        /// <param name="timelineItemPermission">The permission to be granted, including details such as the user or group, familyId, and permission type.</param>
        /// <param name="currentUserInfo">Information about the current user attempting to grant the permission.</param>
        /// <returns>The granted <see cref="ProgenyPermission"/> object if the operation is successful; otherwise, <see
        /// langword="null"/> if the current user lacks the required access rights  or if the specified permission
        /// already exists.</returns>
        Task<TimelineItemPermission> GrantItemPermission(TimelineItemPermission timelineItemPermission, UserInfo currentUserInfo);

        /// <summary>
        /// Revokes a specific permission for a timeline item from a user or group.
        /// </summary>
        /// <remarks>The method checks whether the current user has sufficient access rights to revoke the
        /// specified permission. If the permission does not exist or the user lacks the necessary access rights, the
        /// method returns <see langword="false"/>.</remarks>
        /// <param name="timelineItemPermission">The permission to be revoked, including details such as the user or group, permission type, and progenyId.</param>
        /// <param name="currentUserInfo">The information of the user attempting to revoke the permission.</param>
        /// <returns><see langword="true"/> if the permission was successfully revoked; otherwise, <see langword="false"/>.</returns>
        Task<bool> RevokeItemPermission(TimelineItemPermission timelineItemPermission, UserInfo currentUserInfo);

        /// <summary>
        /// Updates the permission level of an existing timelineItem permission.
        /// </summary>
        /// <remarks>This method validates whether the current user has the necessary access rights to
        /// update the specified permission level. If the user does not have sufficient rights or the specified
        /// permission does not exist, the method returns <see langword="null"/>.</remarks>
        /// <param name="timelineItemPermission">The updated timelineItem permission details, including the new permission level.</param>
        /// <param name="currentUserInfo">Information about the current user performing the update, used to validate access rights.</param>
        /// <returns>The updated <see cref="TimelineItemPermission"/> object if the operation is successful; otherwise, <see
        /// langword="null"/> if the user lacks sufficient access rights or the specified permission does not exist.</returns>
        Task<TimelineItemPermission> UpdateItemPermission(TimelineItemPermission timelineItemPermission, UserInfo currentUserInfo);

        /// <summary>
        /// Determines whether a user has the specified access level for a given progeny.
        /// </summary>
        /// <param name="progenyId">The unique identifier of the progeny whose access is being checked.</param>
        /// <param name="userInfo">The information of the user whose access is being verified.</param>
        /// <param name="requiredLevel">The minimum permission level required for access.</param>
        /// <returns>Boolean value indicating whether the user has the required access level for the progeny.</returns>
        Task<bool> HasProgenyPermission(int progenyId, UserInfo userInfo, PermissionLevel requiredLevel);

        /// <summary>
        /// Grants a specified permission to a user or group for a progeny, if the current user has the necessary
        /// access rights.
        /// </summary>
        /// <remarks>This method ensures that the current user has the necessary access rights to grant
        /// the specified permission. If the permission already exists,  or if the current user does not have sufficient
        /// privileges, the method returns <see langword="null"/>. The method also sets the creation and modification
        /// timestamps  for the new permission before saving it to the database.</remarks>
        /// <param name="progenyPermission">The permission to be granted, including details such as the user or group, familyId, and permission type.</param>
        /// <param name="currentUserInfo">Information about the current user attempting to grant the permission.</param>
        /// <returns>The granted <see cref="ProgenyPermission"/> object if the operation is successful; otherwise, <see
        /// langword="null"/> if the current user lacks the required access rights  or if the specified permission
        /// already exists.</returns>
        Task<ProgenyPermission> GrantProgenyPermission(ProgenyPermission progenyPermission, UserInfo currentUserInfo);

        /// <summary>
        /// Revokes a specific permission for a progeny from a user or group.
        /// </summary>
        /// <remarks>The method checks whether the current user has sufficient access rights to revoke the
        /// specified permission. If the permission does not exist or the user lacks the necessary access rights, the
        /// method returns <see langword="false"/>.</remarks>
        /// <param name="progenyPermission">The permission to be revoked, including details such as the user or group, permission type, and progenyId.</param>
        /// <param name="currentUserInfo">The information of the user attempting to revoke the permission.</param>
        /// <returns><see langword="true"/> if the permission was successfully revoked; otherwise, <see langword="false"/>.</returns>
        Task<bool> RevokeProgenyPermission(ProgenyPermission progenyPermission, UserInfo currentUserInfo);

        /// <summary>
        /// Updates the permission level of an existing progeny permission.
        /// </summary>
        /// <remarks>This method validates whether the current user has the necessary access rights to
        /// update the specified permission level. If the user does not have sufficient rights or the specified
        /// permission does not exist, the method returns <see langword="null"/>.</remarks>
        /// <param name="progenyPermission">The updated progeny permission details, including the new permission level.</param>
        /// <param name="currentUserInfo">Information about the current user performing the update, used to validate access rights.</param>
        /// <returns>The updated <see cref="ProgenyPermission"/> object if the operation is successful; otherwise, <see
        /// langword="null"/> if the user lacks sufficient access rights or the specified permission does not exist.</returns>
        Task<ProgenyPermission> UpdateProgenyPermission(ProgenyPermission progenyPermission, UserInfo currentUserInfo);

        /// <summary>
        /// Retrieves a list of permissions associated with a specified progeny.
        /// </summary>
        /// <remarks>This method checks whether the current user has the necessary access rights to manage
        /// permissions for the specified progeny. If the user does not have sufficient permissions, an empty list is
        /// returned.</remarks>
        /// <param name="progenyId">The unique identifier of the progeny whose permissions are to be retrieved.</param>
        /// <param name="currentUserInfo">The user information of the current user making the request. This is used to verify access permissions.</param>
        /// <returns>A list of <see cref="ProgenyPermission"/> objects representing the permissions for the specified progeny. 
        /// Returns an empty list if the current user does not have the required access rights.</returns>
        Task<List<ProgenyPermission>> GetProgenyPermissionsList(int progenyId, UserInfo currentUserInfo);

        /// <summary>
        /// Updates the administrative permissions for a specified progeny based on the current list of administrators.
        /// </summary>
        /// <remarks>This method synchronizes the administrative permissions for the specified progeny by
        /// performing the following actions: <list type="bullet"> <item><description>Downgrades permissions for users
        /// who are no longer in the current list of administrators.</description></item> <item><description>Adds or
        /// updates permissions for new administrators in the list.</description></item> </list> The method ensures that
        /// the database reflects the current state of the progeny's administrative list.</remarks>
        /// <param name="progenyId">The unique identifier of the progeny whose administrative permissions are to be updated.</param>
        /// <returns><see langword="true"/> if the administrative permissions were successfully updated;  otherwise, <see
        /// langword="false"/> if the specified progeny does not exist.</returns>
        Task<bool> ProgenyAdminsUpdated(int progenyId);

        /// <summary>
        /// Determines whether a user has the specified access level for a given family.
        /// </summary>
        /// <param name="familyId">The unique identifier of the family whose access to is being checked.</param>
        /// <param name="userInfo">The information of the user whose access is being verified.</param>
        /// <param name="requiredLevel">The minimum permission level required for access.</param>
        /// <returns>Boolean value indicating whether the user has the required access level for the family.</returns>
        Task<bool> HasFamilyPermission(int familyId, UserInfo userInfo, PermissionLevel requiredLevel);

        /// <summary>
        /// Grants a specified permission to a user or group for a family, if the current user has the necessary
        /// access rights.
        /// </summary>
        /// <remarks>This method ensures that the current user has the necessary access rights to grant
        /// the specified permission. If the permission already exists,  or if the current user does not have sufficient
        /// privileges, the method returns <see langword="null"/>. The method also sets the creation and modification
        /// timestamps  for the new permission before saving it to the database.</remarks>
        /// <param name="familyPermission">The permission to be granted, including details such as the user or group, familyId, and permission type.</param>
        /// <param name="currentUserInfo">Information about the current user attempting to grant the permission.</param>
        /// <returns>The granted <see cref="FamilyPermission"/> object if the operation is successful; otherwise, <see
        /// langword="null"/> if the current user lacks the required access rights  or if the specified permission
        /// already exists.</returns>
        Task<FamilyPermission> GrantFamilyPermission(FamilyPermission familyPermission, UserInfo currentUserInfo);

        /// <summary>
        /// Revokes a specific permission for a family from a user or group.
        /// </summary>
        /// <remarks>The method checks whether the current user has sufficient access rights to revoke the
        /// specified permission. If the permission does not exist or the user lacks the necessary access rights, the
        /// method returns <see langword="false"/>.</remarks>
        /// <param name="familyPermission">The permission to be revoked, including details such as the user or group, permission type, and familyId.</param>
        /// <param name="currentUserInfo">The information of the user attempting to revoke the permission.</param>
        /// <returns><see langword="true"/> if the permission was successfully revoked; otherwise, <see langword="false"/>.</returns>
        Task<bool> RevokeFamilyPermission(FamilyPermission familyPermission, UserInfo currentUserInfo);

        /// <summary>
        /// Updates the permission level of an existing family permission.
        /// </summary>
        /// <remarks>This method validates whether the current user has the necessary access rights to
        /// update the specified permission level. If the user does not have sufficient rights or the specified
        /// permission does not exist, the method returns <see langword="null"/>.</remarks>
        /// <param name="familyPermission">The updated family permission details, including the new permission level.</param>
        /// <param name="currentUserInfo">Information about the current user performing the update, used to validate access rights.</param>
        /// <returns>The updated <see cref="FamilyPermission"/> object if the operation is successful; otherwise, <see
        /// langword="null"/> if the user lacks sufficient access rights or the specified permission does not exist.</returns>
        Task<FamilyPermission> UpdateFamilyPermission(FamilyPermission familyPermission, UserInfo currentUserInfo);

        /// <summary>
        /// Retrieves a list of permissions associated with a specific family.
        /// </summary>
        /// <remarks>The method checks whether the current user has the necessary access rights to manage
        /// the specified family before retrieving the permissions. If the user lacks the required permissions, an empty
        /// list is returned.</remarks>
        /// <param name="familyId">The unique identifier of the family whose permissions are to be retrieved.</param>
        /// <param name="currentUserInfo">The information of the user making the request, used to verify access permissions.</param>
        /// <returns>A list of <see cref="FamilyPermission"/> objects representing the permissions for the specified family.
        /// Returns an empty list if the user does not have access to manage the specified family.</returns>
        Task<List<FamilyPermission>> GetFamilyPermissionsList(int familyId, UserInfo currentUserInfo);

        /// <summary>
        /// Retrieves the permission settings for a specific progeny and user group.
        /// </summary>
        /// <remarks>This method checks whether the current user has the necessary access rights to
        /// retrieve the permission settings. If the user does not have access, the method returns <see
        /// langword="null"/>.</remarks>
        /// <param name="progenyId">The unique identifier of the progeny. Must be greater than 0.</param>
        /// <param name="userGroupId">The unique identifier of the user group. Must be greater than 0.</param>
        /// <param name="currentUserInfo">The information of the current user making the request. Used to verify access permissions.</param>
        /// <returns>A <see cref="ProgenyPermission"/> object representing the permission settings for the specified progeny and
        /// user group,  or <see langword="null"/> if the progeny or user group does not exist, the user lacks access,
        /// or the identifiers are invalid.</returns>
        Task<ProgenyPermission> GetProgenyPermissionForGroup(int progenyId, int userGroupId, UserInfo currentUserInfo);

        /// <summary>
        /// Retrieves the family permission associated with a specific family and user group.
        /// </summary>
        /// <remarks>The method checks whether the current user has access to manage permissions for the
        /// specified family  before attempting to retrieve the associated family permission. If the user does not have
        /// the required  access, the method returns <c>null</c>.</remarks>
        /// <param name="familyId">The unique identifier of the family. Must be greater than zero.</param>
        /// <param name="userGroupId">The unique identifier of the user group. Must be greater than zero.</param>
        /// <param name="currentUserInfo">The information of the current user making the request. Cannot be <c>null</c>.</param>
        /// <returns>A <see cref="FamilyPermission"/> object representing the permission for the specified family and user group,
        /// or <c>null</c> if the identifiers are invalid, the user lacks access, or no matching permission is found.</returns>
        Task<FamilyPermission> GetFamilyPermissionForGroup(int familyId, int userGroupId, UserInfo currentUserInfo);

        /// <summary>
        /// Retrieves a list of progeny IDs that the specified user can access based on their permissions.
        /// </summary>
        /// <remarks>This method aggregates permissions directly assigned to the user as well as
        /// permissions granted through user groups.</remarks>
        /// <param name="userInfo">The user information, including the user's unique identifier.</param>
        /// <param name="permissionLevel">The required permission level to access the progeny. This parameter is currently unused but may be used in
        /// future implementations to filter results based on permission levels.</param>
        /// <returns>A list of integers representing the IDs of progenies the user has access to. The list contains distinct IDs
        /// and may be empty if the user has no access to any progeny.</returns>
        Task<List<int>> ProgeniesUserCanAccess(UserInfo userInfo, PermissionLevel permissionLevel);

        /// <summary>
        /// Retrieves a list of family IDs that the specified user can access based on the given permission level.
        /// </summary>
        /// <remarks>This method checks both user-specific permissions and permissions granted through
        /// user groups. If the user  has sufficient permissions for a family either directly or through a group, the
        /// family ID will be included  in the result.</remarks>
        /// <param name="userInfo">The user information, including the user's unique identifier.</param>
        /// <param name="permissionLevel">The minimum permission level required to access a family. Only families where the user has this level of
        /// access  or higher will be included in the result.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains a list of distinct family IDs 
        /// that the user can access.</returns>
        Task<List<int>> FamiliesUserCanAccess(UserInfo userInfo, PermissionLevel permissionLevel);

        /// <summary>
        /// Updates the email address associated with a user's permissions across all relevant entities.
        /// </summary>
        /// <remarks>This method updates the email address for all permissions related to the specified
        /// user in the Progeny, Family, and TimelineItem permissions databases. The changes are persisted to the
        /// database upon successful completion of the operation.</remarks>
        /// <param name="userInfo">The user information containing the user's unique identifier.</param>
        /// <param name="newEmail">The new email address to associate with the user's permissions.</param>
        /// <returns>A task that represents the asynchronous operation.</returns>
        Task ChangeUsersEmailForPermissions(UserInfo userInfo, string newEmail);

        /// <summary>
        /// Sets the user ID for all permissions associated with a new user based on their email address.
        /// </summary>
        /// <param name="userInfo">The user information containing the user's unique identifier and email address.</param>
        /// <returns></returns>
        Task UpdatePermissionsForNewUser(UserInfo userInfo);

        /// <summary>
        /// Adds permissions to a timeline item for specified users or groups.
        /// </summary>
        /// <remarks>This method processes each permission in the <paramref
        /// name="itemPermissionsDtoList"/> and associates it with the specified timeline item. Permissions can be
        /// granted to individual users or groups based on the provided identifiers in the <see
        /// cref="ItemPermissionDto"/> objects. If a permission references a non-existent progeny or family permission,
        /// it will be skipped.</remarks>
        /// <param name="itemType">The type of the timeline item to which permissions will be added.</param>
        /// <param name="itemId">The unique identifier of the timeline item.</param>
        /// <param name="progenyId">The unique identifier of the progeny associated with the timeline item.</param>
        /// <param name="familyId">The unique identifier of the family associated with the timeline item.</param>
        /// <param name="itemPermissionsDtoList">A list of <see cref="ItemPermissionDto"/> objects representing the permissions to be added.</param>
        /// <param name="currentUserInfo">The information about the current user performing the operation.</param>
        /// <returns></returns>
        Task AddItemPermissions(KinaUnaTypes.TimeLineType itemType, int itemId, int progenyId, int familyId, List<ItemPermissionDto> itemPermissionsDtoList, UserInfo currentUserInfo);

        /// <summary>
        /// Updates the permissions for a specific timeline item based on the provided parameters.
        /// </summary>
        /// <remarks>This method updates the permissions for a timeline item based on the provided list of
        /// desired permissions. It ensures that certain permission levels, such as <see
        /// cref="PermissionLevel.CreatorOnly"/> and <see cref="PermissionLevel.Private"/>, cannot be directly modified.
        /// If the item is set to inherit permissions, the method will handle the inheritance logic accordingly.  The
        /// method also validates the current user's permissions and updates them as necessary, taking into account any
        /// family or progeny-level permissions that may apply. If the permissions are changed from inheritance or
        /// restricted levels to custom permissions, the method ensures that all necessary permissions are updated or
        /// added.  This method is asynchronous and performs database operations to retrieve and update
        /// permissions.</remarks>
        /// <param name="itemType">The type of the timeline item to update.</param>
        /// <param name="itemId">The unique identifier of the timeline item.</param>
        /// <param name="progenyId">The unique identifier of the progeny associated with the timeline item.</param>
        /// <param name="familyId">The unique identifier of the family associated with the timeline item.</param>
        /// <param name="itemPermissionsDtoList">A list of <see cref="ItemPermissionDto"/> objects representing the desired permissions for the timeline
        /// item.</param>
        /// <param name="currentUserInfo">The user information of the current user making the update request.</param>
        /// <returns>A list of <see cref="TimelineItemPermission"/> objects representing the permissions that were changed as a
        /// result of the update. If no changes were made, the list will be empty.</returns>
        Task<List<TimelineItemPermission>> UpdateItemPermissions(KinaUnaTypes.TimeLineType itemType, int itemId, int progenyId, int familyId, List<ItemPermissionDto> itemPermissionsDtoList, UserInfo currentUserInfo);

        /// <summary>
        /// Retrieves a list of permissions for a specific timeline item that the current user has access to view.
        /// </summary>
        /// <param name="itemType">The type of the timeline item, represented as a <see cref="KinaUnaTypes.TimeLineType"/> enumeration.</param>
        /// <param name="itemId">The unique identifier of the timeline item.</param>
        /// <param name="currentUserInfo">The information of the current user requesting the permissions.</param>
        /// <returns>List of <see cref="TimelineItemPermission"/> objects representing the permissions for the specified timeline item that the current user has access to view.</returns>
        Task<List<TimelineItemPermission>> GetTimelineItemPermissionsList(KinaUnaTypes.TimeLineType itemType, int itemId, UserInfo currentUserInfo);

        /// <summary>
        /// Gets the highest permission level for a specific progeny and user by checking both direct user permissions and group permissions.
        /// </summary>
        /// <param name="progenyId">The unique identifier of the progeny.</param>
        /// <param name="currentUserInfo">The information of the user whose permissions are being checked.</param>
        /// <returns></returns>
        Task<ProgenyPermission> GetProgenyPermissionForUser(int progenyId, UserInfo currentUserInfo);

        /// <summary>
        /// Retrieves the highest permission level for a user within a specified family, considering both direct user
        /// permissions and group memberships.
        /// </summary>
        /// <remarks>This method first checks for direct permissions assigned to the user for the
        /// specified family. If no direct permissions are found, it evaluates the user's group memberships and their
        /// associated permissions. Group permissions with a <see cref="PermissionLevel"/> of <see
        /// cref="PermissionLevel.CreatorOnly"/> or higher are excluded from consideration.</remarks>
        /// <param name="familyId">The unique identifier of the family for which the permissions are being retrieved.</param>
        /// <param name="userInfo">An object containing information about the user whose permissions are being evaluated.</param>
        /// <returns>A <see cref="FamilyPermission"/> object representing the highest permission level the user has within the
        /// specified family. If the user has no permissions, the returned object will have a <see
        /// cref="PermissionLevel"/> of <see cref="PermissionLevel.None"/>.</returns>
        Task<FamilyPermission> GetFamilyPermissionForUser(int familyId, UserInfo userInfo);
    }
}
