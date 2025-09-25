using System.Threading.Tasks;
using KinaUna.Data.Models;
using KinaUna.Data.Models.Family;

namespace KinaUnaProgenyApi.Services.FamiliesServices
{
    public interface IFamilyAuditLogsService
    {
        Task<FamilyAuditLog> AddFamilyAuditLogEntry(FamilyAuditLog logEntry);
        Task<FamilyAuditLog> UpdateFamilyAuditLogEntry(FamilyAuditLog logEntry);
        Task<FamilyAuditLog> AddFamilyCreatedAuditLogEntry(Family family, UserInfo userInfo);
        Task<FamilyAuditLog> AddFamilyUpdatedAuditLogEntry(Family family, UserInfo userInfo);
        Task<FamilyAuditLog> AddFamilyDeletedAuditLogEntry(Family family, UserInfo userInfo);
        Task<FamilyAuditLog> AddFamilyMemberAddedAuditLogEntry(FamilyMember familyMember, UserInfo userInfo);
        Task<FamilyAuditLog> AddFamilyMemberUpdatedAuditLogEntry(FamilyMember familyMember, UserInfo userInfo);
        Task<FamilyAuditLog> AddFamilyMemberDeletedAuditLogEntry(FamilyMember familyMember, UserInfo userInfo);
    }
}
