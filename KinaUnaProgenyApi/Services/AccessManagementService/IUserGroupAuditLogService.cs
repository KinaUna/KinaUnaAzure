using System.Threading.Tasks;
using KinaUna.Data.Models;
using KinaUna.Data.Models.AccessManagement;

namespace KinaUnaProgenyApi.Services.AccessManagementService
{
    public interface IUserGroupAuditLogService
    {
        Task<UserGroupAuditLog> GetUserGroupAuditLogEntry(int userGroupAuditLogId);
        Task<UserGroupAuditLog> AddUserGroupAuditLogEntry(UserGroupAuditLog logEntry);
        Task<UserGroupAuditLog> UpdateUserGroupAuditLogEntry(UserGroupAuditLog logEntry);
        Task<UserGroupAuditLog> AddUserGroupCreatedAuditLogEntry(UserGroup userGroup, UserInfo userInfo);
        Task<UserGroupAuditLog> AddUserGroupUpdatedAuditLogEntry(UserGroup userGroup, UserInfo userInfo);
        Task<UserGroupAuditLog> AddUserGroupDeletedAuditLogEntry(UserGroup userGroup, UserInfo userInfo);
        Task<UserGroupAuditLog> AddUserGroupMemberAddedAuditLogEntry(UserGroupMember userGroupMember, UserInfo userInfo);
        Task<UserGroupAuditLog> AddUserGroupMemberUpdatedAuditLogEntry(UserGroupMember userGroupMember, UserInfo userInfo);
        Task<UserGroupAuditLog> AddUserGroupMemberDeletedAuditLogEntry(UserGroupMember userGroupMember, UserInfo userInfo);
    }
}
