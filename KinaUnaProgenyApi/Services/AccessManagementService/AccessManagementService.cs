using KinaUna.Data.Contexts;
using KinaUna.Data.Models;
using KinaUna.Data.Models.AccessManagement;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using KinaUna.Data.Extensions;
using KinaUna.Data.Models.Family;
using Microsoft.EntityFrameworkCore;

namespace KinaUnaProgenyApi.Services.AccessManagementService
{
    /// <summary>
    /// Service for managing access permissions for users and groups to various resources such as progeny, families, and timeline items.
    /// </summary>
    /// <param name="progenyDbContext"></param>
    public class AccessManagementService(ProgenyDbContext progenyDbContext) : IAccessManagementService
    {
        /// <summary>
        /// Determines whether a user has the specified access level for a given timeline item (e.g., Note, TodoItem, Sleep, etc.).
        /// </summary>
        /// <param name="itemType">KinaUnaTypes.TimeLineType of the item whose access to is being checked.</param>
        /// <param name="itemId">The unique identifier of the item whose access to is being checked.</param>
        /// <param name="userInfo">The information of the user whose access is being verified.</param>
        /// <param name="requiredLevel">The minimum permission level required for access.</param>
        /// <returns>Boolean value indicating whether the user has the required access level for the timeline item.</returns>
        public async Task<bool> HasItemPermission(KinaUnaTypes.TimeLineType itemType, int itemId, UserInfo userInfo, PermissionLevel requiredLevel)
        {
            if (userInfo == null || itemId == 0)
            {
                return false;
            }

            TimelineItemPermission timelineItemPermission = await progenyDbContext.TimelineItemPermissionsDb
                .AsNoTracking()
                .SingleOrDefaultAsync(tp => tp.UserId == userInfo.UserId && tp.TimelineType == itemType && tp.ItemId == itemId);
            if (timelineItemPermission != null && timelineItemPermission.PermissionLevel >= requiredLevel)
            {
                return true;
            }
            
            List<TimelineItemPermission> groupPermissions = await progenyDbContext.TimelineItemPermissionsDb
                .AsNoTracking()
                .Where(tp => tp.GroupId > 0 && tp.TimelineType == itemType && tp.ItemId == itemId)
                .ToListAsync();
            
            PermissionLevel highestGroupPermission = PermissionLevel.None;
            foreach (TimelineItemPermission permission in groupPermissions)
            {
                bool isMember = await progenyDbContext.UserGroupMembersDb.AnyAsync(ug => ug.UserId == userInfo.UserId && ug.UserGroupId == permission.GroupId);
                if (isMember && permission.PermissionLevel > highestGroupPermission)
                {
                    highestGroupPermission = permission.PermissionLevel;
                }
            }

            return highestGroupPermission >= requiredLevel;
        }

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
        public async Task<TimelineItemPermission> GrantItemPermission(TimelineItemPermission timelineItemPermission, UserInfo currentUserInfo)
        {
            // Check if the current user can grant the specified permission level.
            if (timelineItemPermission.ProgenyId > 0)
            {
                if (!await IsUserAccessManager(currentUserInfo.UserId, PermissionType.TimelineItem, timelineItemPermission.ProgenyId))
                {
                    return null; // Todo: Use result object instead.
                }
            }
            else
            {
                if (!await IsUserAccessManager(currentUserInfo.UserId, PermissionType.Family, timelineItemPermission.FamilyId))
                {
                    return null; // Todo: Use result object instead.
                }
            }

            // Check if the permission exists.
            TimelineItemPermission existingPermission = await progenyDbContext.TimelineItemPermissionsDb
                .SingleOrDefaultAsync(tp => 
                    tp.TimelineItemPermissionId == timelineItemPermission.TimelineItemPermissionId
                    && tp.ProgenyId == timelineItemPermission.ProgenyId 
                    && tp.FamilyId == timelineItemPermission.FamilyId);
            
            if (existingPermission == null)
            {
                existingPermission = await progenyDbContext.TimelineItemPermissionsDb.SingleOrDefaultAsync(
                    tp => tp.ProgenyId == timelineItemPermission.ProgenyId
                          && tp.FamilyId == timelineItemPermission.FamilyId
                          && tp.UserId == timelineItemPermission.UserId);
                if (existingPermission == null)
                {
                    existingPermission = await progenyDbContext.TimelineItemPermissionsDb.SingleOrDefaultAsync(tp =>
                        tp.ProgenyId == timelineItemPermission.ProgenyId && tp.FamilyId == timelineItemPermission.FamilyId && tp.Email == timelineItemPermission.Email);
                    if (existingPermission == null)
                    {
                        return null; // Todo: Use result object instead
                    }
                }
            }

            timelineItemPermission.CreatedBy = currentUserInfo.UserId;
            timelineItemPermission.CreatedTime = System.DateTime.UtcNow;
            timelineItemPermission.ModifiedBy = currentUserInfo.UserId;
            timelineItemPermission.ModifiedTime = System.DateTime.UtcNow;

            progenyDbContext.TimelineItemPermissionsDb.Add(timelineItemPermission);
            await progenyDbContext.SaveChangesAsync();

            return timelineItemPermission; // Todo: Use result object instead.
        }

