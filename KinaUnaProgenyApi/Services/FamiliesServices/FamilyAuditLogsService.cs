using System;
using System.Threading.Tasks;
using KinaUna.Data.Contexts;
using KinaUna.Data.Models;
using KinaUna.Data.Models.Family;

namespace KinaUnaProgenyApi.Services.FamiliesServices
{
    public class FamilyAuditLogsService(ProgenyDbContext progenyDbContext): IFamilyAuditLogsService
    {
        public async Task<FamilyAuditLog> AddFamilyAuditLogEntry(FamilyAuditLog logEntry)
        {
            await progenyDbContext.FamilyAuditLogsDb.AddAsync(logEntry);
            await progenyDbContext.SaveChangesAsync();

            return logEntry;
        }

        public async Task<FamilyAuditLog> UpdateFamilyAuditLogEntry(FamilyAuditLog logEntry)
        {
            progenyDbContext.FamilyAuditLogsDb.Update(logEntry);
            await progenyDbContext.SaveChangesAsync();
            return logEntry;
        }

        public async Task<FamilyAuditLog> AddFamilyCreatedAuditLogEntry(Family family, UserInfo userInfo)
        {
            FamilyAuditLog logEntry = new()
            {
                FamilyId = family.FamilyId,
                Action = FamilyAction.CreateFamily,
                EntityType = nameof(Family),
                EntityBefore = string.Empty,
                EntityAfter = System.Text.Json.JsonSerializer.Serialize(family),
                ChangedBy = userInfo.UserId,
                ChangeTime = DateTime.UtcNow
            };

            await progenyDbContext.FamilyAuditLogsDb.AddAsync(logEntry);
            await progenyDbContext.SaveChangesAsync();
            
            return logEntry;
        }

        public async Task<FamilyAuditLog> AddFamilyUpdatedAuditLogEntry(Family family, UserInfo userInfo)
        {
            FamilyAuditLog logEntry = new()
            {
                FamilyId = family.FamilyId,
                Action = FamilyAction.UpdateFamily,
                EntityType = nameof(Family),
                EntityBefore = System.Text.Json.JsonSerializer.Serialize(family),
                EntityAfter = string.Empty,
                ChangedBy = userInfo.UserId,
                ChangeTime = DateTime.UtcNow
            };

            await progenyDbContext.FamilyAuditLogsDb.AddAsync(logEntry);
            await progenyDbContext.SaveChangesAsync();

            return logEntry;
        }

        public async Task<FamilyAuditLog> AddFamilyDeletedAuditLogEntry(Family family, UserInfo userInfo)
        {
            FamilyAuditLog logEntry = new()
            {
                FamilyId = family.FamilyId,
                Action = FamilyAction.DeleteFamily,
                EntityType = nameof(Family),
                EntityBefore = System.Text.Json.JsonSerializer.Serialize(family),
                EntityAfter = string.Empty,
                ChangedBy = userInfo.UserId,
                ChangeTime = DateTime.UtcNow
            };

            await progenyDbContext.FamilyAuditLogsDb.AddAsync(logEntry);
            await progenyDbContext.SaveChangesAsync();

            return logEntry;
        }


        public async Task<FamilyAuditLog> AddFamilyMemberAddedAuditLogEntry(FamilyMember familyMember, UserInfo userInfo)
        {
            FamilyAuditLog logEntry = new()
            {
                FamilyMemberId = familyMember.FamilyMemberId,
                FamilyId = familyMember.FamilyId,
                Action = FamilyAction.AddFamilyMember,
                EntityType = nameof(FamilyMember),
                EntityBefore = string.Empty,
                EntityAfter = System.Text.Json.JsonSerializer.Serialize(familyMember),
                ChangedBy = userInfo.UserId,
                ChangeTime = DateTime.UtcNow
            };

            await progenyDbContext.FamilyAuditLogsDb.AddAsync(logEntry);
            await progenyDbContext.SaveChangesAsync();

            return logEntry;
        }

        public async Task<FamilyAuditLog> AddFamilyMemberUpdatedAuditLogEntry(FamilyMember familyMember, UserInfo userInfo)
        {
            FamilyAuditLog logEntry = new()
            {
                FamilyMemberId = familyMember.FamilyMemberId,
                FamilyId = familyMember.FamilyId,
                Action = FamilyAction.UpdateFamilyMember,
                EntityType = nameof(FamilyMember),
                EntityBefore = System.Text.Json.JsonSerializer.Serialize(familyMember),
                EntityAfter = string.Empty,
                ChangedBy = userInfo.UserId,
                ChangeTime = DateTime.UtcNow
            };

            await progenyDbContext.FamilyAuditLogsDb.AddAsync(logEntry);
            await progenyDbContext.SaveChangesAsync();

            return logEntry;
        }

        public async Task<FamilyAuditLog> AddFamilyMemberDeletedAuditLogEntry(FamilyMember familyMember, UserInfo userInfo)
        {
            FamilyAuditLog logEntry = new()
            {
                FamilyMemberId = familyMember.FamilyMemberId,
                FamilyId = familyMember.FamilyId,
                Action = FamilyAction.DeleteFamilyMember,
                EntityType = nameof(FamilyMember),
                EntityBefore = System.Text.Json.JsonSerializer.Serialize(familyMember),
                EntityAfter = string.Empty,
                ChangedBy = userInfo.UserId,
                ChangeTime = DateTime.UtcNow
            };

            await progenyDbContext.FamilyAuditLogsDb.AddAsync(logEntry);
            await progenyDbContext.SaveChangesAsync();

            return logEntry;
        }
    }
}
