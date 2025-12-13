using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KinaUna.OpenIddict.Migrations.ProgenyDb
{
    /// <inheritdoc />
    public partial class GroupOnlyProgenyPermissions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Email",
                table: "ProgenyPermissionsDb");

            migrationBuilder.DropColumn(
                name: "UserId",
                table: "ProgenyPermissionsDb");

            migrationBuilder.DropColumn(
                name: "Email",
                table: "FamilyPermissionsDb");

            migrationBuilder.DropColumn(
                name: "UserId",
                table: "FamilyPermissionsDb");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Email",
                table: "ProgenyPermissionsDb",
                type: "nvarchar(256)",
                maxLength: 256,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "UserId",
                table: "ProgenyPermissionsDb",
                type: "nvarchar(256)",
                maxLength: 256,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Email",
                table: "FamilyPermissionsDb",
                type: "nvarchar(256)",
                maxLength: 256,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "UserId",
                table: "FamilyPermissionsDb",
                type: "nvarchar(256)",
                maxLength: 256,
                nullable: true);
        }
    }
}
