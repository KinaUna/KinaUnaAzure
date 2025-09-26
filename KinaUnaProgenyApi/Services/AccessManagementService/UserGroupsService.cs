using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using KinaUna.Data.Contexts;
using KinaUna.Data.Models;
using KinaUna.Data.Models.AccessManagement;
using Microsoft.EntityFrameworkCore;

namespace KinaUnaProgenyApi.Services.AccessManagementService
{
    /// <summary>
    /// User group service. Handles user groups and their members.
    /// </summary>
    /// <param name="progenyDbContext"></param>
    /// <param name="accessManagementService"></param>
    public class UserGroupsService(ProgenyDbContext progenyDbContext, IAccessManagementService accessManagementService, IUserGroupAuditLogsService userGroupAuditLogService): IUserGroupsService
    {
        /// <summary>
        /// Gets a user group by its unique identifier, including its members, if the current user has the necessary permissions.
        /// </summary>
        /// <param name="groupId">The unique identifier of the user group to retrieve.</param>
        /// <param name="currentUserInfo">The information about the current user, used to verify access permissions.</param>
        /// <returns>The <see cref="UserGroup"/> object representing the requested user group, including its members, if the user has access.</returns>
        public async Task<UserGroup> GetUserGroup(int groupId, UserInfo currentUserInfo)
        {
            UserGroup group = await progenyDbContext.UserGroupsDb.AsNoTracking().FirstOrDefaultAsync(ug => ug.UserGroupId == groupId);
            if (group == null) {
                return new UserGroup();
            }

            bool hasAccess = false;
            // Check if the user has access to the group. User must have Edit permission for the family to be able to see the group and its members.
            if (group.FamilyId != 0)
            {
                hasAccess = await accessManagementService.HasFamilyPermission(group.FamilyId, currentUserInfo, PermissionLevel.Edit);
                
            }
            else
            {
                if (group.ProgenyId != 0)
                {
                    if (await accessManagementService.HasProgenyPermission(group.ProgenyId, currentUserInfo, PermissionLevel.Edit))
                    {
                        hasAccess = true;
                    }
                }
            }
            
            if (!hasAccess)
            {
                return new UserGroup();
            }

            group.Members = await GetUserGroupMembersList(group.UserGroupId);

            return group;
        }

        /// <summary>
        /// Retrieves a list of user groups associated with the specified progeny that the current user has
        /// administrative access to.
        /// </summary>
        /// <remarks>Each returned <see cref="UserGroup"/> includes its associated members. Only groups
        /// for which the current user has  administrative permissions are included in the result.</remarks>
        /// <param name="progenyId">The unique identifier of the progeny whose user groups are to be retrieved.</param>
        /// <param name="currentUserInfo">The information about the current user, used to determine access permissions.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains a list of <see
        /// cref="UserGroup"/> objects  that the current user has administrative access to. If no groups are accessible,
        /// the list will be empty.</returns>
        public async Task<List<UserGroup>> GetUserGroupsForProgeny(int progenyId, UserInfo currentUserInfo)
        {
            List<UserGroup> groups = await progenyDbContext.UserGroupsDb.AsNoTracking().Where(ug => ug.ProgenyId == progenyId).ToListAsync();
            List<UserGroup> accessibleGroups = [];
            foreach (UserGroup group in groups)
            {
                bool hasAccess = await accessManagementService.HasProgenyPermission(progenyId, currentUserInfo, PermissionLevel.Admin);
                if (!hasAccess) continue;
                ProgenyPermission groupPermission = await accessManagementService.GetProgenyPermissionForGroup(group.ProgenyId, group.UserGroupId, currentUserInfo);
                group.PermissionLevel = groupPermission.PermissionLevel;
                group.Members = await GetUserGroupMembersList(group.UserGroupId);
                accessibleGroups.Add(group);
            }

            return accessibleGroups;
        }