        /// <summary>
        /// Revokes a specific permission for a timeline item from a user or group.
        /// </summary>
        /// <remarks>The method checks whether the current user has sufficient access rights to revoke the
        /// specified permission. If the permission does not exist or the user lacks the necessary access rights, the
        /// method returns <see langword="false"/>.</remarks>
        /// <param name="timelineItemPermission">The permission to be revoked, including details such as the user or group, permission type, and progenyId.</param>
        /// <param name="currentUserInfo">The information of the user attempting to revoke the permission.</param>
        /// <returns><see langword="true"/> if the permission was successfully revoked; otherwise, <see langword="false"/>.</returns>
        public async Task<bool> RevokeItemPermission(TimelineItemPermission timelineItemPermission, UserInfo currentUserInfo)
        {
            // Check if the current user can grant the specified permission level.
            if (timelineItemPermission.ProgenyId > 0)
            {
                if (!await IsUserAccessManager(currentUserInfo.UserId, PermissionType.TimelineItem, timelineItemPermission.ProgenyId))
                {
                    return false; // Todo: Use result object instead.
                }
            }
            else
            {
                if (!await IsUserAccessManager(currentUserInfo.UserId, PermissionType.Family, timelineItemPermission.FamilyId))
                {
                    return false; // Todo: Use result object instead.
                }
            }

            // Check if the permission exists.
            TimelineItemPermission existingPermission = await progenyDbContext.TimelineItemPermissionsDb
                .SingleOrDefaultAsync(tp => tp.TimelineItemPermissionId == timelineItemPermission.TimelineItemPermissionId
                                            && tp.ProgenyId == timelineItemPermission.ProgenyId && tp.FamilyId == timelineItemPermission.FamilyId);
            if (existingPermission == null)
            {
                existingPermission = await progenyDbContext.TimelineItemPermissionsDb.SingleOrDefaultAsync(tp => tp.ProgenyId == timelineItemPermission.ProgenyId
                                                                                                                 && tp.FamilyId == timelineItemPermission.FamilyId
                                                                                                                 && tp.UserId == timelineItemPermission.UserId);
                if (existingPermission == null)
                {
                    existingPermission = await progenyDbContext.TimelineItemPermissionsDb.SingleOrDefaultAsync(tp =>
                        tp.ProgenyId == timelineItemPermission.ProgenyId && tp.FamilyId == timelineItemPermission.FamilyId && tp.Email == timelineItemPermission.Email);
                    if (existingPermission == null)
                    {
                        return false; // Todo: Use result object instead
                    }
                }
            }
            
            progenyDbContext.TimelineItemPermissionsDb.Remove(existingPermission);
            await progenyDbContext.SaveChangesAsync();

            return true; // Todo: Use result object instead.
        }

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
        public async Task<TimelineItemPermission> UpdateItemPermission(TimelineItemPermission timelineItemPermission, UserInfo currentUserInfo)
        {
            // Check if the current user can grant the specified permission level.
            if (timelineItemPermission.ProgenyId > 0)
            {
                if (!await IsUserAccessManager(currentUserInfo.UserId, PermissionType.TimelineItem, timelineItemPermission.ProgenyId))
                {
                    return null; // Todo: Use result object instead.
                }
            }
            else
            {
                if (!await IsUserAccessManager(currentUserInfo.UserId, PermissionType.Family, timelineItemPermission.FamilyId))
                {
                    return null; // Todo: Use result object instead.
                }
            }

            // Check if the permission exists.
            TimelineItemPermission existingPermission = await progenyDbContext.TimelineItemPermissionsDb
                .SingleOrDefaultAsync(tp => tp.TimelineItemPermissionId == timelineItemPermission.TimelineItemPermissionId
                                            && tp.ProgenyId == timelineItemPermission.ProgenyId && tp.FamilyId == timelineItemPermission.FamilyId);
            if (existingPermission == null)
            {
                existingPermission = await progenyDbContext.TimelineItemPermissionsDb.SingleOrDefaultAsync(
                    tp => tp.ProgenyId == timelineItemPermission.ProgenyId 
                          && tp.FamilyId == timelineItemPermission.FamilyId 
                          && tp.UserId == timelineItemPermission.UserId);
                if (existingPermission == null)
                {
                    existingPermission = await progenyDbContext.TimelineItemPermissionsDb.SingleOrDefaultAsync(
                        tp => tp.ProgenyId == timelineItemPermission.ProgenyId && tp.FamilyId == timelineItemPermission.FamilyId && tp.Email == timelineItemPermission.Email);
                    if (existingPermission == null)
                    {
                        return null; // Todo: Use result object instead
                    }
                }
            }
            
            existingPermission.PermissionLevel = timelineItemPermission.PermissionLevel;
            existingPermission.ModifiedTime = System.DateTime.UtcNow;
            existingPermission.ModifiedBy = currentUserInfo.UserId;

            progenyDbContext.TimelineItemPermissionsDb.Update(existingPermission);
            await progenyDbContext.SaveChangesAsync();

            return existingPermission; // Todo: Use result object instead.
        }

