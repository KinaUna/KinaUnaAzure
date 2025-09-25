using System;
using System.ComponentModel.DataAnnotations;

namespace KinaUna.Data.Models.Family
{
    public class FamilyAuditLog
    {
        public int FamilyAuditLogId { get; set; }

        public int FamilyId { get; set; } = 0; // For Family only, not FamilyMember
        public int FamilyMemberId { get; set; } = 0; // For FamilyMember only
        
        public FamilyAction Action { get; set; } = FamilyAction.Unknown; // E.g., CreateFamily, UpdateFamily, DeleteFamily, AddFamilyMember, UpdateFamilyMember, RemoveFamilyMember

        [MaxLength(256)]
        public string EntityType { get; set; } = string.Empty; // E.g., "Family", "FamilyMember", etc.

        [MaxLength(8192)]
        public string EntityBefore { get; set; } = string.Empty; // JSON or text details of the item before the change

        [MaxLength(8192)]
        public string EntityAfter { get; set; } = string.Empty; // JSON or text details of the item after the change

        [MaxLength(256)]
        public string ChangedBy { get; set; }
        
        public DateTime ChangeTime { get; set; }
        
    }
}
