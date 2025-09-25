using System.Collections.Generic;
using System.Threading.Tasks;
using KinaUna.Data.Models;
using KinaUna.Data.Models.AccessManagement;
using KinaUna.Data.Models.Family;

namespace KinaUnaProgenyApi.Services.FamiliesServices
{
    /// <summary>
    /// Provides methods for managing families, including retrieving, creating, updating, and deleting family records,
    /// as well as managing family members and permissions.
    /// </summary>
    /// <remarks>This service is designed to handle operations related to family entities, including access
    /// control based on user permissions. It ensures that only authorized users can perform actions on family data. The
    /// service interacts with the database to retrieve and modify family-related information.</remarks>
    public interface IFamiliesService
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
        Task<Family> GetFamilyById(int familyId, UserInfo currentUserInfo);

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
        /// <param name="currentUserInfo"></param>
        /// <returns>A list of <see cref="Family"/> objects representing the families associated with the specified email
        /// address.  If no families are found, an empty list is returned.</returns>
        Task<List<Family>> GetUsersFamiliesByEmail(string userEmail, UserInfo currentUserInfo);

        /// <summary>
        /// Retrieves a list of families associated with the specified user ID.
        /// </summary>
        /// <remarks>This method queries the database to find all families that the specified user is a
        /// member of. Each family is included only once in the result,  even if the user has multiple memberships in
        /// the same family.</remarks>
        /// <param name="userId">The unique identifier of the user whose families are to be retrieved. Cannot be null or empty.</param>
        /// <param name="currentUserInfo"></param>
        /// <returns>A task that represents the asynchronous operation. The task result contains a list of <see cref="Family"/>
        /// objects associated with the user.  If the user is not associated with any families, the list will be empty.</returns>
        Task<List<Family>> GetUsersFamiliesByUserId(string userId, UserInfo currentUserInfo);

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
        Task<Family> AddFamily(Family family, UserInfo currentUserInfo);

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
        Task<Family> UpdateFamily(Family family, UserInfo currentUserInfo);

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
        Task<bool> DeleteFamily(int familyId, UserInfo currentUserInfo);
    }
}
