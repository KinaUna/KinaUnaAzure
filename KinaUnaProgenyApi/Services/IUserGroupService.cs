using System.Threading.Tasks;
using KinaUna.Data.Models;

namespace KinaUnaProgenyApi.Services
{
    public interface IUserGroupService
    {
        Task<UserGroup> GetUserGroup(int groupId);
        Task<UserGroup> AddUserGroup(UserGroup userGroup);
        Task<UserGroup> UpdateUserGroup(UserGroup userGroup);
        Task<bool> RemoveUserGroup(int groupId);
        Task<UserGroupMember> AddUserGroupMember(UserGroupMember member);
        Task<UserGroupMember> UpdateUserGroupMember(UserGroupMember member);
        Task<bool> RemoveUserGroupMember(int memberId);
    }
}
