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
    /// Provides methods for managing families, including retrieving, creating, updating, and deleting family records,
    /// as well as managing family members and permissions.
    /// </summary>
    /// <remarks>This service is designed to handle operations related to family entities, including access
    /// control based on user permissions. It ensures that only authorized users can perform actions on family data. The
    /// service interacts with the database to retrieve and modify family-related information.</remarks>
    /// <param name="progenyDbContext"></param>
    /// <param name="familyMembersService"></param>
    public class FamilyService(ProgenyDbContext progenyDbContext, IFamilyMembersService familyMembersService, IAccessManagementService accessManagementService): IFamilyService
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
            FamilyPermission familyPermission = await progenyDbContext.FamilyPermissionsDb.AsNoTracking()
                .SingleOrDefaultAsync(fp => fp.FamilyId == familyId && fp.UserId == currentUserInfo.UserId);
            if (familyPermission == null || familyPermission.PermissionLevel == PermissionLevel.None)
            {
                return new Family(); // No access to this family. Return empty family object.
            }

            Family family = await progenyDbContext.FamiliesDb.AsNoTracking().SingleOrDefaultAsync(f => f.FamilyId == familyId);
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
        /// <returns>A list of <see cref="Family"/> objects representing the families associated with the specified email
        /// address.  If no families are found, an empty list is returned.</returns>
        public async Task<List<Family>> GetUsersFamiliesByEmail(string userEmail)
        {
            List<FamilyMember> familyMemberItems = await progenyDbContext.FamilyMembersDb.AsNoTracking().Where(fm  => fm.Email == userEmail.Trim().ToLower()).ToListAsync();

            List<Family> userFamilies = [];
            foreach (FamilyMember familyMember in familyMemberItems)
            {
                if (!userFamilies.Exists(f => f.FamilyId == familyMember.FamilyId))
                {
                    Family family = await progenyDbContext.FamiliesDb.AsNoTracking().SingleOrDefaultAsync(f => f.FamilyId == familyMember.FamilyId);
                    userFamilies.Add(family);
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
        /// <returns>A task that represents the asynchronous operation. The task result contains a list of <see cref="Family"/>
        /// objects associated with the user.  If the user is not associated with any families, the list will be empty.</returns>
        public async Task<List<Family>> GetUsersFamiliesByUserId(string userId)
        {
            List<FamilyMember> familyMemberItems = await progenyDbContext.FamilyMembersDb.AsNoTracking().Where(fm => fm.UserId == userId).ToListAsync();

            List<Family> userFamilies = [];
            foreach (FamilyMember familyMember in familyMemberItems)
            {
                if (!userFamilies.Exists(f => f.FamilyId == familyMember.FamilyId))
                {
                    Family family = await progenyDbContext.FamiliesDb.AsNoTracking().SingleOrDefaultAsync(f => f.FamilyId == familyMember.FamilyId);
                    userFamilies.Add(family);
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
            // Todo: Audit log entry.

            // Add FamilyPermissions for all admins in the family.
            foreach (string adminEmail in family.GetAdminsList())
            {
                UserInfo userInfo = await progenyDbContext.UserInfoDb.AsNoTracking().SingleOrDefaultAsync(u => u.UserEmail.ToUpper() == adminEmail.ToUpper());

                FamilyPermission familyPermission = new()
                {
                    FamilyId = family.FamilyId,
                    PermissionLevel = PermissionLevel.Admin,
                    UserId = userInfo?.UserId ?? string.Empty,
                    Email = adminEmail,
                    CreatedBy = currentUserInfo.UserEmail,
                    CreatedTime = System.DateTime.UtcNow,
                    ModifiedBy = currentUserInfo.UserEmail,
                    ModifiedTime = System.DateTime.UtcNow
                };

                await accessManagementService.GrantFamilyPermission(familyPermission, currentUserInfo);
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
            // Todo: Audit log entry.

            // Add FamilyPermissions for new admins
            foreach (string newAdmin in newAdmins)
            {
                if (!existingAdmins.Exists(a => a.ToUpper() == newAdmin.ToUpper()))
                {
                    // Check if we already have a permission entry for this user.
                    UserInfo userInfo = await progenyDbContext.UserInfoDb.AsNoTracking().SingleOrDefaultAsync(u => u.UserEmail.ToUpper() == newAdmin.ToUpper());
                    FamilyPermission familyPermission = new FamilyPermission
                    {
                        FamilyId = existingFamily.FamilyId,
                        PermissionLevel = PermissionLevel.Admin,
                        UserId = userInfo?.UserId ?? string.Empty,
                        Email = newAdmin,
                    };

                    FamilyPermission existingPermission = await accessManagementService.UpdateFamilyPermission(familyPermission, currentUserInfo);
                    if (existingPermission == null)
                    {
                        await accessManagementService.GrantFamilyPermission(familyPermission, currentUserInfo);
                    }
                }
            }


            // Remove FamilyPermissions for old admins
            foreach (string existingAdmin in existingAdmins)
            {
                if (!newAdmins.Exists(a => a.ToUpper() == existingAdmin.ToUpper()))
                {
                    // Remove admin permission for this user.
                    UserInfo userInfo = await progenyDbContext.UserInfoDb.AsNoTracking().SingleOrDefaultAsync(u => u.UserEmail.ToUpper() == existingAdmin.ToUpper());
                    FamilyPermission familyPermission = new FamilyPermission
                    {
                        FamilyId = existingFamily.FamilyId,
                        PermissionLevel = PermissionLevel.Edit,
                        UserId = userInfo?.UserId ?? string.Empty,
                        Email = existingAdmin,
                    };
                    _ = await accessManagementService.UpdateFamilyPermission(familyPermission, currentUserInfo);
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

            // Remove all family members and permissions as well.
            List<FamilyMember> familyMembers = await familyMembersService.GetFamilyMembersForFamily(familyId, currentUserInfo);
            if (familyMembers.Count > 0)
            {
                foreach (FamilyMember familyMember in familyMembers)
                {
                    await familyMembersService.DeleteFamilyMember(familyMember.FamilyMemberId, currentUserInfo);
                }
            }
            
            List<FamilyPermission> familyPermissions = await progenyDbContext.FamilyPermissionsDb.Where(fp => fp.FamilyId == familyId && fp.UserId != currentUserInfo.UserId).ToListAsync();
            if (familyPermissions.Count > 0)
            {
                foreach (FamilyPermission familyPermission in familyPermissions)
                {
                    await accessManagementService.RevokeFamilyPermission(familyPermission, currentUserInfo);
                }
            }

            // Current user cannot remove own permission from the access management service, so we do it here.
            FamilyPermission ownPermission = await progenyDbContext.FamilyPermissionsDb.SingleOrDefaultAsync(fp => fp.FamilyId == familyId && fp.UserId == currentUserInfo.UserId);
            if (ownPermission != null)
            {
                progenyDbContext.FamilyPermissionsDb.Remove(ownPermission);
                await progenyDbContext.SaveChangesAsync();
            }

            // Todo: Audit log the deletion of the family and related data.

            return true;
        }
    }
}
