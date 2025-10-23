using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using KinaUna.Data.Contexts;
using KinaUna.Data.Extensions;
using KinaUna.Data.Models;
using KinaUna.Data.Models.AccessManagement;
using KinaUna.Data.Models.Family;
using KinaUnaProgenyApi.Services.AccessManagementService;
using Microsoft.EntityFrameworkCore;

namespace KinaUnaProgenyApi.Services.FamiliesServices
{
    /// <summary>
    /// Provides methods for managing families, including retrieving, creating, updating, and deleting family records,
    /// as well as managing family members and permissions.
    /// </summary>
    /// <remarks>This service is designed to handle operations related to family entities, including access
    /// control based on user permissions. It ensures that only authorized users can perform actions on family data. The
    /// service interacts with the database to retrieve and modify family-related information.</remarks>
    /// <param name="progenyDbContext"></param>
    /// <param name="familyMembersService"></param>
    public class FamiliesService(ProgenyDbContext progenyDbContext, IFamilyMembersService familyMembersService,
        IAccessManagementService accessManagementService, IFamilyAuditLogsService familyAuditLogService, 
        IUserGroupsService userGroupsService): IFamiliesService
    {
        /// <summary>
        /// Retrieves a family by its unique identifier, including its members, if the current user has the necessary
        /// permissions.
        /// </summary>
        /// <remarks>This method checks the user's access permissions for the specified family before
        /// retrieving the data. If the user does not have access, the method returns an empty <see cref="Family"/>
        /// object instead of throwing an exception.</remarks>
        /// <param name="familyId">The unique identifier of the family to retrieve.</param>
        /// <param name="currentUserInfo">The information about the current user, used to verify access permissions.</param>
        /// <returns>A <see cref="Family"/> object representing the requested family, including its members, if the user has
        /// access. If the user does not have the required permissions, an empty <see cref="Family"/> object is
        /// returned.</returns>
        public async Task<Family> GetFamilyById(int familyId, UserInfo currentUserInfo)
        {
            // Check if user has access to this family.
            if (!await accessManagementService.HasFamilyPermission(familyId, currentUserInfo, PermissionLevel.View))
            {
                // No access, return empty family.
                return new Family();
            }

            Family family = await progenyDbContext.FamiliesDb.AsNoTracking().SingleOrDefaultAsync(f => f.FamilyId == familyId);
            if (family == null)
            {
                return new Family();
            }

            family.FamilyPermission = await accessManagementService.GetFamilyPermissionForUser(familyId, currentUserInfo);
            family.FamilyMembers = await familyMembersService.GetFamilyMembersForFamily(familyId, currentUserInfo);
            return family;
        }

        /// <summary>
        /// Retrieves a list of families associated with the specified user's email address.
        /// This should not be used under normal circumstances, it is intended for use when a new user signs up,
        /// to check if there are any families associated with the email address they used to sign up.
        /// Normally, use <see cref="GetUsersFamiliesByUserId"/> instead.
        /// </summary>
        /// <remarks>This method performs a case-insensitive search for family memberships based on the
        /// provided email address.  Each family is included only once in the result, even if the user is associated
        /// with multiple roles within the same family.</remarks>
        /// <param name="userEmail">The email address of the user whose families are to be retrieved. This parameter cannot be null or empty.</param>
        /// <param name="currentUserInfo">The information about the current user, used to verify access permissions.</param>
        /// <returns>A list of <see cref="Family"/> objects representing the families associated with the specified email
        /// address.  If no families are found, an empty list is returned.</returns>
        public async Task<List<Family>> GetUsersFamiliesByEmail(string userEmail, UserInfo currentUserInfo)
        {
            List<FamilyMember> familyMemberItems = await progenyDbContext.FamilyMembersDb.AsNoTracking().Where(fm  => fm.Email == userEmail.Trim().ToLower()).ToListAsync();

            List<Family> userFamilies = [];
            foreach (FamilyMember familyMember in familyMemberItems)
            {
                if (!userFamilies.Exists(f => f.FamilyId == familyMember.FamilyId))
                {
                    Family family = await GetFamilyById(familyMember.FamilyId, currentUserInfo);
                    if (family.FamilyId != 0)
                    {
                        family.FamilyPermission = await accessManagementService.GetFamilyPermissionForUser(family.FamilyId, currentUserInfo);
                        userFamilies.Add(family);
                    }
                }
            }

            return userFamilies;
        }

        /// <summary>
        /// Retrieves a list of families associated with the specified user ID.
        /// </summary>
        /// <remarks>This method queries the database to find all families that the specified user is a
        /// member of. Each family is included only once in the result,  even if the user has multiple memberships in
        /// the same family.</remarks>
        /// <param name="userId">The unique identifier of the user whose families are to be retrieved. Cannot be null or empty.</param>
        /// <param name="currentUserInfo">The information about the current user, used to verify access permissions.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains a list of <see cref="Family"/>
        /// objects associated with the user.  If the user is not associated with any families, the list will be empty.</returns>
        public async Task<List<Family>> GetUsersFamiliesByUserId(string userId, UserInfo currentUserInfo)
        {
            List<FamilyMember> familyMemberItems = await progenyDbContext.FamilyMembersDb.AsNoTracking().Where(fm => fm.UserId == userId).ToListAsync();

            List<Family> userFamilies = [];
            foreach (FamilyMember familyMember in familyMemberItems)
            {
                if (!userFamilies.Exists(f => f.FamilyId == familyMember.FamilyId))
                {
                    Family family = await GetFamilyById(familyMember.FamilyId, currentUserInfo);
                    if (family.FamilyId != 0)
                    {
                        family.FamilyPermission = await accessManagementService.GetFamilyPermissionForUser(family.FamilyId, currentUserInfo);
                        userFamilies.Add(family);
                    }
                }
            }

            List<Family> adminFamilies = await progenyDbContext.FamiliesDb.AsNoTracking()
                .Where(f => f.Admins.ToUpper().Contains(currentUserInfo.UserEmail.ToUpper()))
                .ToListAsync();
            foreach (Family adminFamily in adminFamilies)
            {
                if (!userFamilies.Exists(f => f.FamilyId == adminFamily.FamilyId))
                {
                    Family family = await GetFamilyById(adminFamily.FamilyId, currentUserInfo);
                    if (family.FamilyId != 0 && family.IsInAdminList(currentUserInfo.UserEmail))
                    {
                        family.FamilyPermission = await accessManagementService.GetFamilyPermissionForUser(family.FamilyId, currentUserInfo);
                        userFamilies.Add(family);
                    }
                }
            }

            return userFamilies;
        }

        /// <summary>
        /// Adds a new family to the database and assigns administrative permissions to the specified users.
        /// </summary>
        /// <remarks>This method ensures that the user performing the operation is added to the family's
        /// administrator list if not already present.  It also creates and saves corresponding <see
        /// cref="FamilyPermission"/> entries for all administrators of the family, granting them edit-level
        /// permissions.</remarks>
        /// <param name="family">The <see cref="Family"/> object to be added. This object must contain the necessary details about the
        /// family.</param>
        /// <param name="currentUserInfo">The <see cref="UserInfo"/> object representing the user performing the operation. This user will be added as
        /// an administrator of the family if not already included.</param>
        /// <returns>A <see cref="Family"/> object representing the newly added family, including any updates made during the
        /// operation.</returns>
        public async Task<Family> AddFamily(Family family, UserInfo currentUserInfo)
        {
            // Todo: Check if user not a child, and is allowed to create families. Add a UserInfo property for IsAdult or IsLegalAge?

            family.CreatedTime = System.DateTime.UtcNow;
            family.ModifiedTime = System.DateTime.UtcNow;
            family.CreatedBy = currentUserInfo.UserId;
            family.ModifiedBy = currentUserInfo.UserId;
            // Ensure the current user is in the Admins list.
            if (!family.IsInAdminList(currentUserInfo.UserEmail))
            {
                family.AddToAdminList(currentUserInfo.UserEmail);
            }

            // Add the family to the database.
            await progenyDbContext.FamiliesDb.AddAsync(family);
            await progenyDbContext.SaveChangesAsync();

            await familyAuditLogService.AddFamilyCreatedAuditLogEntry(family, currentUserInfo);

            // Add Admin user group for family.
            UserGroup adminGroup = new()
            {
                FamilyId = family.FamilyId,
                Name = "Administrators - " + family.Name,
                CreatedBy = currentUserInfo.UserEmail,
                CreatedTime = System.DateTime.UtcNow,
                ModifiedBy = currentUserInfo.UserEmail,
                ModifiedTime = System.DateTime.UtcNow,
                PermissionLevel = PermissionLevel.Admin
            };
            
            adminGroup = await userGroupsService.AddUserGroup(adminGroup, currentUserInfo);
            
            // Add all admins in the family to admin group.
            foreach (string adminEmail in family.GetAdminsList())
            {
                UserInfo userInfo = await progenyDbContext.UserInfoDb.AsNoTracking().SingleOrDefaultAsync(u => u.UserEmail.ToUpper() == adminEmail.ToUpper());
                
                UserGroupMember groupMember = new()
                {
                    UserGroupId = adminGroup.UserGroupId,
                    UserId = userInfo?.UserId ?? string.Empty,
                    Email = adminEmail,
                    CreatedBy = currentUserInfo.UserEmail,
                    CreatedTime = System.DateTime.UtcNow,
                    ModifiedBy = currentUserInfo.UserEmail,
                    ModifiedTime = System.DateTime.UtcNow
                };

                await userGroupsService.AddUserGroupMember(groupMember, currentUserInfo);

                // Also add as family member.
                FamilyMember familyMember = new()
                {
                    FamilyId = family.FamilyId,
                    MemberType = FamilyMemberType.Unknown,
                    UserId = userInfo?.UserId ?? string.Empty,
                    Email = adminEmail,
                    CreatedBy = currentUserInfo.UserEmail,
                    CreatedTime = System.DateTime.UtcNow,
                    ModifiedBy = currentUserInfo.UserEmail,
                    ModifiedTime = System.DateTime.UtcNow
                };
                await familyMembersService.AddFamilyMember(familyMember, currentUserInfo);
            }
            
            return family;
        }

        /// <summary>
        /// Updates the details of an existing family, including its name, description, and admin list.
        /// </summary>
        /// <remarks>This method updates the family's name, description, and admin list. Changes to the
        /// admin list will also update the associated permissions for the affected users: <list type="bullet">
        /// <item><description>New admins are added to the admin list and granted admin-level
        /// permissions.</description></item> <item><description>Removed admins are downgraded to edit-level permissions
        /// unless they are the last remaining admin.</description></item> </list> The method ensures that at least one
        /// admin remains in the family. If the last admin is removed, the operation is skipped for that user.</remarks>
        /// <param name="family">The <see cref="Family"/> object containing the updated family details. The <see cref="Family.FamilyId"/>
        /// property must match an existing family.</param>
        /// <param name="currentUserInfo">The <see cref="UserInfo"/> object representing the user performing the update. The user must have admin
        /// privileges for the family.</param>
        /// <returns>The updated <see cref="Family"/> object if the update is successful; otherwise, <see langword="null"/> if
        /// the family does not exist or the user lacks admin privileges.</returns>
        public async Task<Family> UpdateFamily(Family family, UserInfo currentUserInfo)
        {
            Family existingFamily = await progenyDbContext.FamiliesDb.SingleOrDefaultAsync(f =>  f.FamilyId == family.FamilyId);
            // Only admins can update family details.
            if (existingFamily == null || !family.IsInAdminList(currentUserInfo.UserEmail))
            {
                return null;
            }

            FamilyAuditLog logEntry = await familyAuditLogService.AddFamilyUpdatedAuditLogEntry(existingFamily, currentUserInfo);

            // Cannot remove yourself from the admin list. This is to ensure there is always at least one valid admin.
            if (!family.IsInAdminList(currentUserInfo.UserEmail))
            {
                family.AddToAdminList(currentUserInfo.UserEmail);
            }

            List<string> existingAdmins = existingFamily.GetAdminsList();
            List<string> newAdmins = family.GetAdminsList();

            existingFamily.Name = family.Name;
            existingFamily.Description = family.Description;
            existingFamily.ModifiedBy = currentUserInfo.UserId;
            existingFamily.ModifiedTime = System.DateTime.UtcNow;
            existingFamily.Admins = family.Admins;

            progenyDbContext.FamiliesDb.Update(existingFamily);
            await progenyDbContext.SaveChangesAsync();
            
            logEntry.EntityAfter = System.Text.Json.JsonSerializer.Serialize(existingFamily);
            await familyAuditLogService.UpdateFamilyAuditLogEntry(logEntry);

            List<UserGroup> familyUserGroups = await userGroupsService.GetUserGroupsForFamily(existingFamily.FamilyId, currentUserInfo);
            UserGroup adminGroup = familyUserGroups.FirstOrDefault(ug => ug.PermissionLevel == PermissionLevel.Admin);
            if (adminGroup == null)
            {
                return existingFamily;
            }
            // Add FamilyPermissions for new admins
            foreach (string newAdmin in newAdmins)
            {
                if (!existingAdmins.Exists(a => a.ToUpper() == newAdmin.ToUpper()))
                {
                    UserInfo userInfo = await progenyDbContext.UserInfoDb.AsNoTracking().SingleOrDefaultAsync(u => u.UserEmail.ToUpper() == newAdmin.ToUpper());
                    UserGroupMember groupMember = new()
                    {
                        UserGroupId = adminGroup.UserGroupId,
                        Email = newAdmin,
                        UserId = userInfo?.UserId ?? string.Empty,
                        CreatedBy = currentUserInfo.UserEmail,
                        CreatedTime = System.DateTime.UtcNow,
                        ModifiedBy = currentUserInfo.UserEmail,
                        ModifiedTime = System.DateTime.UtcNow
                    };
                    await userGroupsService.AddUserGroupMember(groupMember, currentUserInfo);
                }
            }

            // Remove administrator group membership for old admins
            foreach (string existingAdmin in existingAdmins)
            {
                if (!newAdmins.Exists(a => a.ToUpper() == existingAdmin.ToUpper()))
                {
                    // Remove admin permission for this user.
                    UserGroupMember groupMember = adminGroup.Members.SingleOrDefault(gm => gm.Email.ToUpper() == existingAdmin.ToUpper());
                    if (groupMember != null)
                    {
                        await userGroupsService.RemoveUserGroupMember(groupMember.UserGroupMemberId, currentUserInfo);
                    }
                }
            }
            
            return existingFamily;
        }

        /// <summary>
        /// Deletes a family and its associated members and permissions from the database.
        /// </summary>
        /// <remarks>Only users with administrative permissions for the specified family are allowed to
        /// delete it.  If the family does not exist or the caller lacks the necessary permissions, the method returns
        /// <see langword="false"/>. This method also removes all associated family members and permissions from the
        /// database.</remarks>
        /// <param name="familyId">The unique identifier of the family to delete.</param>
        /// <param name="currentUserInfo">The user information of the caller, used to verify permissions.</param>
        /// <returns><see langword="true"/> if the family was successfully deleted; otherwise, <see langword="false"/>.</returns>
        public async Task<bool> DeleteFamily(int familyId, UserInfo currentUserInfo)
        {
            
            Family existingFamily = await progenyDbContext.FamiliesDb.SingleOrDefaultAsync(f => f.FamilyId == familyId);
            // Only admins can delete families.
            if (existingFamily == null || !existingFamily.IsInAdminList(currentUserInfo.UserEmail))
            {
                return false;
            }
            
            progenyDbContext.FamiliesDb.Remove(existingFamily);
            await progenyDbContext.SaveChangesAsync();

            await familyAuditLogService.AddFamilyDeletedAuditLogEntry(existingFamily, currentUserInfo);

            // Remove all family members and permissions as well.
            List<FamilyMember> familyMembers = await familyMembersService.GetFamilyMembersForFamily(familyId, currentUserInfo);
            if (familyMembers.Count > 0)
            {
                foreach (FamilyMember familyMember in familyMembers)
                {
                    await familyMembersService.DeleteFamilyMember(familyMember.FamilyMemberId, currentUserInfo);
                }
            }
            
            List<FamilyPermission> familyPermissions = await progenyDbContext.FamilyPermissionsDb.Where(fp => fp.FamilyId == familyId).ToListAsync();
            if (familyPermissions.Count > 0)
            {
                foreach (FamilyPermission familyPermission in familyPermissions)
                {
                    await accessManagementService.RevokeFamilyPermission(familyPermission, currentUserInfo);
                }
            }
            
            return true;
        }

        /// <summary>
        /// Updates the email address for a user in the admin lists of all families they manage.
        /// </summary>
        /// <remarks>This method identifies all families where the specified user is listed as an admin
        /// and updates the admin list  to replace the user's current email address with the new email address. Changes
        /// are persisted to the database.</remarks>
        /// <param name="userInfo">The user information containing the current email address of the user.</param>
        /// <param name="newEmail">The new email address to replace the user's current email address.</param>
        /// <returns></returns>
        public async Task ChangeUsersEmailForFamilies(UserInfo userInfo, string newEmail)
        {
            List<Family> families = await progenyDbContext.FamiliesDb.Where(fm => fm.Admins.ToUpper().Contains(userInfo.UserEmail.ToUpper())).ToListAsync();
            if (families.Count == 0) return;
            foreach (Family family in families)
            {
                family.RemoveFromAdminList(userInfo.UserEmail);
                family.AddToAdminList(newEmail);

                progenyDbContext.FamiliesDb.Update(family);
            }

            await progenyDbContext.SaveChangesAsync();
        }
    }
}
