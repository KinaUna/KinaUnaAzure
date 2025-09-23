using KinaUna.Data.Contexts;
using KinaUna.Data.Models;
using KinaUna.Data.Models.AccessManagement;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace KinaUnaProgenyApi.Services.AccessManagementService
{
    public class AccessManagementService(ProgenyDbContext progenyDbContext) : IAccessManagementService
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
        public async Task<bool> HasPermissionAsync(string userId, PermissionType permissionType, PermissionLevel requiredLevel, int resourceId, KinaUnaTypes.TimeLineType? timelineType)
        {
            // Get user's direct permissions
            PermissionLevel? userPermission = await GetDirectUserPermissionAsync(userId, permissionType, resourceId, timelineType);
            if (userPermission.HasValue && userPermission.Value >= requiredLevel)
            {
                return true;
            }

            // Get user's group permissions
            List<PermissionLevel> groupPermissions = await GetGroupPermissionsAsync(userId, permissionType, resourceId, timelineType);
            PermissionLevel highestGroupPermission = groupPermissions.Count != 0 ? groupPermissions.Max() : PermissionLevel.None;

            return highestGroupPermission >= requiredLevel;
        }

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
        public async Task<ResourcePermission> GrantPermission(int entityId, ResourcePermission resourcePermission, UserInfo currentUserInfo)
        {
            // Check if the current user can grant the specified permission level.
            if (!await IsUserAccessManager(currentUserInfo.UserId, resourcePermission.PermissionType, entityId))
            {
                return null; // Todo: Use result object instead.
            }

            // Check if the permission already exists.
            ResourcePermission existingPermission = await progenyDbContext.ResourcePermissionsDb
                .SingleOrDefaultAsync(r => r.UserId == resourcePermission.UserId && r.GroupId == resourcePermission.GroupId
                                           && r.PermissionType == resourcePermission.PermissionType 
                                           && r.ResourceId == resourcePermission.ResourceId 
                                           && r.TimelineType == resourcePermission.TimelineType);
            if (existingPermission != null)
            {
                return null; // Todo: Use result object instead.
            }

            resourcePermission.CreatedTime = System.DateTime.UtcNow;
            resourcePermission.ModifiedTime = System.DateTime.UtcNow;

            progenyDbContext.ResourcePermissionsDb.Add(resourcePermission);
            await progenyDbContext.SaveChangesAsync();

            return resourcePermission; // Todo: Use result object instead.
        }

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
        public async Task<bool> RevokePermission(int entityId, ResourcePermission resourcePermission, UserInfo currentUserInfo)
        {
            // Check if the current user can grant the specified permission level.
            if (!await IsUserAccessManager(currentUserInfo.UserId, resourcePermission.PermissionType, entityId))
            {
                return false; // Todo: Use result object instead.
            }
            
            // Check if the permission exists.
            ResourcePermission existingPermission = await progenyDbContext.ResourcePermissionsDb
                .SingleOrDefaultAsync(r => r.UserId == resourcePermission.UserId && r.GroupId == resourcePermission.GroupId
                                           && r.PermissionType == resourcePermission.PermissionType 
                                           && r.ResourceId == resourcePermission.ResourceId 
                                           && r.TimelineType == resourcePermission.TimelineType);
            if (existingPermission == null)
            {
                return false; // Todo: Use result object instead.
            }
            progenyDbContext.ResourcePermissionsDb.Remove(existingPermission);
            await progenyDbContext.SaveChangesAsync();
            return true; // Todo: Use result object instead.
        }

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
        public async Task<ResourcePermission> UpdatePermission(int entityId, ResourcePermission resourcePermission, UserInfo currentUserInfo)
        {
            // Check if the current user can grant the specified permission level.
            if (!await IsUserAccessManager(currentUserInfo.UserId, resourcePermission.PermissionType, entityId))
            {
                return null; // Todo: Use result object instead.
            }

            // Check if the permission exists.
            ResourcePermission existingPermission = await progenyDbContext.ResourcePermissionsDb
                .SingleOrDefaultAsync(r => r.UserId == resourcePermission.UserId && r.GroupId == resourcePermission.GroupId
                                           && r.PermissionType == resourcePermission.PermissionType 
                                           && r.ResourceId == resourcePermission.ResourceId 
                                           && r.TimelineType == resourcePermission.TimelineType);
            if (existingPermission == null)
            {
                return null; // Todo: Use result object instead.
            }

            existingPermission.PermissionLevel = resourcePermission.PermissionLevel;
            existingPermission.ModifiedTime = System.DateTime.UtcNow;
            existingPermission.ModifiedBy = currentUserInfo.UserId;
            progenyDbContext.ResourcePermissionsDb.Update(existingPermission);
            await progenyDbContext.SaveChangesAsync();
            return existingPermission; // Todo: Use result object instead.
        }

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
        public async Task<List<ResourcePermission>> GetPermissionsForResource(PermissionType permissionType, int resourceId, KinaUnaTypes.TimeLineType? timelineType)
        {
            List<ResourcePermission> permissions = await progenyDbContext.ResourcePermissionsDb.AsNoTracking()
                .Where(r => r.PermissionType == permissionType && r.ResourceId == resourceId && r.TimelineType == timelineType)
                .ToListAsync();
            return permissions;
        }

        /// <summary>
        /// Retrieves a list of resource permissions associated with the specified user.
        /// </summary>
        /// <remarks>This method queries the database to retrieve permissions associated with the user.
        /// Permissions can be  assigned directly to the user or inherited through their membership in user groups.</remarks>
        /// <param name="userId">The unique identifier of the user whose permissions are to be retrieved. Cannot be null or empty.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains a list of  <see
        /// cref="ResourcePermission"/> objects representing the permissions assigned directly to the user  or through
        /// their membership in user groups. Returns an empty list if no permissions are found.</returns>
        public async Task<List<ResourcePermission>> GetPermissionsForUser(string userId)
        {
            List<ResourcePermission> permissions = await progenyDbContext.ResourcePermissionsDb.AsNoTracking()
                .Where(r => r.UserId == userId || progenyDbContext.UserGroupMembersDb.Any(ug => ug.UserId == userId && ug.UserGroupId == r.GroupId))
                .ToListAsync();
            return permissions;
        }

        /// <summary>
        /// Retrieves the list of resource permissions associated with the specified group.
        /// </summary>
        /// <remarks>This method queries the database for resource permissions linked to the specified
        /// group. The results are retrieved without tracking changes to the entities.</remarks>
        /// <param name="groupId">The unique identifier of the group whose permissions are to be retrieved.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains a list of <see
        /// cref="ResourcePermission"/> objects associated with the specified group. If no permissions are found, an
        /// empty list is returned.</returns>
        public async Task<List<ResourcePermission>> GetPermissionsForGroup(int groupId)
        {
            List<ResourcePermission> permissions = await progenyDbContext.ResourcePermissionsDb.AsNoTracking()
                .Where(r => r.GroupId == groupId)
                .ToListAsync();
            return permissions;
        }

        /// <summary>
        /// Asynchronously retrieves the direct permission level for a specified user on a given resource.
        /// </summary>
        /// <remarks>This method queries the database for a direct permission entry that matches the
        /// specified user, permission type,  resource, and timeline type. If no matching entry is found, the method
        /// returns <see langword="null"/>.</remarks>
        /// <param name="userId">The unique identifier of the user whose permission level is being retrieved. Cannot be <see
        /// langword="null"/> or empty.</param>
        /// <param name="permissionType">The type of permission to check for the user on the resource (e.g. TimelineItem, FamilyMember, Family).</param>
        /// <param name="resourceId">The unique identifier of the resource for which the permission level is being checked.</param>
        /// <param name="timelineType">The timeline type associated with the resource, or <see langword="null"/> if no specific timeline type is
        /// applicable.</param>
        /// <returns>A <see cref="PermissionLevel"/> representing the user's direct permission level for the specified resource, 
        /// or <see langword="null"/> if no direct permission is found.</returns>
        private async Task<PermissionLevel?> GetDirectUserPermissionAsync(string userId, PermissionType permissionType, int resourceId, KinaUnaTypes.TimeLineType? timelineType)
        {
            ResourcePermission permission = await progenyDbContext.ResourcePermissionsDb.AsNoTracking()
                .SingleOrDefaultAsync(r => r.UserId == userId && r.PermissionType == permissionType && r.ResourceId == resourceId && r.TimelineType == timelineType);
            return permission?.PermissionLevel;
        }

        /// <summary>
        /// Retrieves the permission levels for a user based on their group memberships and the specified resource and
        /// permission criteria.
        /// </summary>
        /// <remarks>This method checks the user's membership in groups that have permissions for the
        /// specified resource and filters the results based on the provided criteria.</remarks>
        /// <param name="userId">The unique identifier of the user whose group permissions are being retrieved. Cannot be null or empty.</param>
        /// <param name="permissionType">The type of permission to filter by (e.g. TimelineItem, FamilyMember, Family).</param>
        /// <param name="resourceId">The unique identifier of the resource for which permissions are being checked.</param>
        /// <param name="timelineType">The timeline type associated with the resource, or <see langword="null"/> if no specific timeline type is
        /// applicable.</param>
        /// <returns>A list of <see cref="PermissionLevel"/> values representing the permission levels granted to the user
        /// through their group memberships. The list will be empty if no matching permissions are found.</returns>
        private async Task<List<PermissionLevel>> GetGroupPermissionsAsync(string userId, PermissionType permissionType, int resourceId, KinaUnaTypes.TimeLineType? timelineType)
        {
            List<ResourcePermission> permissions = await progenyDbContext.ResourcePermissionsDb.AsNoTracking()
                .Where(r => r.GroupId > 0 && r.PermissionType == permissionType && r.ResourceId == resourceId && r.TimelineType == timelineType)
                .ToListAsync();
            List<PermissionLevel> groupPermissions = new List<PermissionLevel>();
            foreach (var permission in permissions)
            {
                bool isMember = await progenyDbContext.UserGroupMembersDb.AnyAsync(ug => ug.UserId == userId && ug.UserGroupId == permission.GroupId);
                if (isMember)
                {
                    groupPermissions.Add(permission.PermissionLevel);
                }
            }

            return groupPermissions;
        }

        /// <summary>
        /// Determines whether the specified user has administrative access to a resource based on the given permission
        /// type, entity ID, and optional timeline type.
        /// </summary>
        /// <remarks>This method evaluates the user's access by querying the resource permissions database
        /// for matching entries. The user must have an administrative permission level for the specified resource and
        /// permission type to be granted access.</remarks>
        /// <param name="userId">The unique identifier of the user whose access is being checked. Cannot be <see langword="null"/> or empty.</param>
        /// <param name="permissionType">The type of permission to evaluate. This determines the scope of the access check (e.g., timeline item,
        /// family member, or family).</param>
        /// <param name="entityId">The unique identifier of the resource entity for which admin access is being checked. (e.g., ProgenyId, FamilyMemberId, or FamilyId)</param>
        /// <returns><see langword="true"/> if the user has administrative access to the specified resource based on the provided
        /// parameters; otherwise, <see langword="false"/>.</returns>
        private async Task<bool> IsUserAccessManager(string userId, PermissionType permissionType, int entityId)
        {
            if (permissionType == PermissionType.TimelineItem)
            {
                // For timeline items, we need to check if the user has admin rights for the Progeny associated with the timeline item.
                ResourcePermission progenyPermission = await progenyDbContext.ResourcePermissionsDb.AsNoTracking()
                    .SingleOrDefaultAsync(r => r.UserId == userId 
                                               && r.PermissionType == PermissionType.FamilyMember 
                                               && r.ResourceId == entityId 
                                               && r.PermissionLevel == PermissionLevel.Admin);
                if (progenyPermission != null)
                {
                    return true;
                }
            }

            if (permissionType == PermissionType.FamilyMember)
            {
                ResourcePermission familyPermission = await progenyDbContext.ResourcePermissionsDb.AsNoTracking()
                    .SingleOrDefaultAsync(r => r.UserId == userId 
                                               && r.PermissionType == PermissionType.FamilyMember 
                                               && r.ResourceId == entityId 
                                               && r.PermissionLevel == PermissionLevel.Admin);
                if (familyPermission != null)
                {
                    return true;
                }
            }

            if (permissionType == PermissionType.Family)
            {
                ResourcePermission familyPermission = await progenyDbContext.ResourcePermissionsDb.AsNoTracking()
                    .SingleOrDefaultAsync(r => r.UserId == userId 
                                               && r.PermissionType == PermissionType.Family 
                                               && r.ResourceId == entityId 
                                               && r.PermissionLevel == PermissionLevel.Admin);
                if (familyPermission != null)
                {
                    return true;
                }
            }
            
            return false;
        }
    }
}
