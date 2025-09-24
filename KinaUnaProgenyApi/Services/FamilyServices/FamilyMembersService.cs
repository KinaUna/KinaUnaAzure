using System;
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

namespace KinaUnaProgenyApi.Services.FamilyServices
{
    /// <summary>
    /// Provides functionality for managing family members within a family, including adding, updating, and deleting
    /// family members.
    /// </summary>
    /// <remarks>This service enforces permission checks to ensure that only authorized users can perform
    /// operations on family members. Permissions are determined based on the user's role within the family and their
    /// associated permission level.</remarks>
    /// <param name="progenyDbContext"></param>
    public class FamilyMembersService(ProgenyDbContext progenyDbContext, IAccessManagementService accessManagementService) : IFamilyMembersService
    {
        /// <summary>
        /// Adds a new family member to the specified family and assigns the given permission level.
        /// </summary>
        /// <remarks>This method performs the following actions: <list type="bullet"> <item>Validates that
        /// the current user has the necessary permissions to add a family member to the specified family.</item>
        /// <item>Trims and validates the email address of the new family member, if provided, and associates it with an
        /// existing user account if one exists.</item> <item>Creates or updates the permission entry for the new family
        /// member with the specified permission level.</item> </list> If the specified family does not exist, or if the
        /// current user lacks the required permissions, the method returns <see langword="null"/> without making any
        /// changes.</remarks>
        /// <param name="familyMember">The <see cref="FamilyMember"/> object representing the family member to add. The <see
        /// cref="FamilyMember.FamilyId"/> property must be set to the ID of the target family.</param>
        /// <param name="permissionLevel">The <see cref="PermissionLevel"/> to assign to the new family member. This determines the level of access
        /// the family member will have within the family.</param>
        /// <param name="currentUserInfo">The <see cref="UserInfo"/> object representing the user performing the operation. This user must have
        /// sufficient permissions to add family members to the specified family.</param>
        /// <returns>A <see cref="FamilyMember"/> object representing the newly added family member, or <see langword="null"/> if
        /// the operation fails due to insufficient permissions or an invalid family ID.</returns>
        public async Task<FamilyMember> AddFamilyMember(FamilyMember familyMember, PermissionLevel permissionLevel, UserInfo currentUserInfo)
        {
            // Check if the current user has access to add family members to the family.
            bool allowAdd = false;
            Family family = await progenyDbContext.FamiliesDb.SingleOrDefaultAsync(f => f.FamilyId == familyMember.FamilyId);
            if (family == null)
            {
                return null;
            }

            if (family.IsInAdminList(currentUserInfo.UserEmail))
            {
                allowAdd = true;
            }
            else
            {
                if (await accessManagementService.HasFamilyPermission(familyMember.FamilyId, currentUserInfo, PermissionLevel.Edit))
                {
                    allowAdd = true;
                }
            }

            if (!allowAdd)
            {
                return null;
            }

            // Check progeny permissions.
            Progeny progeny = await progenyDbContext.ProgenyDb.SingleOrDefaultAsync(p => p.Id == familyMember.ProgenyId);
            if (progeny == null)
            {
                return null;
            }

            bool hasProgenyAccess = false;
            if (progeny.IsInAdminList(currentUserInfo.UserEmail))
            {
                hasProgenyAccess = true;
            }
            else
            {
                // Check if the user has at least view permission for the progeny.
                
                if (await accessManagementService.HasProgenyPermission(familyMember.ProgenyId, currentUserInfo, PermissionLevel.View))
                {
                    hasProgenyAccess = true;
                }
            }

            if (!hasProgenyAccess)
            {
                return null;
            }

            familyMember.Email = familyMember.Email.Trim();
            if (!string.IsNullOrWhiteSpace(familyMember.Email))
            {
                UserInfo familyMemberUserInfo = await progenyDbContext.UserInfoDb.AsNoTracking().SingleOrDefaultAsync(u => u.UserEmail.ToUpper() == familyMember.Email.ToUpper());
                if (familyMemberUserInfo != null)
                {
                    familyMember.UserId = familyMemberUserInfo.UserId;
                }
            }

            familyMember.CreatedTime = DateTime.UtcNow;
            familyMember.ModifiedTime = DateTime.UtcNow;
            familyMember.CreatedBy = currentUserInfo.UserId;
            familyMember.ModifiedBy = currentUserInfo.UserId;

            progenyDbContext.FamilyMembersDb.Add(familyMember);
            await progenyDbContext.SaveChangesAsync();

            if (string.IsNullOrWhiteSpace(familyMember.Email)) return familyMember;

            // Check if the family member already has a permission entry, if not, create one with the specified permission level.
            FamilyPermission existingPermission = await progenyDbContext.FamilyPermissionsDb
                .SingleOrDefaultAsync(fp => fp.FamilyId == familyMember.FamilyId && fp.Email == familyMember.Email);
            if (existingPermission == null)
            {
                FamilyPermission familyPermission = new()
                {
                    FamilyId = familyMember.FamilyId,
                    UserId = familyMember.UserId,
                    PermissionLevel = permissionLevel,
                    CreatedBy = currentUserInfo.UserId,
                    CreatedTime = DateTime.UtcNow,
                    ModifiedBy = currentUserInfo.UserId,
                    ModifiedTime = DateTime.UtcNow
                };
                progenyDbContext.FamilyPermissionsDb.Add(familyPermission);
            }
            else // Update the permission level to the specified level.
            {
                existingPermission.PermissionLevel = permissionLevel;
                existingPermission.ModifiedBy = currentUserInfo.UserId;
                existingPermission.ModifiedTime = DateTime.UtcNow;
            }

            await progenyDbContext.SaveChangesAsync();
            
            // Todo: Audit log entry.

            return familyMember;
        }

