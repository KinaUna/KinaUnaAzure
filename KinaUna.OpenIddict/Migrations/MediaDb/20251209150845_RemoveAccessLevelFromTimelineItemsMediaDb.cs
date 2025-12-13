using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KinaUna.OpenIddict.Migrations.MediaDb
{
    /// <inheritdoc />
    public partial class RemoveAccessLevelFromTimelineItemsMediaDb : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AccessLevel",
                table: "VideoDb");

            migrationBuilder.DropColumn(
                name: "AccessLevel",
                table: "PicturesDb");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "AccessLevel",
                table: "VideoDb",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "AccessLevel",
                table: "PicturesDb",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }
    }
}
