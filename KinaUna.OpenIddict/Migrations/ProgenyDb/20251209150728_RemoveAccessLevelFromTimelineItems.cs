using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KinaUna.OpenIddict.Migrations.ProgenyDb
{
    /// <inheritdoc />
    public partial class RemoveAccessLevelFromTimelineItems : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "UserAccessDb");

            migrationBuilder.DropColumn(
                name: "AccessLevel",
                table: "VocabularyDb");

            migrationBuilder.DropColumn(
                name: "AccessLevel",
                table: "VaccinationsDb");

            migrationBuilder.DropColumn(
                name: "AccessLevel",
                table: "TodoItemsDb");

            migrationBuilder.DropColumn(
                name: "AccessLevel",
                table: "TimeLineDb");

            migrationBuilder.DropColumn(
                name: "AccessLevel",
                table: "SleepDb");

            migrationBuilder.DropColumn(
                name: "AccessLevel",
                table: "SkillsDb");

            migrationBuilder.DropColumn(
                name: "AccessLevel",
                table: "NotesDb");

            migrationBuilder.DropColumn(
                name: "AccessLevel",
                table: "MeasurementsDb");

            migrationBuilder.DropColumn(
                name: "AccessLevel",
                table: "LocationsDb");

            migrationBuilder.DropColumn(
                name: "AccessLevel",
                table: "KanbanBoardsDb");

            migrationBuilder.DropColumn(
                name: "AccessLevel",
                table: "FriendsDb");

            migrationBuilder.DropColumn(
                name: "AccessLevel",
                table: "ContactsDb");

            migrationBuilder.DropColumn(
                name: "AccessLevel",
                table: "CalendarDb");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "AccessLevel",
                table: "VocabularyDb",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "AccessLevel",
                table: "VaccinationsDb",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "AccessLevel",
                table: "TodoItemsDb",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "AccessLevel",
                table: "TimeLineDb",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "AccessLevel",
                table: "SleepDb",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "AccessLevel",
                table: "SkillsDb",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "AccessLevel",
                table: "NotesDb",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "AccessLevel",
                table: "MeasurementsDb",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "AccessLevel",
                table: "LocationsDb",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "AccessLevel",
                table: "KanbanBoardsDb",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "AccessLevel",
                table: "FriendsDb",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "AccessLevel",
                table: "ContactsDb",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "AccessLevel",
                table: "CalendarDb",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "UserAccessDb",
                columns: table => new
                {
                    AccessId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AccessLevel = table.Column<int>(type: "int", nullable: false),
                    CanContribute = table.Column<bool>(type: "bit", nullable: false),
                    ProgenyId = table.Column<int>(type: "int", nullable: false),
                    UserId = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserAccessDb", x => x.AccessId);
                });
        }
    }
}