        /// <summary>
        /// Determines whether a user has the specified access level for a given progeny.
        /// </summary>
        /// <param name="progenyId">The unique identifier of the progeny whose access to is being checked.</param>
        /// <param name="userInfo">The information of the user whose access is being verified.</param>
        /// <param name="requiredLevel">The minimum permission level required for access.</param>
        /// <returns>Boolean value indicating whether the user has the required access level for the progeny.</returns>
        public async Task<bool> HasProgenyPermission(int progenyId, UserInfo userInfo, PermissionLevel requiredLevel)
        {
            if (userInfo == null || progenyId == 0)
            {
                return false;
            }

            ProgenyPermission progenyPermission = await progenyDbContext.ProgenyPermissionsDb
                .AsNoTracking()
                .SingleOrDefaultAsync(pp => pp.UserId == userInfo.UserId && pp.ProgenyId == progenyId);
            if (progenyPermission != null && progenyPermission.PermissionLevel >= requiredLevel)
            {
                return true;
            }
            
            List<ProgenyPermission> groupPermissions = await progenyDbContext.ProgenyPermissionsDb
                .AsNoTracking()
                .Where(pp => pp.GroupId > 0 && pp.ProgenyId == progenyId)
                .ToListAsync();
            PermissionLevel highestGroupPermission = PermissionLevel.None;
            foreach (ProgenyPermission permission in groupPermissions)
            {
                bool isMember = await progenyDbContext.UserGroupMembersDb.AnyAsync(ug => ug.UserId == userInfo.UserId && ug.UserGroupId == permission.GroupId);
                if (isMember && permission.PermissionLevel > highestGroupPermission)
                {
                    highestGroupPermission = permission.PermissionLevel;
                }
            }

            return highestGroupPermission >= requiredLevel;
        }

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
        public async Task<ProgenyPermission> GrantProgenyPermission(ProgenyPermission progenyPermission, UserInfo currentUserInfo)
        {
            // Check if the current user can grant the specified permission level.
            if (!await IsUserAccessManager(currentUserInfo.UserId, PermissionType.Progeny, progenyPermission.ProgenyId))
            {
                return null; // Todo: Use result object instead.
            }

            // Check if the permission already exists.
            ProgenyPermission existingPermission = await progenyDbContext.ProgenyPermissionsDb
                .SingleOrDefaultAsync(pp => pp.UserId == progenyPermission.UserId 
                                            && pp.GroupId == progenyPermission.GroupId
                                            && pp.ProgenyId == progenyPermission.ProgenyId);
            if (existingPermission != null)
            {
                return null; // Todo: Use result object instead.
            }

            progenyPermission.CreatedBy = currentUserInfo.UserId;
            progenyPermission.CreatedTime = System.DateTime.UtcNow;
            progenyPermission.ModifiedBy = currentUserInfo.UserId;
            progenyPermission.ModifiedTime = System.DateTime.UtcNow;

            progenyDbContext.ProgenyPermissionsDb.Add(progenyPermission);
            await progenyDbContext.SaveChangesAsync();

            return progenyPermission; // Todo: Use result object instead.
        }

