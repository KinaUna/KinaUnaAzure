using Microsoft.EntityFrameworkCore.Migrations;

namespace KinaUna.OpenIddict.Migrations.ProgenyDb
{
    /// <inheritdoc />
    public partial class AddRecurrenceToCalendarItem : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "RecurrenceRuleId",
                table: "CalendarDb",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "RecurrenceRulesDb",
                columns: table => new
                {
                    RecurrenceRuleId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Frequency = table.Column<int>(type: "int", nullable: false),
                    Interval = table.Column<int>(type: "int", nullable: false),
                    Count = table.Column<int>(type: "int", nullable: false),
                    Until = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ByDay = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ByMonthDay = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ByMonth = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RecurrenceRulesDb", x => x.RecurrenceRuleId);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "RecurrenceRulesDb");

            migrationBuilder.DropColumn(
                name: "RecurrenceRuleId",
                table: "CalendarDb");
        }
    }
}
