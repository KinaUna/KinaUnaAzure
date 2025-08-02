using Microsoft.EntityFrameworkCore.Migrations;

namespace KinaUna.OpenIddict.Migrations.ProgenyDb
{
    /// <inheritdoc />
    public partial class AddCalendarReminders : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CalendarRemindersDb",
                columns: table => new
                {
                    CalendarReminderId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    EventId = table.Column<int>(type: "int", nullable: false),
                    UserId = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    NotifyTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Notified = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CalendarRemindersDb", x => x.CalendarReminderId);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CalendarRemindersDb");
        }
    }
}