        /// <summary>
        /// Revokes a specific permission for a progeny from a user or group.
        /// </summary>
        /// <remarks>The method checks whether the current user has sufficient access rights to revoke the
        /// specified permission. If the permission does not exist or the user lacks the necessary access rights, the
        /// method returns <see langword="false"/>.</remarks>
        /// <param name="progenyPermission">The permission to be revoked, including details such as the user or group, permission type, and progenyId.</param>
        /// <param name="currentUserInfo">The information of the user attempting to revoke the permission.</param>
        /// <returns><see langword="true"/> if the permission was successfully revoked; otherwise, <see langword="false"/>.</returns>
        public async Task<bool> RevokeProgenyPermission(ProgenyPermission progenyPermission, UserInfo currentUserInfo)
        {
            // Check if the current user can grant the specified permission level.
            if (!await IsUserAccessManager(currentUserInfo.UserId, PermissionType.Progeny, progenyPermission.ProgenyId))
            {
                return false; // Todo: Use result object instead.
            }

            // Don't allow removing own admin rights. You need to assign admin rights to another user first, then they may remove your access.
            if (progenyPermission.UserId == currentUserInfo.UserId)
            {
                return false; // Todo: Use result object instead.
            }

            // Check if the permission exists.
            ProgenyPermission existingPermission = await progenyDbContext.ProgenyPermissionsDb
                .SingleOrDefaultAsync(pp => pp.ProgenyPermissionId == progenyPermission.ProgenyPermissionId && pp.ProgenyId == progenyPermission.ProgenyId);
            if (existingPermission == null)
            {
                existingPermission = await progenyDbContext.ProgenyPermissionsDb.SingleOrDefaultAsync(pp => pp.ProgenyId == progenyPermission.ProgenyId && pp.UserId == progenyPermission.UserId);
                if (existingPermission == null)
                {
                    existingPermission = await progenyDbContext.ProgenyPermissionsDb.SingleOrDefaultAsync(pp => pp.ProgenyId == progenyPermission.ProgenyId && pp.Email == progenyPermission.Email);
                    if (existingPermission == null)
                    {
                        return false; // Todo: Use result object instead
                    }
                }
            }

            // If the existing permission is admin, remove from Family Admins list.
            if (existingPermission.PermissionLevel == PermissionLevel.Admin)
            {
                Progeny progeny = await progenyDbContext.ProgenyDb.SingleOrDefaultAsync(p => p.Id == progenyPermission.ProgenyId);
                if (progeny != null)
                {
                    if (progeny.IsInAdminList(progenyPermission.Email))
                    {
                        progeny.RemoveFromAdminList(progenyPermission.Email);
                        progenyDbContext.ProgenyDb.Update(progeny);
                    }
                }
            }

            // If the existing permission is not admin and the new permission is, add to the Family Admins list.
            if (progenyPermission.PermissionLevel == PermissionLevel.Admin && existingPermission.PermissionLevel != PermissionLevel.Admin)
            {
                Progeny progeny = await progenyDbContext.ProgenyDb.SingleOrDefaultAsync(p => p.Id == progenyPermission.ProgenyId);
                if (progeny != null)
                {
                    if (!progeny.IsInAdminList(progenyPermission.Email))
                    {
                        progeny.AddToAdminList(progenyPermission.Email);
                        progenyDbContext.ProgenyDb.Update(progeny);
                    }
                }
            }

            progenyDbContext.ProgenyPermissionsDb.Remove(existingPermission);
            await progenyDbContext.SaveChangesAsync();

            return true; // Todo: Use result object instead.
        }

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
        public async Task<ProgenyPermission> UpdateProgenyPermission(ProgenyPermission progenyPermission, UserInfo currentUserInfo)
        {
            // Check if the current user can grant the specified permission level.
            if (!await IsUserAccessManager(currentUserInfo.UserId, PermissionType.Progeny, progenyPermission.ProgenyId))
            {
                return null; // Todo: Use result object instead.
            }

            // Check if the permission exists.
            ProgenyPermission existingPermission = await progenyDbContext.ProgenyPermissionsDb
                .SingleOrDefaultAsync(pp => pp.ProgenyPermissionId == progenyPermission.ProgenyPermissionId
                                            && pp.ProgenyId == progenyPermission.ProgenyId);
            if (existingPermission == null)
            {
                existingPermission = await progenyDbContext.ProgenyPermissionsDb.SingleOrDefaultAsync(pp => pp.ProgenyId == progenyPermission.ProgenyId && pp.UserId == progenyPermission.UserId);
                if (existingPermission == null)
                {
                    existingPermission = await progenyDbContext.ProgenyPermissionsDb.SingleOrDefaultAsync(pp => pp.ProgenyId == progenyPermission.ProgenyId && pp.Email == progenyPermission.Email);
                    if (existingPermission == null)
                    {
                        return null; // Todo: Use result object instead
                    }
                }
            }

            // If the existing permission is admin and the new permission isn't, remove from Progeny Admins list.
            if (progenyPermission.PermissionLevel == PermissionLevel.Admin && existingPermission.PermissionLevel != PermissionLevel.Admin)
            {
                Progeny progeny = await progenyDbContext.ProgenyDb.SingleOrDefaultAsync(p => p.Id == progenyPermission.ProgenyId);
                if (progeny != null)
                {
                    if (progeny.IsInAdminList(progenyPermission.Email))
                    {
                        progeny.RemoveFromAdminList(progenyPermission.Email);
                        progenyDbContext.ProgenyDb.Update(progeny);
                    }
                }
            }

            // If the existing permission is not admin and the new permission is, add to the Progeny Admins list.
            if (progenyPermission.PermissionLevel == PermissionLevel.Admin && existingPermission.PermissionLevel != PermissionLevel.Admin)
            {
                Progeny progeny = await progenyDbContext.ProgenyDb.SingleOrDefaultAsync(f => f.Id == progenyPermission.ProgenyId);
                if (progeny != null)
                {
                    if (!progeny.IsInAdminList(progenyPermission.Email))
                    {
                        progeny.AddToAdminList(progenyPermission.Email);
                        progenyDbContext.ProgenyDb.Update(progeny);
                    }
                }
            }

            existingPermission.PermissionLevel = progenyPermission.PermissionLevel;
            existingPermission.ModifiedTime = System.DateTime.UtcNow;
            existingPermission.ModifiedBy = currentUserInfo.UserId;

            progenyDbContext.ProgenyPermissionsDb.Update(existingPermission);
            await progenyDbContext.SaveChangesAsync();

            return existingPermission; // Todo: Use result object instead.
        }

