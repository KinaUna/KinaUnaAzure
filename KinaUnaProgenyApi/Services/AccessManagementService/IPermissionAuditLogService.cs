using KinaUna.Data.Models;
using KinaUna.Data.Models.AccessManagement;
using System.Threading.Tasks;

namespace KinaUnaProgenyApi.Services.AccessManagementService
{
    public interface IPermissionAuditLogService
    {
        Task<PermissionAuditLog> GetPermissionAuditLogEntry(int permissionAuditLogId);
        Task<PermissionAuditLog> AddPermissionAuditLogEntry(PermissionAuditLog logEntry);
        Task<PermissionAuditLog> UpdatePermissionAuditLogEntry(PermissionAuditLog logEntry);
        Task<PermissionAuditLog> AddTimelineItemPermissionAuditLogEntry(PermissionAction action, TimelineItemPermission itemPermissionBefore, UserInfo userInfo);
        Task<PermissionAuditLog> AddProgenyPermissionAuditLogEntry(PermissionAction action, ProgenyPermission progenyPermissionBefore, UserInfo userInfo);
        Task<PermissionAuditLog> AddFamilyPermissionAuditLogEntry(PermissionAction action, FamilyPermission familyPermissionBefore, UserInfo userInfo);
    }
}