        /// <summary>
        /// Retrieves a list of user groups associated with the specified family that the current user has access to.
        /// </summary>
        /// <remarks>Only user groups for which the current user has administrative permissions are
        /// included in the result.</remarks>
        /// <param name="familyId">The unique identifier of the family whose user groups are to be retrieved.</param>
        /// <param name="currentUserInfo">The information of the current user, used to determine access permissions.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains a list of <see
        /// cref="UserGroup"/> objects  that the current user has access to. Each user group includes its associated
        /// members.</returns>
        public async Task<List<UserGroup>> GetUserGroupsForFamily(int familyId, UserInfo currentUserInfo)
        {
            List<UserGroup> groups = await progenyDbContext.UserGroupsDb.AsNoTracking().Where(ug => ug.FamilyId == familyId).ToListAsync();
            List<UserGroup> accessibleGroups = [];
            foreach (UserGroup group in groups)
            {
                bool hasAccess = await accessManagementService.HasFamilyPermission(familyId, currentUserInfo, PermissionLevel.Admin);
                if (!hasAccess) continue;
                FamilyPermission groupPermission = await accessManagementService.GetFamilyPermissionForGroup(group.FamilyId, group.UserGroupId, currentUserInfo);
                group.PermissionLevel = groupPermission.PermissionLevel;
                group.Members = await GetUserGroupMembersList(group.UserGroupId);
                accessibleGroups.Add(group);
            }
            return accessibleGroups;
        }

        public async Task<List<UserGroup>> GetUsersUserGroupsByUserId(string userId, UserInfo currentUserInfo)
        {
            List<UserGroupMember> allUsersUserGroupMembers = await progenyDbContext.UserGroupMembersDb.AsNoTracking().Where(ugm => ugm.UserId == userId).ToListAsync();
            List<UserGroup> accessibleUserGroups = [];
            foreach (UserGroupMember member in allUsersUserGroupMembers)
            {
                UserGroup group = await progenyDbContext.UserGroupsDb.AsNoTracking().FirstOrDefaultAsync(ug => ug.UserGroupId == member.UserGroupId);
                if (group == null) continue;
                bool hasAccess = false;
                if (group.FamilyId != 0)
                {
                    hasAccess = await accessManagementService.HasFamilyPermission(group.FamilyId, currentUserInfo, PermissionLevel.Edit);
                }
                else
                {
                    if (group.ProgenyId != 0)
                    {
                        hasAccess = await accessManagementService.HasProgenyPermission(group.ProgenyId, currentUserInfo, PermissionLevel.Edit);
                    }
                }
                //
                if (!hasAccess) continue;
                
                accessibleUserGroups.Add(group);
            }

            return accessibleUserGroups;
        }

        public async Task<List<UserGroup>> GetUsersUserGroupsByEmail(string userEmail, UserInfo currentUserInfo)
        {
            userEmail = userEmail.Trim();

            List<UserGroupMember> allUsersUserGroupMembers = await progenyDbContext.UserGroupMembersDb.AsNoTracking().Where(ugm => ugm.Email.ToUpper() == userEmail.ToUpper()).ToListAsync();
            List<UserGroup> accessibleUserGroups = [];
            foreach (UserGroupMember member in allUsersUserGroupMembers)
            {
                UserGroup group = await progenyDbContext.UserGroupsDb.AsNoTracking().FirstOrDefaultAsync(ug => ug.UserGroupId == member.UserGroupId);
                if (group == null) continue;
                bool hasAccess = false;
                if (group.FamilyId != 0)
                {
                    hasAccess = await accessManagementService.HasFamilyPermission(group.FamilyId, currentUserInfo, PermissionLevel.Edit);
                }
                else
                {
                    if (group.ProgenyId != 0)
                    {
                        hasAccess = await accessManagementService.HasProgenyPermission(group.ProgenyId, currentUserInfo, PermissionLevel.Edit);
                    }
                }

                if (!hasAccess) continue;
                
                accessibleUserGroups.Add(group);
            }

            return accessibleUserGroups;
        }

