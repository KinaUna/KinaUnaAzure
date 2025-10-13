using KinaUna.Data.Models.AccessManagement;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace KinaUnaWeb.Services.HttpClients
{
    /// <summary>
    /// Provides methods to interact with the UserAccess API.
    /// </summary>
    public interface IUserAccessHttpClient
    {
        /// <summary>
        /// Retrieves a list of permissions for a specific timeline item.
        /// </summary>
        /// <remarks>This method retrieves the permissions for a timeline item by making an HTTP request
        /// to the Access Management API.  The caller must ensure that the user is authenticated, as the method requires
        /// a valid access token to perform the operation.</remarks>
        /// <param name="itemType">The type of the timeline item, represented as a <see cref="KinaUnaTypes.TimeLineType"/> enumeration.</param>
        /// <param name="itemId">The unique identifier of the timeline item.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains a list of <see
        /// cref="TimelineItemPermission"/> objects representing the permissions for the specified timeline item. If the
        /// operation fails, an empty list is returned.</returns>
        Task<List<TimelineItemPermission>> GetTimelineItemPermissionsList(KinaUnaTypes.TimeLineType itemType, int itemId);

        Task<bool> ConvertUserAccessesToUserGroups();

        Task<bool> ConvertItemAccessLevelToItemPermissions(int itemType);
    }
}
