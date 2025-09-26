using KinaUna.Data.Models;
using KinaUna.Data.Models.AccessManagement;
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
    }
}