        /// <summary>
        /// Adds a new user group to the database.
        /// </summary>
        /// <param name="userGroup">The <see cref="UserGroup"/> object to be added. This object must contain the necessary details about the user group.</param>
        /// <param name="currentUserInfo">The information about the current user, used to verify access permissions.</param>
        /// <returns>The added <see cref="UserGroup"/> object if the operation is successful; otherwise, <see langword="null"/> if the current user lacks the required access rights.</returns>
        public async Task<UserGroup> AddUserGroup(UserGroup userGroup, UserInfo currentUserInfo)
        {
            // Check if the user creating the group has Edit permission for the family or progeny.
            bool hasAccess = false;
            if (userGroup.FamilyId != 0)
            {
                hasAccess = await accessManagementService.HasFamilyPermission(userGroup.FamilyId, currentUserInfo, PermissionLevel.Edit);
            }
            else
            {
                if (userGroup.ProgenyId != 0)
                {
                    hasAccess = await accessManagementService.HasProgenyPermission(userGroup.ProgenyId, currentUserInfo, PermissionLevel.Edit);
                }
            }

            if (!hasAccess)
            {
                return null;
            }

            userGroup.CreatedTime = DateTime.UtcNow;
            userGroup.ModifiedTime = DateTime.UtcNow;

            progenyDbContext.UserGroupsDb.Add(userGroup);
            await progenyDbContext.SaveChangesAsync();

            await userGroupAuditLogService.AddUserGroupCreatedAuditLogEntry(userGroup, currentUserInfo);

            return userGroup;
        }

        /// <summary>
        /// Updates the details of an existing user group in the database.
        /// </summary>
        /// <param name="userGroup">The <see cref="UserGroup"/> object containing the updated details of the user group. The <see cref="UserGroup.UserGroupId"/> property must correspond to an existing user group.</param>
        /// <param name="currentUserInfo">The information about the current user, used to verify access permissions.</param>
        /// <returns>The updated <see cref="UserGroup"/> object if the operation is successful; otherwise, <see langword="null"/> if the user group does not exist or the current user lacks the required access rights.</returns>
        public async Task<UserGroup> UpdateUserGroup(UserGroup userGroup, UserInfo currentUserInfo)
        {
            UserGroup group = await progenyDbContext.UserGroupsDb.SingleOrDefaultAsync(ug => ug.UserGroupId == userGroup.UserGroupId);
            if (group == null)
            {
                return null;
            }

            // Check if the user updating the group has Edit permission for the family or progeny.
            bool hasAccess = false;
            if (userGroup.FamilyId != 0)
            {
                hasAccess = await accessManagementService.HasFamilyPermission(userGroup.FamilyId, currentUserInfo, PermissionLevel.Edit);
            }
            else
            {
                if (userGroup.ProgenyId != 0)
                {
                    hasAccess = await accessManagementService.HasProgenyPermission(userGroup.ProgenyId, currentUserInfo, PermissionLevel.Edit);
                }
            }

            if (!hasAccess)
            {
                return null;
            }

            UserGroupAuditLog logEntry = await userGroupAuditLogService.AddUserGroupUpdatedAuditLogEntry(group, currentUserInfo);

            group.IsFamily = userGroup.IsFamily;
            group.Name = userGroup.Name;
            group.Description = userGroup.Description;
            group.ProgenyId = userGroup.ProgenyId;
            group.FamilyId = userGroup.FamilyId;
            group.ModifiedBy = userGroup.ModifiedBy;
            group.ModifiedTime = DateTime.UtcNow;
            
            await progenyDbContext.SaveChangesAsync();

            logEntry.EntityAfter = System.Text.Json.JsonSerializer.Serialize(group);
            await userGroupAuditLogService.UpdateUserGroupAuditLogEntry(logEntry);
            return group;
        }

        /// <summary>
        /// Removes a user group from the database, including all its members.
        /// </summary>
        /// <param name="groupId">The unique identifier of the user group to be removed.</param>
        /// <param name="currentUserInfo">The information about the current user, used to verify access permissions.</param>
        /// <returns>Boolean value indicating whether the user group was successfully removed. Returns <see langword="true"/>
        /// if the operation is successful; otherwise, <see langword="false"/>
        /// if the user group does not exist or the current user lacks the required access rights.</returns>
        public async Task<bool> RemoveUserGroup(int groupId, UserInfo currentUserInfo)
        {
            UserGroup group = await progenyDbContext.UserGroupsDb.SingleOrDefaultAsync(ug => ug.UserGroupId == groupId);
            if (group == null)
            {
                return false;
            }

            // Check if the user deleting the group has Edit permission for the family or progeny.
            bool hasAccess = false;
            if (group.FamilyId != 0)
            {
                hasAccess = await accessManagementService.HasFamilyPermission(group.FamilyId, currentUserInfo, PermissionLevel.Edit);
            }
            else
            {
                if (group.ProgenyId != 0)
                {
                    hasAccess = await accessManagementService.HasProgenyPermission(group.ProgenyId, currentUserInfo, PermissionLevel.Edit);
                }
            }

            if (!hasAccess)
            {
                return false;
            }

            // Remove all members in the group first.
            List<UserGroupMember> members = await GetUserGroupMembersList(groupId);
            if (members.Count > 0)
            {
                progenyDbContext.UserGroupMembersDb.RemoveRange(members);
            }
            
            progenyDbContext.UserGroupsDb.Remove(group);
            await progenyDbContext.SaveChangesAsync();

            await userGroupAuditLogService.AddUserGroupDeletedAuditLogEntry(group, currentUserInfo);

            return true;
        }

