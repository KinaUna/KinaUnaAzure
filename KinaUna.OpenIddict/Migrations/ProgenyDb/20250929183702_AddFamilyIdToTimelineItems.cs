using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KinaUna.OpenIddict.Migrations.ProgenyDb
{
    /// <inheritdoc />
    public partial class AddFamilyIdToTimelineItems : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CreatedBy",
                table: "VocabularyDb",
                type: "nvarchar(256)",
                maxLength: 256,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedTime",
                table: "VocabularyDb",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<string>(
                name: "ModifiedBy",
                table: "VocabularyDb",
                type: "nvarchar(256)",
                maxLength: 256,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ModifiedTime",
                table: "VocabularyDb",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<string>(
                name: "CreatedBy",
                table: "VaccinationsDb",
                type: "nvarchar(256)",
                maxLength: 256,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedTime",
                table: "VaccinationsDb",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<string>(
                name: "ModifiedBy",
                table: "VaccinationsDb",
                type: "nvarchar(256)",
                maxLength: 256,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ModifiedTime",
                table: "VaccinationsDb",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<int>(
                name: "FamilyId",
                table: "TodoItemsDb",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AlterColumn<string>(
                name: "ItemId",
                table: "TimeLineDb",
                type: "nvarchar(256)",
                maxLength: 256,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AddColumn<int>(
                name: "FamilyId",
                table: "TimeLineDb",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "ModifiedBy",
                table: "TimeLineDb",
                type: "nvarchar(256)",
                maxLength: 256,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ModifiedTime",
                table: "TimeLineDb",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<string>(
                name: "CreatedBy",
                table: "SleepDb",
                type: "nvarchar(256)",
                maxLength: 256,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedTime",
                table: "SleepDb",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<string>(
                name: "ModifiedBy",
                table: "SleepDb",
                type: "nvarchar(256)",
                maxLength: 256,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ModifiedTime",
                table: "SleepDb",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<string>(
                name: "CreatedBy",
                table: "SkillsDb",
                type: "nvarchar(256)",
                maxLength: 256,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedTime",
                table: "SkillsDb",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<string>(
                name: "ModifiedBy",
                table: "SkillsDb",
                type: "nvarchar(256)",
                maxLength: 256,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ModifiedTime",
                table: "SkillsDb",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<string>(
                name: "ModifiedBy",
                table: "ProgenyInfoDb",
                type: "nvarchar(256)",
                maxLength: 256,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ModifiedTime",
                table: "ProgenyInfoDb",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<string>(
                name: "CreatedBy",
                table: "ProgenyDb",
                type: "nvarchar(256)",
                maxLength: 256,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedTime",
                table: "ProgenyDb",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<string>(
                name: "Email",
                table: "ProgenyDb",
                type: "nvarchar(1024)",
                maxLength: 1024,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ModifiedBy",
                table: "ProgenyDb",
                type: "nvarchar(256)",
                maxLength: 256,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ModifiedTime",
                table: "ProgenyDb",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<string>(
                name: "UserId",
                table: "ProgenyDb",
                type: "nvarchar(256)",
                maxLength: 256,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CreatedBy",
                table: "NotesDb",
                type: "nvarchar(256)",
                maxLength: 256,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedTime",
                table: "NotesDb",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<string>(
                name: "ModifiedBy",
                table: "NotesDb",
                type: "nvarchar(256)",
                maxLength: 256,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ModifiedTime",
                table: "NotesDb",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<string>(
                name: "CreatedBy",
                table: "MeasurementsDb",
                type: "nvarchar(256)",
                maxLength: 256,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedTime",
                table: "MeasurementsDb",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<string>(
                name: "ModifiedBy",
                table: "MeasurementsDb",
                type: "nvarchar(256)",
                maxLength: 256,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ModifiedTime",
                table: "MeasurementsDb",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<string>(
                name: "CreatedBy",
                table: "LocationsDb",
                type: "nvarchar(256)",
                maxLength: 256,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedTime",
                table: "LocationsDb",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<int>(
                name: "FamilyId",
                table: "LocationsDb",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "ModifiedBy",
                table: "LocationsDb",
                type: "nvarchar(256)",
                maxLength: 256,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ModifiedTime",
                table: "LocationsDb",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<int>(
                name: "FamilyId",
                table: "KanbanBoardsDb",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "CreatedBy",
                table: "FriendsDb",
                type: "nvarchar(256)",
                maxLength: 256,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedTime",
                table: "FriendsDb",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<string>(
                name: "ModifiedBy",
                table: "FriendsDb",
                type: "nvarchar(256)",
                maxLength: 256,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ModifiedTime",
                table: "FriendsDb",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AlterColumn<string>(
                name: "Notes",
                table: "ContactsDb",
                type: "nvarchar(max)",
                maxLength: 4096,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(256)",
                oldMaxLength: 256,
                oldNullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CreatedBy",
                table: "ContactsDb",
                type: "nvarchar(256)",
                maxLength: 256,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedTime",
                table: "ContactsDb",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<int>(
                name: "FamilyId",
                table: "ContactsDb",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "ModifiedBy",
                table: "ContactsDb",
                type: "nvarchar(256)",
                maxLength: 256,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ModifiedTime",
                table: "ContactsDb",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<string>(
                name: "CreatedBy",
                table: "CalendarDb",
                type: "nvarchar(256)",
                maxLength: 256,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedTime",
                table: "CalendarDb",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<int>(
                name: "FamilyId",
                table: "CalendarDb",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "ModifiedBy",
                table: "CalendarDb",
                type: "nvarchar(256)",
                maxLength: 256,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ModifiedTime",
                table: "CalendarDb",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CreatedBy",
                table: "VocabularyDb");

            migrationBuilder.DropColumn(
                name: "CreatedTime",
                table: "VocabularyDb");

            migrationBuilder.DropColumn(
                name: "ModifiedBy",
                table: "VocabularyDb");

            migrationBuilder.DropColumn(
                name: "ModifiedTime",
                table: "VocabularyDb");

            migrationBuilder.DropColumn(
                name: "CreatedBy",
                table: "VaccinationsDb");

            migrationBuilder.DropColumn(
                name: "CreatedTime",
                table: "VaccinationsDb");

            migrationBuilder.DropColumn(
                name: "ModifiedBy",
                table: "VaccinationsDb");

            migrationBuilder.DropColumn(
                name: "ModifiedTime",
                table: "VaccinationsDb");

            migrationBuilder.DropColumn(
                name: "FamilyId",
                table: "TodoItemsDb");

            migrationBuilder.DropColumn(
                name: "FamilyId",
                table: "TimeLineDb");

            migrationBuilder.DropColumn(
                name: "ModifiedBy",
                table: "TimeLineDb");

            migrationBuilder.DropColumn(
                name: "ModifiedTime",
                table: "TimeLineDb");

            migrationBuilder.DropColumn(
                name: "CreatedBy",
                table: "SleepDb");

            migrationBuilder.DropColumn(
                name: "CreatedTime",
                table: "SleepDb");

            migrationBuilder.DropColumn(
                name: "ModifiedBy",
                table: "SleepDb");

            migrationBuilder.DropColumn(
                name: "ModifiedTime",
                table: "SleepDb");

            migrationBuilder.DropColumn(
                name: "CreatedBy",
                table: "SkillsDb");

            migrationBuilder.DropColumn(
                name: "CreatedTime",
                table: "SkillsDb");

            migrationBuilder.DropColumn(
                name: "ModifiedBy",
                table: "SkillsDb");

            migrationBuilder.DropColumn(
                name: "ModifiedTime",
                table: "SkillsDb");

            migrationBuilder.DropColumn(
                name: "ModifiedBy",
                table: "ProgenyInfoDb");

            migrationBuilder.DropColumn(
                name: "ModifiedTime",
                table: "ProgenyInfoDb");

            migrationBuilder.DropColumn(
                name: "CreatedBy",
                table: "ProgenyDb");

            migrationBuilder.DropColumn(
                name: "CreatedTime",
                table: "ProgenyDb");

            migrationBuilder.DropColumn(
                name: "Email",
                table: "ProgenyDb");

            migrationBuilder.DropColumn(
                name: "ModifiedBy",
                table: "ProgenyDb");

            migrationBuilder.DropColumn(
                name: "ModifiedTime",
                table: "ProgenyDb");

            migrationBuilder.DropColumn(
                name: "UserId",
                table: "ProgenyDb");

            migrationBuilder.DropColumn(
                name: "CreatedBy",
                table: "NotesDb");

            migrationBuilder.DropColumn(
                name: "CreatedTime",
                table: "NotesDb");

            migrationBuilder.DropColumn(
                name: "ModifiedBy",
                table: "NotesDb");

            migrationBuilder.DropColumn(
                name: "ModifiedTime",
                table: "NotesDb");

            migrationBuilder.DropColumn(
                name: "CreatedBy",
                table: "MeasurementsDb");

            migrationBuilder.DropColumn(
                name: "CreatedTime",
                table: "MeasurementsDb");

            migrationBuilder.DropColumn(
                name: "ModifiedBy",
                table: "MeasurementsDb");

            migrationBuilder.DropColumn(
                name: "ModifiedTime",
                table: "MeasurementsDb");

            migrationBuilder.DropColumn(
                name: "CreatedBy",
                table: "LocationsDb");

            migrationBuilder.DropColumn(
                name: "CreatedTime",
                table: "LocationsDb");

            migrationBuilder.DropColumn(
                name: "FamilyId",
                table: "LocationsDb");

            migrationBuilder.DropColumn(
                name: "ModifiedBy",
                table: "LocationsDb");

            migrationBuilder.DropColumn(
                name: "ModifiedTime",
                table: "LocationsDb");

            migrationBuilder.DropColumn(
                name: "FamilyId",
                table: "KanbanBoardsDb");

            migrationBuilder.DropColumn(
                name: "CreatedBy",
                table: "FriendsDb");

            migrationBuilder.DropColumn(
                name: "CreatedTime",
                table: "FriendsDb");

            migrationBuilder.DropColumn(
                name: "ModifiedBy",
                table: "FriendsDb");

            migrationBuilder.DropColumn(
                name: "ModifiedTime",
                table: "FriendsDb");

            migrationBuilder.DropColumn(
                name: "CreatedBy",
                table: "ContactsDb");

            migrationBuilder.DropColumn(
                name: "CreatedTime",
                table: "ContactsDb");

            migrationBuilder.DropColumn(
                name: "FamilyId",
                table: "ContactsDb");

            migrationBuilder.DropColumn(
                name: "ModifiedBy",
                table: "ContactsDb");

            migrationBuilder.DropColumn(
                name: "ModifiedTime",
                table: "ContactsDb");

            migrationBuilder.DropColumn(
                name: "CreatedBy",
                table: "CalendarDb");

            migrationBuilder.DropColumn(
                name: "CreatedTime",
                table: "CalendarDb");

            migrationBuilder.DropColumn(
                name: "FamilyId",
                table: "CalendarDb");

            migrationBuilder.DropColumn(
                name: "ModifiedBy",
                table: "CalendarDb");

            migrationBuilder.DropColumn(
                name: "ModifiedTime",
                table: "CalendarDb");

            migrationBuilder.AlterColumn<string>(
                name: "ItemId",
                table: "TimeLineDb",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(256)",
                oldMaxLength: 256,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Notes",
                table: "ContactsDb",
                type: "nvarchar(256)",
                maxLength: 256,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldMaxLength: 4096,
                oldNullable: true);
        }
    }
}
