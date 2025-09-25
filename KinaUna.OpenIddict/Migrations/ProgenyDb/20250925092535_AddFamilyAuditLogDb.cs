using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KinaUna.OpenIddict.Migrations.ProgenyDb
{
    /// <inheritdoc />
    public partial class AddFamilyAuditLogDb : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "FamilyAuditLogsDb",
                columns: table => new
                {
                    FamilyAuditLogId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    FamilyId = table.Column<int>(type: "int", nullable: false),
                    FamilyMemberId = table.Column<int>(type: "int", nullable: false),
                    Action = table.Column<int>(type: "int", nullable: false),
                    EntityType = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    EntityBefore = table.Column<string>(type: "nvarchar(max)", maxLength: 8192, nullable: true),
                    EntityAfter = table.Column<string>(type: "nvarchar(max)", maxLength: 8192, nullable: true),
                    ChangedBy = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    ChangeTime = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FamilyAuditLogsDb", x => x.FamilyAuditLogId);
                });

            migrationBuilder.CreateTable(
                name: "PermissionAuditLogsDb",
                columns: table => new
                {
                    PermissionAuditLogId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ProgenyId = table.Column<int>(type: "int", nullable: false),
                    FamilyId = table.Column<int>(type: "int", nullable: false),
                    ItemId = table.Column<int>(type: "int", nullable: false),
                    TimelineType = table.Column<int>(type: "int", nullable: true),
                    PermissionId = table.Column<int>(type: "int", nullable: false),
                    PermissionType = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    Action = table.Column<int>(type: "int", nullable: false),
                    ChangedBy = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    ChangeTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ItemBefore = table.Column<string>(type: "nvarchar(max)", maxLength: 8192, nullable: true),
                    ItemAfter = table.Column<string>(type: "nvarchar(max)", maxLength: 8192, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PermissionAuditLogsDb", x => x.PermissionAuditLogId);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "FamilyAuditLogsDb");

            migrationBuilder.DropTable(
                name: "PermissionAuditLogsDb");
        }
    }
}
