using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KinaUna.OpenIddict.Migrations.ProgenyDb
{
    /// <inheritdoc />
    public partial class KanbanBoardAddIsDeletedTagsContext : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "KanbanItemsDb",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "Context",
                table: "KanbanBoardsDb",
                type: "nvarchar(256)",
                maxLength: 256,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "KanbanBoardsDb",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "Tags",
                table: "KanbanBoardsDb",
                type: "nvarchar(256)",
                maxLength: 256,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "KanbanItemsDb");

            migrationBuilder.DropColumn(
                name: "Context",
                table: "KanbanBoardsDb");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "KanbanBoardsDb");

            migrationBuilder.DropColumn(
                name: "Tags",
                table: "KanbanBoardsDb");
        }
    }
}