        /// <summary>
        /// Gets the list of members in a user group.
        /// </summary>
        /// <param name="userGroupId">The unique identifier of the user group whose members are to be retrieved.</param>
        /// <returns></returns>
        private async Task<List<UserGroupMember>> GetUserGroupMembersList(int userGroupId)
        {
            List<UserGroupMember> members = await progenyDbContext.UserGroupMembersDb.AsNoTracking().Where(ugm => ugm.UserGroupId == userGroupId).ToListAsync();
            // Todo: Filter out members that the current user should not see?
            return members;
        }

        /// <summary>
        /// Adds a new member to a user group.
        /// </summary>
        /// <param name="userGroupMember">The <see cref="UserGroupMember"/> object representing the member to add. The <see cref="UserGroupMember.UserGroupId"/> property must be set to the ID of the target user group.</param>
        /// <param name="currentUserInfo">The <see cref="UserInfo"/> object representing the user performing the operation. This user must have sufficient permissions to add members to the specified user group.</param>
        /// <returns>UserGroupMember object representing the newly added member, or <see langword="null"/> if the operation fails due to insufficient permissions or an invalid user group ID.</returns>
        public async Task<UserGroupMember> AddUserGroupMember(UserGroupMember userGroupMember, UserInfo currentUserInfo)
        {
            // Check if the user adding the member has Edit permission for the family or progeny.
            UserGroup group = await progenyDbContext.UserGroupsDb.AsNoTracking().FirstOrDefaultAsync(ug => ug.UserGroupId == userGroupMember.UserGroupId);
            if (group == null)
            {
                return null;
            }
            bool hasAccess = false;
            if (group.FamilyId != 0)
            {
                hasAccess = await accessManagementService.HasFamilyPermission(group.FamilyId, currentUserInfo, PermissionLevel.Edit);
            }
            else
            {
                if (group.ProgenyId != 0)
                {
                    hasAccess = await accessManagementService.HasProgenyPermission(group.ProgenyId, currentUserInfo, PermissionLevel.Edit);
                }
            }
            if (!hasAccess)
            {
                return null;
            }
            // Trim email and check if there is a user with this email.
            if (!string.IsNullOrEmpty(userGroupMember.Email))
            {
                userGroupMember.Email = userGroupMember.Email.Trim();
                UserInfo user = await progenyDbContext.UserInfoDb.AsNoTracking().FirstOrDefaultAsync(u => u.UserEmail.ToLower() == userGroupMember.Email.ToLower());
                if (user != null)
                {
                    userGroupMember.UserId = user.UserId;
                }
            }

            userGroupMember.CreatedTime = DateTime.UtcNow;
            userGroupMember.ModifiedTime = DateTime.UtcNow;

            progenyDbContext.UserGroupMembersDb.Add(userGroupMember);
            await progenyDbContext.SaveChangesAsync();

            await userGroupAuditLogService.AddUserGroupMemberAddedAuditLogEntry(userGroupMember, currentUserInfo);

            return userGroupMember;
        }

