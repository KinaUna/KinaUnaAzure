using System.Text.Json;
using System.Threading.Tasks;
using KinaUna.Data.Contexts;
using KinaUna.Data.Models;
using KinaUna.Data.Models.AccessManagement;
using Microsoft.EntityFrameworkCore;

namespace KinaUnaProgenyApi.Services.AccessManagementService
{
    public class PermissionAuditLogService(ProgenyDbContext progenyDbContext): IPermissionAuditLogService
    {
        public async Task<PermissionAuditLog> AddPermissionAuditLogEntry(PermissionAuditLog logEntry)
        {
            progenyDbContext.PermissionAuditLogsDb.Add(logEntry);
            await progenyDbContext.SaveChangesAsync();
            return logEntry;
        }

        public async Task<PermissionAuditLog> UpdatePermissionAuditLogEntry(PermissionAuditLog logEntry)
        {
            progenyDbContext.PermissionAuditLogsDb.Update(logEntry);
            await progenyDbContext.SaveChangesAsync();
            return logEntry;
        }

        public async Task<PermissionAuditLog> GetPermissionAuditLogEntry(int permissionAuditLogId)
        {
            PermissionAuditLog? logEntry = await progenyDbContext.PermissionAuditLogsDb.AsNoTracking().SingleOrDefaultAsync(pau => pau.PermissionAuditLogId == permissionAuditLogId);
            return logEntry;
        }

        public async Task<PermissionAuditLog> AddTimelineItemPermissionAuditLogEntry(PermissionAction action, TimelineItemPermission itemPermissionBefore, UserInfo userInfo)
        {
            PermissionAuditLog logEntry = new PermissionAuditLog
            {
                EntityId = itemPermissionBefore.TimelineItemPermissionId,
                EntityType = nameof(TimelineItemPermission),
                Action = action,
                ChangedBy = !string.IsNullOrEmpty(userInfo.UserId) ? userInfo.UserId : userInfo.UserEmail,
                ChangeTime = System.DateTime.UtcNow,
                ItemBefore = JsonSerializer.Serialize(itemPermissionBefore),
                ItemAfter = string.Empty
            };

            progenyDbContext.PermissionAuditLogsDb.Add(logEntry);
            await progenyDbContext.SaveChangesAsync();

            return logEntry;
        }

        public async Task<PermissionAuditLog> AddProgenyPermissionAuditLogEntry(PermissionAction action, ProgenyPermission progenyPermissionBefore, UserInfo userInfo)
        {
            PermissionAuditLog logEntry = new PermissionAuditLog
            {
                EntityId = progenyPermissionBefore.ProgenyPermissionId,
                EntityType = nameof(ProgenyPermission),
                Action = action,
                ChangedBy = !string.IsNullOrEmpty(userInfo.UserId) ? userInfo.UserId : userInfo.UserEmail,
                ChangeTime = System.DateTime.UtcNow,
                ItemBefore = JsonSerializer.Serialize(progenyPermissionBefore),
                ItemAfter = string.Empty
            };

            progenyDbContext.PermissionAuditLogsDb.Add(logEntry);
            await progenyDbContext.SaveChangesAsync();
            
            return logEntry;
        }

        public async Task<PermissionAuditLog> AddFamilyPermissionAuditLogEntry(PermissionAction action, FamilyPermission familyPermissionBefore, UserInfo userInfo)
        {
            PermissionAuditLog logEntry = new PermissionAuditLog
            {
                EntityId = familyPermissionBefore.FamilyPermissionId,
                EntityType = nameof(FamilyPermission),
                Action = action,
                ChangedBy = !string.IsNullOrEmpty(userInfo.UserId) ? userInfo.UserId : userInfo.UserEmail,
                ChangeTime = System.DateTime.UtcNow,
                ItemBefore = JsonSerializer.Serialize(familyPermissionBefore),
                ItemAfter = string.Empty
            };

            progenyDbContext.PermissionAuditLogsDb.Add(logEntry);
            await progenyDbContext.SaveChangesAsync();

            return logEntry;
        }

    }
}
