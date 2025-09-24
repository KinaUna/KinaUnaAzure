using KinaUna.Data.Models;
using KinaUna.Data.Models.AccessManagement;
using KinaUna.Data.Models.Family;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace KinaUnaProgenyApi.Services.FamilyServices
{
    /// <summary>
    /// Provides functionality for managing family members within a family, including adding, updating, and deleting
    /// family members.
    /// </summary>
    /// <remarks>This service enforces permission checks to ensure that only authorized users can perform
    /// operations on family members. Permissions are determined based on the user's role within the family and their
    /// associated permission level.</remarks>
    public interface IFamilyMembersService
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
        Task<FamilyMember> AddFamilyMember(FamilyMember familyMember, PermissionLevel permissionLevel, UserInfo currentUserInfo);

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
        Task<FamilyMember> UpdateFamilyMember(FamilyMember familyMember, UserInfo currentUserInfo);

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
        Task<bool> DeleteFamilyMember(int familyMemberId, UserInfo currentUserInfo);

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
        Task<List<FamilyMember>> GetFamilyMembersForFamily(int familyId, UserInfo currentUserInfo);
    }
}
