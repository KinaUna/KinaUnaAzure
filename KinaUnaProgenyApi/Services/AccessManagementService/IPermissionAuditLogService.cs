using KinaUna.Data.Models;
using KinaUna.Data.Models.AccessManagement;
using System.Threading.Tasks;

namespace KinaUnaProgenyApi.Services.AccessManagementService
{
    public interface IPermissionAuditLogService
    {
        Task<PermissionAuditLog> UpdatePermissionAuditLogEntry(PermissionAuditLog logEntry);
        Task<PermissionAuditLog> AddProgenyPermissionAuditLogEntry(PermissionAction action, ProgenyPermission progenyPermissionBefore, UserInfo userInfo);
    }
}