        /// <summary>
        /// Updates the details of an existing family member in the database.
        /// </summary>
        /// <remarks>The method checks whether the <paramref name="currentUserInfo"/> has sufficient
        /// permissions to update the family member. Permissions are granted if the user is an administrator of the
        /// family or has an edit-level permission for the family. If the user lacks the required permissions, the
        /// method returns <see langword="null"/>.</remarks>
        /// <param name="familyMember">The <see cref="FamilyMember"/> object containing the updated details of the family member. The <see
        /// cref="FamilyMember.FamilyMemberId"/> property must correspond to an existing family member.</param>
        /// <param name="currentUserInfo">The <see cref="UserInfo"/> object representing the user performing the update. The user must have the
        /// necessary permissions to modify the family member.</param>
        /// <returns>A <see cref="FamilyMember"/> object representing the updated family member if the operation is successful;
        /// otherwise, <see langword="null"/> if the family member does not exist, the family does not exist, or the
        /// user lacks the required permissions.</returns>
        public async Task<FamilyMember> UpdateFamilyMember(FamilyMember familyMember, UserInfo currentUserInfo)
        {
            // Check if the current user has access to update family members in the family.
            bool allowUpdate = false;
            Family family = await progenyDbContext.FamiliesDb.SingleOrDefaultAsync(f => f.FamilyId == familyMember.FamilyId);
            if (family == null)
            {
                return null;
            }

            if (family.IsInAdminList(currentUserInfo.UserEmail))
            {
                allowUpdate = true;
            }
            else
            {
                // Check if the user has edit permission for the family.
                if (await accessManagementService.HasFamilyPermission(familyMember.FamilyId, currentUserInfo, PermissionLevel.Edit))
                {
                    allowUpdate = true;
                }
            }

            if (!allowUpdate)
            {
                return null;
            }

            // Check progeny permissions.
            Progeny progeny = await progenyDbContext.ProgenyDb.SingleOrDefaultAsync(p => p.Id == familyMember.ProgenyId);
            if (progeny == null)
            {
                return null;
            }

            bool hasProgenyAccess = false;
            if (progeny.IsInAdminList(currentUserInfo.UserEmail))
            {
                hasProgenyAccess = true;
            }
            else
            {
                // Check if the user has at least view permission for the progeny.

                if (await accessManagementService.HasProgenyPermission(familyMember.ProgenyId, currentUserInfo, PermissionLevel.View))
                {
                    hasProgenyAccess = true;
                }
            }

            if (!hasProgenyAccess)
            {
                return null;
            }

            FamilyMember existingFamilyMember = await progenyDbContext.FamilyMembersDb.SingleOrDefaultAsync(fm => fm.FamilyMemberId == familyMember.FamilyMemberId);
            if (existingFamilyMember == null)
            {
                return null;
            }

            existingFamilyMember.UserId = familyMember.UserId;
            existingFamilyMember.MemberType = familyMember.MemberType;
            existingFamilyMember.Email = familyMember.Email;
            existingFamilyMember.MemberType = familyMember.MemberType;
            existingFamilyMember.ModifiedBy = currentUserInfo.UserId;
            existingFamilyMember.ModifiedTime = DateTime.UtcNow;
            existingFamilyMember.ProgenyId = familyMember.ProgenyId;
            existingFamilyMember.FamilyId = familyMember.FamilyId;

            progenyDbContext.FamilyMembersDb.Update(existingFamilyMember);
            await progenyDbContext.SaveChangesAsync();

            // Todo: Audit log entry.

            return existingFamilyMember;
        }