        /// <summary>
        /// Determines whether a user has the specified access level for a given family.
        /// </summary>
        /// <param name="familyId">The unique identifier of the family whose access to is being checked.</param>
        /// <param name="userInfo">The information of the user whose access is being verified.</param>
        /// <param name="requiredLevel">The minimum permission level required for access.</param>
        /// <returns>Boolean value indicating whether the user has the required access level for the family.</returns>
        public async Task<bool> HasFamilyPermission(int familyId, UserInfo userInfo, PermissionLevel requiredLevel)
        {
            if (userInfo == null || familyId == 0)
            {
                return false;
            }

            FamilyPermission familyPermission = await progenyDbContext.FamilyPermissionsDb
                .AsNoTracking()
                .SingleOrDefaultAsync(fp => fp.UserId == userInfo.UserId && fp.FamilyId == familyId);
            if (familyPermission != null && familyPermission.PermissionLevel >= requiredLevel)
            {
                return true;
            }

            List<FamilyPermission> groupPermissions = await progenyDbContext.FamilyPermissionsDb
                .AsNoTracking()
                .Where(fp => fp.GroupId > 0 && fp.FamilyId== familyId)
                .ToListAsync();
            PermissionLevel highestGroupPermission = PermissionLevel.None;
            foreach (FamilyPermission permission in groupPermissions)
            {
                bool isMember = await progenyDbContext.UserGroupMembersDb.AnyAsync(ug => ug.UserId == userInfo.UserId && ug.UserGroupId == permission.GroupId);
                if (isMember && permission.PermissionLevel > highestGroupPermission)
                {
                    highestGroupPermission = permission.PermissionLevel;
                }
            }

            return highestGroupPermission >= requiredLevel;
        }
        
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
        public async Task<FamilyPermission> GrantFamilyPermission(FamilyPermission familyPermission, UserInfo currentUserInfo)
        {
            // Check if the current user can grant the specified permission level.
            if (!await IsUserAccessManager(currentUserInfo.UserId, PermissionType.Family, familyPermission.FamilyId))
            {
                return null; // Todo: Use result object instead.
            }

            // Check if the permission already exists.
            FamilyPermission existingPermission = await progenyDbContext.FamilyPermissionsDb
                .SingleOrDefaultAsync(fp => fp.UserId == familyPermission.UserId && fp.GroupId == familyPermission.GroupId
                                           && fp.FamilyId == familyPermission.FamilyId);
            if (existingPermission != null)
            {
                return null; // Todo: Use result object instead.
            }

            familyPermission.CreatedBy = currentUserInfo.UserId;
            familyPermission.CreatedTime = System.DateTime.UtcNow;
            familyPermission.ModifiedBy = currentUserInfo.UserId;
            familyPermission.ModifiedTime = System.DateTime.UtcNow;
            
            progenyDbContext.FamilyPermissionsDb.Add(familyPermission);
            await progenyDbContext.SaveChangesAsync();

            return familyPermission; // Todo: Use result object instead.
        }

