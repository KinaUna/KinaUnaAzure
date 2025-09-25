using System;
using System.Threading.Tasks;
using KinaUna.Data.Contexts;
using KinaUna.Data.Models;
using KinaUna.Data.Models.AccessManagement;
using Microsoft.EntityFrameworkCore;

namespace KinaUnaProgenyApi.Services.AccessManagementService
{
    public class UserGroupAuditLogService(ProgenyDbContext progenyDbContext): IUserGroupAuditLogService
    {
        public async Task<UserGroupAuditLog> GetUserGroupAuditLogEntry(int logEntryId)
        {
            return await progenyDbContext.UserGroupAuditLogsDb.SingleOrDefaultAsync(u => u.UserGroupAuditLogId == logEntryId);
        }

        public async Task<UserGroupAuditLog> AddUserGroupAuditLogEntry(UserGroupAuditLog logEntry)
        {
            await progenyDbContext.UserGroupAuditLogsDb.AddAsync(logEntry);
            await progenyDbContext.SaveChangesAsync();

            return logEntry;
        }
        public async Task<UserGroupAuditLog> UpdateUserGroupAuditLogEntry(UserGroupAuditLog logEntry)
        {
            progenyDbContext.UserGroupAuditLogsDb.Update(logEntry);
            await progenyDbContext.SaveChangesAsync();

            return logEntry;
        }

        public async Task<UserGroupAuditLog> AddUserGroupCreatedAuditLogEntry(UserGroup userGroup, UserInfo userInfo)
        {
            UserGroupAuditLog logEntry = new()
            {
                UserGroupId = userGroup.UserGroupId,
                Action = UserGroupAction.CreateGroup ,
                EntityType = nameof(UserGroup),
                EntityBefore = string.Empty,
                EntityAfter = System.Text.Json.JsonSerializer.Serialize(userGroup),
                ChangedBy = userInfo.UserId,
                ChangeTime = DateTime.UtcNow
            };
            
            await progenyDbContext.UserGroupAuditLogsDb.AddAsync(logEntry);
            await progenyDbContext.SaveChangesAsync();

            return logEntry;
        }

        public async Task<UserGroupAuditLog> AddUserGroupUpdatedAuditLogEntry(UserGroup userGroup, UserInfo userInfo)
        {
            UserGroupAuditLog logEntry = new()
            {
                UserGroupId = userGroup.UserGroupId,
                Action = UserGroupAction.UpdateGroup,
                EntityType = nameof(UserGroup),
                EntityBefore = System.Text.Json.JsonSerializer.Serialize(userGroup),
                EntityAfter = string.Empty,
                ChangedBy = userInfo.UserId,
                ChangeTime = DateTime.UtcNow
            };
            await progenyDbContext.UserGroupAuditLogsDb.AddAsync(logEntry);
            await progenyDbContext.SaveChangesAsync();

            return logEntry;
        }

        public async Task<UserGroupAuditLog> AddUserGroupDeletedAuditLogEntry(UserGroup userGroup, UserInfo userInfo)
        {
            UserGroupAuditLog logEntry = new()
            {
                UserGroupId = userGroup.UserGroupId,
                Action = UserGroupAction.DeleteGroup,
                EntityType = nameof(UserGroup),
                EntityBefore = System.Text.Json.JsonSerializer.Serialize(userGroup),
                EntityAfter = string.Empty,
                ChangedBy = userInfo.UserId,
                ChangeTime = DateTime.UtcNow
            };
            await progenyDbContext.UserGroupAuditLogsDb.AddAsync(logEntry);
            await progenyDbContext.SaveChangesAsync();

            return logEntry;
        }

        public async Task<UserGroupAuditLog> AddUserGroupMemberAddedAuditLogEntry(UserGroupMember userGroupMember, UserInfo userInfo)
        {
            UserGroupAuditLog logEntry = new()
            {
                UserGroupId = userGroupMember.UserGroupId,
                UserGroupMemberId = userGroupMember.UserGroupMemberId,
                Action = UserGroupAction.AddGroupMember,
                EntityType = nameof(UserGroupMember),
                EntityBefore = string.Empty,
                EntityAfter = System.Text.Json.JsonSerializer.Serialize(userGroupMember),
                ChangedBy = userInfo.UserId,
                ChangeTime = DateTime.UtcNow
            };
            await progenyDbContext.UserGroupAuditLogsDb.AddAsync(logEntry);
            await progenyDbContext.SaveChangesAsync();

            return logEntry;
        }

        public async Task<UserGroupAuditLog> AddUserGroupMemberUpdatedAuditLogEntry(UserGroupMember userGroupMember, UserInfo userInfo)
        {
            UserGroupAuditLog logEntry = new()
            {
                UserGroupId = userGroupMember.UserGroupId,
                UserGroupMemberId = userGroupMember.UserGroupMemberId,
                Action = UserGroupAction.UpdateGroupMember,
                EntityType = nameof(UserGroupMember),
                EntityBefore = System.Text.Json.JsonSerializer.Serialize(userGroupMember),
                EntityAfter = string.Empty,
                ChangedBy = userInfo.UserId,
                ChangeTime = DateTime.UtcNow
            };
            await progenyDbContext.UserGroupAuditLogsDb.AddAsync(logEntry);
            await progenyDbContext.SaveChangesAsync();

            return logEntry;
        }

        public async Task<UserGroupAuditLog> AddUserGroupMemberDeletedAuditLogEntry(UserGroupMember userGroupMember, UserInfo userInfo)
        {
            UserGroupAuditLog logEntry = new()
            {
                UserGroupId = userGroupMember.UserGroupId,
                UserGroupMemberId = userGroupMember.UserGroupMemberId,
                Action = UserGroupAction.RemoveGroupMember,
                EntityType = nameof(UserGroupMember),
                EntityBefore = System.Text.Json.JsonSerializer.Serialize(userGroupMember),
                EntityAfter = string.Empty,
                ChangedBy = userInfo.UserId,
                ChangeTime = DateTime.UtcNow
            };
            await progenyDbContext.UserGroupAuditLogsDb.AddAsync(logEntry);
            await progenyDbContext.SaveChangesAsync();

            return logEntry;
        }
    }
}