        /// <summary>
        /// Deletes a family member from the database and removes associated permissions.
        /// </summary>
        /// <remarks>The method checks whether the current user has administrative permissions for the
        /// family associated with the specified family member. If the user does not have the required permissions, the
        /// method returns <see langword="false"/> without making any changes. Additionally, any permissions associated
        /// with the deleted family member are also removed.</remarks>
        /// <param name="familyMemberId">The unique identifier of the family member to delete.</param>
        /// <param name="currentUserInfo">The information of the user attempting to perform the deletion. This is used to verify permissions.</param>
        /// <returns><see langword="true"/> if the family member was successfully deleted; otherwise, <see langword="false"/>.</returns>
        public async Task<bool> DeleteFamilyMember(int familyMemberId, UserInfo currentUserInfo)
        {
            FamilyMember familyMember = await progenyDbContext.FamilyMembersDb.SingleOrDefaultAsync(fm => fm.FamilyMemberId == familyMemberId);
            if (familyMember == null)
            {
                return false;
            }

            // Check if the current user has access to delete family members from the family.
            bool allowDelete = false;
            Family family = await progenyDbContext.FamiliesDb.SingleOrDefaultAsync(f => f.FamilyId == familyMember.FamilyId);
            if (family == null)
            {
                return false;
            }

            if (family.IsInAdminList(currentUserInfo.UserEmail))
            {
                allowDelete = true;
            }
            else
            {
                // Only admins can delete family members.
                if (await accessManagementService.HasFamilyPermission(familyMember.FamilyId, currentUserInfo, PermissionLevel.Admin))
                {
                    allowDelete = true;
                }
            }

            if (!allowDelete)
            {
                return false;
            }

            // Check progeny permissions.
            Progeny progeny = await progenyDbContext.ProgenyDb.SingleOrDefaultAsync(p => p.Id == familyMember.ProgenyId);
            if (progeny == null)
            {
                return false;
            }

            bool hasProgenyAccess = false;
            if (progeny.IsInAdminList(currentUserInfo.UserEmail))
            {
                hasProgenyAccess = true;
            }
            else
            {
                // Check if the user has at least view permission for the progeny.

                if (await accessManagementService.HasProgenyPermission(familyMember.ProgenyId, currentUserInfo, PermissionLevel.View))
                {
                    hasProgenyAccess = true;
                }
            }

            if (!hasProgenyAccess)
            {
                return false;
            }

            progenyDbContext.FamilyMembersDb.Remove(familyMember);

            // Also remove any permissions associated with this family member.
            List<FamilyPermission> permissions = await progenyDbContext.FamilyPermissionsDb
                .Where(p => p.UserId == familyMember.UserId && p.FamilyId == familyMember.FamilyId)
                .ToListAsync();
            if (permissions.Count > 0)
            {
                progenyDbContext.FamilyPermissionsDb.RemoveRange(permissions);
            }
            
            await progenyDbContext.SaveChangesAsync();

            // Todo: Audit log entry.

            return true;
        }

        /// <summary>
        /// Retrieves a list of family members associated with the specified family.
        /// </summary>
        /// <remarks>This method checks the user's access permissions for the specified family before
        /// retrieving the family members.  If the user does not have the required permissions, the method returns an
        /// empty list.</remarks>
        /// <param name="familyId">The unique identifier of the family whose members are to be retrieved.</param>
        /// <param name="currentUserInfo">The user information of the currently authenticated user, used to verify access permissions.</param>
        /// <returns>A list of <see cref="FamilyMember"/> objects representing the members of the specified family.  Returns an
        /// empty list if the user does not have permission to access the family.</returns>
        public async Task<List<FamilyMember>> GetFamilyMembersForFamily(int familyId, UserInfo currentUserInfo)
        {
            // Check if user has access to this family.
            
            if (!await accessManagementService.HasFamilyPermission(familyId, currentUserInfo, PermissionLevel.View))
            {
                return []; // No access to this family. Return empty list.
            }

            List<FamilyMember> familyMembers = await progenyDbContext.FamilyMembersDb.AsNoTracking().Where(fm => fm.FamilyId == familyId).ToListAsync();
            // Filter out any family members that the user does not have access to.
            List<FamilyMember> accessibleFamilyMembers = [];
            foreach (FamilyMember familyMember in familyMembers)
            {
                if (familyMember.ProgenyId > 0)
                {
                    if (await accessManagementService.HasProgenyPermission(familyMember.ProgenyId, currentUserInfo, PermissionLevel.View))
                    {
                        accessibleFamilyMembers.Add(familyMember);
                    }
                }
                else
                {
                    // The family member is not associated with a specific progeny, so we include them as no data is available anyway.
                    accessibleFamilyMembers.Add(familyMember);
                }
            }

            return accessibleFamilyMembers;
        }
    }
}