        /// <summary>
        /// Updates a user group member.
        /// </summary>
        /// <param name="userGroupMember">The <see cref="UserGroupMember"/> object containing the updated details of the user group member. The <see cref="UserGroupMember.UserGroupMemberId"/> property must correspond to an existing user group member.</param>
        /// <param name="currentUserInfo">The information about the current user, used to verify access permissions.</param>
        /// <returns>The updated <see cref="UserGroupMember"/> object if the operation is successful; otherwise, <see langword="null"/> if the user group member does not exist or the current user lacks the required access rights.</returns>
        public async Task<UserGroupMember> UpdateUserGroupMember(UserGroupMember userGroupMember, UserInfo currentUserInfo)
        {
            UserGroupMember member = await progenyDbContext.UserGroupMembersDb.SingleOrDefaultAsync(u => u.UserGroupMemberId == userGroupMember.UserGroupMemberId);
            if (member == null)
            {
                return null;
            }
            // Check if the user updating the member has Edit permission for the family or progeny.
            UserGroup group = await progenyDbContext.UserGroupsDb.AsNoTracking().FirstOrDefaultAsync(ug => ug.UserGroupId == userGroupMember.UserGroupId);
            if (group == null)
            {
                return null;
            }
            bool hasAccess = false;
            if (group.FamilyId != 0)
            {
                hasAccess = await accessManagementService.HasFamilyPermission(group.FamilyId, currentUserInfo, PermissionLevel.Edit);
            }
            else
            {
                if (group.ProgenyId != 0)
                {
                    hasAccess = await accessManagementService.HasProgenyPermission(group.ProgenyId, currentUserInfo, PermissionLevel.Edit);
                }
            }
            if (!hasAccess)
            {
                return null;
            }

            UserGroupAuditLog logEntry = await userGroupAuditLogService.AddUserGroupMemberUpdatedAuditLogEntry(member, currentUserInfo);

            // If UserId is missing, trim email and check if there is a user with this email.
            if (string.IsNullOrWhiteSpace(userGroupMember.UserId) &&!string.IsNullOrEmpty(userGroupMember.Email))
            {
                userGroupMember.Email = userGroupMember.Email.Trim();
                UserInfo user = await progenyDbContext.UserInfoDb.AsNoTracking().FirstOrDefaultAsync(u => u.UserEmail.ToLower() == userGroupMember.Email.ToLower());
                if (user != null)
                {
                    userGroupMember.UserId = user.UserId;
                }
            }
            
            member.Email = userGroupMember.Email;
            member.UserId = userGroupMember.UserId;
            member.UserGroupId = userGroupMember.UserGroupId;
            member.UserOwnerUserId = userGroupMember.UserOwnerUserId;
            member.FamilyOwnerId = userGroupMember.FamilyOwnerId;
            member.ModifiedBy = userGroupMember.ModifiedBy;
            member.ModifiedTime = DateTime.UtcNow;
            
            await progenyDbContext.SaveChangesAsync();

            logEntry.EntityAfter = System.Text.Json.JsonSerializer.Serialize(member);
            await userGroupAuditLogService.UpdateUserGroupAuditLogEntry(logEntry);

            return member;
        }

        /// <summary>
        /// Removes a user group member from the database.
        /// </summary>
        /// <param name="userGroupMemberId">The unique identifier of the user group member to be removed.</param>
        /// <param name="currentUserInfo">The information about the current user, used to verify access permissions.</param>
        /// <returns>True if the user group member was successfully removed; otherwise, false if the user group member does not exist or the current user lacks the required access rights.</returns>
        public async Task<bool> RemoveUserGroupMember(int userGroupMemberId, UserInfo currentUserInfo)
        {
            UserGroupMember member = await progenyDbContext.UserGroupMembersDb.SingleOrDefaultAsync(u => u.UserGroupMemberId == userGroupMemberId);
            if (member == null)
            {
                return false;
            }
            // Check if the user deleting the member has Edit permission for the family or progeny.
            UserGroup group = await progenyDbContext.UserGroupsDb.AsNoTracking().FirstOrDefaultAsync(ug => ug.UserGroupId == member.UserGroupId);
            if (group == null)
            {
                return false;
            }
            bool hasAccess = false;
            if (group.FamilyId != 0)
            {
                hasAccess = await accessManagementService.HasFamilyPermission(group.FamilyId, currentUserInfo, PermissionLevel.Edit);
            }
            else
            {
                if (group.ProgenyId != 0)
                {
                    hasAccess = await accessManagementService.HasProgenyPermission(group.ProgenyId, currentUserInfo, PermissionLevel.Edit);
                }
            }
            if (!hasAccess)
            {
                return false;
            }
            
            progenyDbContext.UserGroupMembersDb.Remove(member);
            await progenyDbContext.SaveChangesAsync();

            await userGroupAuditLogService.AddUserGroupMemberDeletedAuditLogEntry(member, currentUserInfo);

            return true;
        }
        
    }
}
