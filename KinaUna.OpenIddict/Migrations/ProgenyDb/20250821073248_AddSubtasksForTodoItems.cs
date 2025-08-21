using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KinaUna.OpenIddict.Migrations.ProgenyDb
{
    /// <inheritdoc />
    public partial class AddSubtasksForTodoItems : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_TodoItemsDb",
                table: "TodoItemsDb");

            migrationBuilder.RenameTable(
                name: "TodoItemsDb",
                newName: "TodoItem");

            migrationBuilder.AddColumn<int>(
                name: "ParentTodoItemId",
                table: "TodoItem",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddPrimaryKey(
                name: "PK_TodoItem",
                table: "TodoItem",
                column: "TodoItemId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_TodoItem",
                table: "TodoItem");

            migrationBuilder.DropColumn(
                name: "ParentTodoItemId",
                table: "TodoItem");

            migrationBuilder.RenameTable(
                name: "TodoItem",
                newName: "TodoItemsDb");

            migrationBuilder.AddPrimaryKey(
                name: "PK_TodoItemsDb",
                table: "TodoItemsDb",
                column: "TodoItemId");
        }
    }
}
