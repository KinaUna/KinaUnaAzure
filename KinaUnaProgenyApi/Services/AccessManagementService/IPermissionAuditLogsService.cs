using KinaUna.Data.Models;
using KinaUna.Data.Models.AccessManagement;
using System.Threading.Tasks;

namespace KinaUnaProgenyApi.Services.AccessManagementService
{
    /// <summary>
    /// Provides functionality for managing and auditing permission changes in the system.
    /// </summary>
    /// <remarks>This service is responsible for creating, updating, and retrieving audit log entries related
    /// to permission changes. It supports logging changes for various permission types, including timeline item
    /// permissions, progeny permissions,  and family permissions. Each log entry includes details about the change,
    /// such as the action performed, the user  who made the change, and the state of the item before and after the
    /// change.</remarks>
    public interface IPermissionAuditLogsService
    {
        /// <summary>
        /// Retrieves a specific permission audit log entry by its unique identifier.
        /// </summary>
        /// <param name="permissionAuditLogId">The unique identifier of the permission audit log entry to retrieve.</param>
        /// <returns>A <see cref="PermissionAuditLog"/> object representing the requested audit log entry,  or <see
        /// langword="null"/> if no entry with the specified identifier exists.</returns>
        Task<PermissionAuditLog> GetPermissionAuditLogEntry(int permissionAuditLogId);

        /// <summary>
        /// Adds a new permission audit log entry to the database.
        /// </summary>
        /// <param name="logEntry">The permission audit log entry to add.</param>
        /// <returns>The added permission audit log entry.</returns>
        Task<PermissionAuditLog> AddPermissionAuditLogEntry(PermissionAuditLog logEntry);

        /// <summary>
        /// Updates an existing permission audit log entry with the current timestamp and saves the changes to the
        /// database.
        /// </summary>
        /// <remarks>This method updates the <see cref="PermissionAuditLog.ChangeTime"/> property to the
        /// current UTC time before saving the changes to the database. Ensure that the provided <paramref
        /// name="logEntry"/> represents a valid and existing database entry.</remarks>
        /// <param name="logEntry">The <see cref="PermissionAuditLog"/> object to update. The object must already exist in the database.</param>
        /// <returns>The updated <see cref="PermissionAuditLog"/> object.</returns>
        Task<PermissionAuditLog> UpdatePermissionAuditLogEntry(PermissionAuditLog logEntry);

        /// <summary>
        /// Adds a new permission audit log entry for a timeline item.
        /// </summary>
        /// <remarks>This method creates an audit log entry to record changes made to a timeline item's
        /// permissions.  The log entry includes details such as the action performed, the user who made the change, and
        /// the state of the item before the change.</remarks>
        /// <param name="action">The action performed on the timeline item permission. For example, add, update, or delete.</param>
        /// <param name="itemPermissionBefore">The state of the timeline item permission before the action was performed.</param>
        /// <param name="userInfo">Information about the user who performed the action.</param>
        /// <returns>A <see cref="PermissionAuditLog"/> object representing the newly created audit log entry.</returns>
        Task<PermissionAuditLog> AddTimelineItemPermissionAuditLogEntry(PermissionAction action, TimelineItemPermission itemPermissionBefore, UserInfo userInfo);

        /// <summary>
        /// Adds a new audit log entry for a change in progeny permissions.
        /// </summary>
        /// <remarks>This method creates an audit log entry to track changes made to progeny permissions.
        /// The log entry includes details  such as the type of action performed, the state of the permission before the
        /// change, and the user who made the change.  The log entry is saved to the database asynchronously.</remarks>
        /// <param name="action">The action performed on the progeny permission, such as Create, Update, or Delete.</param>
        /// <param name="progenyPermissionBefore">The state of the progeny permission before the change was made.</param>
        /// <param name="userInfo">Information about the user who performed the action, including their user ID or email.</param>
        /// <returns>A <see cref="PermissionAuditLog"/> object representing the newly created audit log entry.</returns>
        Task<PermissionAuditLog> AddProgenyPermissionAuditLogEntry(PermissionAction action, ProgenyPermission progenyPermissionBefore, UserInfo userInfo);

        /// <summary>
        /// Adds a new audit log entry for a family permission change.
        /// </summary>
        /// <remarks>This method creates an audit log entry to track changes made to family permissions.
        /// The log entry includes details  such as the family ID, permission ID, the type of permission, the action
        /// performed, the user who made the change,  and the timestamp of the change. The state of the permission
        /// before the change is serialized and stored in the log.</remarks>
        /// <param name="action">The action performed on the family permission, such as create, update, or delete.</param>
        /// <param name="familyPermissionBefore">The state of the family permission before the change occurred.</param>
        /// <param name="userInfo">Information about the user who performed the action, including their user ID or email.</param>
        /// <returns>A <see cref="PermissionAuditLog"/> object representing the newly created audit log entry.</returns>
        Task<PermissionAuditLog> AddFamilyPermissionAuditLogEntry(PermissionAction action, FamilyPermission familyPermissionBefore, UserInfo userInfo);
    }
}