        /// <summary>
        /// Revokes a specific permission for a family from a user or group.
        /// </summary>
        /// <remarks>The method checks whether the current user has sufficient access rights to revoke the
        /// specified permission. If the permission does not exist or the user lacks the necessary access rights, the
        /// method returns <see langword="false"/>.</remarks>
        /// <param name="familyPermission">The permission to be revoked, including details such as the user or group, permission type, and familyId.</param>
        /// <param name="currentUserInfo">The information of the user attempting to revoke the permission.</param>
        /// <returns><see langword="true"/> if the permission was successfully revoked; otherwise, <see langword="false"/>.</returns>
        public async Task<bool> RevokeFamilyPermission(FamilyPermission familyPermission, UserInfo currentUserInfo)
        {
            // Check if the current user can grant the specified permission level.
            if (!await IsUserAccessManager(currentUserInfo.UserId, PermissionType.Family, familyPermission.FamilyId))
            {
                return false; // Todo: Use result object instead.
            }

            // Don't allow removing own admin rights. You need to assign admin rights to another user first, then they may remove your access.
            if (familyPermission.UserId == currentUserInfo.UserId)
            {
                return false; // Todo: Use result object instead.
            }

            // Check if the permission exists.
            FamilyPermission existingPermission = await progenyDbContext.FamilyPermissionsDb
                .SingleOrDefaultAsync(fp => fp.FamilyPermissionId == familyPermission.FamilyPermissionId && fp.FamilyId == familyPermission.FamilyId);
            if (existingPermission == null)
            {
                existingPermission = await progenyDbContext.FamilyPermissionsDb.SingleOrDefaultAsync(fp => fp.FamilyId == familyPermission.FamilyId && fp.UserId == familyPermission.UserId);
                if (existingPermission == null) {
                    existingPermission = await progenyDbContext.FamilyPermissionsDb.SingleOrDefaultAsync(fp => fp.FamilyId == familyPermission.FamilyId && fp.Email == familyPermission.Email);
                    if (existingPermission == null)
                    {
                        return false; // Todo: Use result object instead
                    }
                }
            }

            // If the existing permission is admin, remove from Family Admins list.
            if (existingPermission.PermissionLevel == PermissionLevel.Admin)
            {
                Family family = await progenyDbContext.FamiliesDb.SingleOrDefaultAsync(f => f.FamilyId == familyPermission.FamilyId);
                if (family != null)
                {
                    if (family.IsInAdminList(familyPermission.Email))
                    {
                        family.RemoveFromAdminList(familyPermission.Email);
                        progenyDbContext.FamiliesDb.Update(family);
                    }
                }
            }

            // If the existing permission is not admin and the new permission is, add to the Family Admins list.
            if (familyPermission.PermissionLevel == PermissionLevel.Admin && existingPermission.PermissionLevel != PermissionLevel.Admin)
            {
                Family family = await progenyDbContext.FamiliesDb.SingleOrDefaultAsync(f => f.FamilyId == familyPermission.FamilyId);
                if (family != null)
                {
                    if (!family.IsInAdminList(familyPermission.Email))
                    {
                        family.AddToAdminList(familyPermission.Email);
                        progenyDbContext.FamiliesDb.Update(family);
                    }
                }
            }

            progenyDbContext.FamilyPermissionsDb.Remove(existingPermission);
            await progenyDbContext.SaveChangesAsync();
            
            return true; // Todo: Use result object instead.
        }

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
        public async Task<FamilyPermission> UpdateFamilyPermission(FamilyPermission familyPermission, UserInfo currentUserInfo)
        {
            // Check if the current user can grant the specified permission level.
            if (!await IsUserAccessManager(currentUserInfo.UserId, PermissionType.Family, familyPermission.FamilyId))
            {
                return null; // Todo: Use result object instead.
            }

            // Check if the permission exists.
            FamilyPermission existingPermission = await progenyDbContext.FamilyPermissionsDb
                .SingleOrDefaultAsync(fp => fp.FamilyPermissionId == familyPermission.FamilyPermissionId && fp.FamilyId == familyPermission.FamilyId);
            if (existingPermission == null)
            {
                existingPermission = await progenyDbContext.FamilyPermissionsDb.SingleOrDefaultAsync(fp => fp.FamilyId == familyPermission.FamilyId && fp.UserId == familyPermission.UserId);
                if (existingPermission == null)
                {
                    existingPermission = await progenyDbContext.FamilyPermissionsDb.SingleOrDefaultAsync(fp => fp.FamilyId == familyPermission.FamilyId && fp.Email == familyPermission.Email);
                    if (existingPermission == null)
                    {
                        return null; // Todo: Use result object instead
                    }
                }
            }

            // If the existing permission is admin and the new permission isn't, remove from Family Admins list.
            if (familyPermission.PermissionLevel == PermissionLevel.Admin && existingPermission.PermissionLevel != PermissionLevel.Admin)
            {
                Family family = await progenyDbContext.FamiliesDb.SingleOrDefaultAsync(f => f.FamilyId == familyPermission.FamilyId);
                if (family != null)
                {
                    if (family.IsInAdminList(familyPermission.Email))
                    {
                        family.RemoveFromAdminList(familyPermission.Email);
                        progenyDbContext.FamiliesDb.Update(family);
                    }
                }
            }

            // If the existing permission is not admin and the new permission is, add to the Family Admins list.
            if (familyPermission.PermissionLevel == PermissionLevel.Admin && existingPermission.PermissionLevel != PermissionLevel.Admin)
            {
                Family family = await progenyDbContext.FamiliesDb.SingleOrDefaultAsync(f => f.FamilyId == familyPermission.FamilyId);
                if (family != null)
                {
                    if (!family.IsInAdminList(familyPermission.Email))
                    {
                        family.AddToAdminList(familyPermission.Email);
                        progenyDbContext.FamiliesDb.Update(family);
                    }
                }
            }

            existingPermission.PermissionLevel = familyPermission.PermissionLevel;
            existingPermission.ModifiedTime = System.DateTime.UtcNow;
            existingPermission.ModifiedBy = currentUserInfo.UserId;
            
            progenyDbContext.FamilyPermissionsDb.Update(existingPermission);
            await progenyDbContext.SaveChangesAsync();
            
            return existingPermission; // Todo: Use result object instead.
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
                ProgenyPermission progenyPermission = await progenyDbContext.ProgenyPermissionsDb.AsNoTracking()
                    .SingleOrDefaultAsync(pp => pp.UserId == userId 
                                               && pp.ProgenyId == entityId 
                                               && pp.PermissionLevel == PermissionLevel.Admin);
                
                if (progenyPermission != null)
                {
                    return true;
                }
            }

            if (permissionType == PermissionType.Progeny)
            {
                ProgenyPermission progenyPermission = await progenyDbContext.ProgenyPermissionsDb.AsNoTracking()
                    .SingleOrDefaultAsync(pp => pp.UserId == userId
                                               && pp.ProgenyId == entityId
                                               && pp.PermissionLevel == PermissionLevel.Admin);

                if (progenyPermission != null)
                {
                    return true;
                }
            }

            if (permissionType == PermissionType.Family)
            {
                FamilyPermission familyPermission = await progenyDbContext.FamilyPermissionsDb.AsNoTracking()
                    .SingleOrDefaultAsync(fp => fp.UserId == userId 
                                               && fp.FamilyId == entityId 
                                               && fp.PermissionLevel == PermissionLevel.Admin);
                if (familyPermission != null)
                {
                    return true;
                }
            }
            
            return false;
        }
    }
}
