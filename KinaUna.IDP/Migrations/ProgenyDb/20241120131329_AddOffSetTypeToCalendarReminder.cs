using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace KinaUna.IDP.Migrations.ProgenyDb
{
    /// <inheritdoc />
    public partial class AddOffSetTypeToCalendarReminder : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "NotifiedDate",
                table: "CalendarRemindersDb",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<int>(
                name: "NotifyTimeOffsetType",
                table: "CalendarRemindersDb",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "RecurrenceRuleId",
                table: "CalendarRemindersDb",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "NotifiedDate",
                table: "CalendarRemindersDb");

            migrationBuilder.DropColumn(
                name: "NotifyTimeOffsetType",
                table: "CalendarRemindersDb");

            migrationBuilder.DropColumn(
                name: "RecurrenceRuleId",
                table: "CalendarRemindersDb");
        }
    }
}
