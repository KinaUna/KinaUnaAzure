using Microsoft.EntityFrameworkCore.Migrations;

namespace KinaUna.OpenIddict.Migrations.ProgenyDb
{
    /// <inheritdoc />
    public partial class AddProgenyIdAndStartToRecurrence : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ProgenyId",
                table: "RecurrenceRulesDb",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "Start",
                table: "RecurrenceRulesDb",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ProgenyId",
                table: "RecurrenceRulesDb");

            migrationBuilder.DropColumn(
                name: "Start",
                table: "RecurrenceRulesDb");
        }
    }
}
