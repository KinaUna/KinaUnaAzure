using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KinaUna.OpenIddict.Migrations.ProgenyDb
{
    /// <inheritdoc />
    public partial class AddInheritToTimelineItemPermission : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "InheritPermissions",
                table: "TimelineItemPermissionsDb",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "InheritPermissions",
                table: "TimelineItemPermissionsDb");
        }
    }
}
