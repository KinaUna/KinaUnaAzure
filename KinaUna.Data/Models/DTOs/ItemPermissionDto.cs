using KinaUna.Data.Models.AccessManagement;

namespace KinaUna.Data.Models.DTOs
{
    public class ItemPermissionDto
    {
        public int ItemPermissionId { get; set; } = 0;
        public int ProgenyPermissionId { get; set; } = 0;
        public int FamilyPermissionId { get; set; } = 0;
        public bool InheritPermissions { get; set; } = true;
        public PermissionLevel PermissionLevel { get; set; } = PermissionLevel.None;
    }
}
