using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

namespace KinaUnaProgenyApi.Migrations
{
    public partial class InitialIdentityMigration : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Id",
                table: "UserAccessDb",
                newName: "AccessId");

            migrationBuilder.CreateTable(
                name: "AddressDb",
                columns: table => new
                {
                    AddressId = table.Column<int>(nullable: false)
                        .Annotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn),
                    AddressLine1 = table.Column<string>(nullable: true),
                    AddressLine2 = table.Column<string>(nullable: true),
                    City = table.Column<string>(nullable: true),
                    State = table.Column<string>(nullable: true),
                    PostalCode = table.Column<string>(nullable: true),
                    Country = table.Column<string>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AddressDb", x => x.AddressId);
                });

            migrationBuilder.CreateTable(
                name: "CalendarDb",
                columns: table => new
                {
                    EventId = table.Column<int>(nullable: false)
                        .Annotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn),
                    ProgenyId = table.Column<int>(nullable: false),
                    Title = table.Column<string>(nullable: true),
                    Notes = table.Column<string>(nullable: true),
                    StartTime = table.Column<DateTime>(nullable: true),
                    EndTime = table.Column<DateTime>(nullable: true),
                    Location = table.Column<string>(nullable: true),
                    Context = table.Column<string>(nullable: true),
                    AllDay = table.Column<bool>(nullable: false),
                    AccessLevel = table.Column<int>(nullable: false),
                    StartString = table.Column<string>(nullable: true),
                    EndString = table.Column<string>(nullable: true),
                    Author = table.Column<string>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CalendarDb", x => x.EventId);
                });

            migrationBuilder.CreateTable(
                name: "ContactsDb",
                columns: table => new
                {
                    ContactId = table.Column<int>(nullable: false)
                        .Annotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn),
                    Active = table.Column<bool>(nullable: false),
                    FirstName = table.Column<string>(nullable: true),
                    MiddleName = table.Column<string>(nullable: true),
                    LastName = table.Column<string>(nullable: true),
                    DisplayName = table.Column<string>(nullable: true),
                    AddressIdNumber = table.Column<int>(nullable: true),
                    Email1 = table.Column<string>(nullable: true),
                    Email2 = table.Column<string>(nullable: true),
                    PhoneNumber = table.Column<string>(nullable: true),
                    MobileNumber = table.Column<string>(nullable: true),
                    Context = table.Column<string>(nullable: true),
                    Notes = table.Column<string>(nullable: true),
                    PictureLink = table.Column<string>(nullable: true),
                    Website = table.Column<string>(nullable: true),
                    AccessLevel = table.Column<int>(nullable: false),
                    ProgenyId = table.Column<int>(nullable: false),
                    Tags = table.Column<string>(nullable: true),
                    DateAdded = table.Column<DateTime>(nullable: true),
                    Author = table.Column<string>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ContactsDb", x => x.ContactId);
                });

            migrationBuilder.CreateTable(
                name: "FriendsDb",
                columns: table => new
                {
                    FriendId = table.Column<int>(nullable: false)
                        .Annotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn),
                    Name = table.Column<string>(nullable: true),
                    Description = table.Column<string>(nullable: true),
                    FriendSince = table.Column<DateTime>(nullable: true),
                    FriendAddedDate = table.Column<DateTime>(nullable: false),
                    PictureLink = table.Column<string>(nullable: true),
                    ProgenyId = table.Column<int>(nullable: false),
                    Type = table.Column<int>(nullable: false),
                    Context = table.Column<string>(nullable: true),
                    Notes = table.Column<string>(nullable: true),
                    Tags = table.Column<string>(nullable: true),
                    AccessLevel = table.Column<int>(nullable: false),
                    Author = table.Column<string>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FriendsDb", x => x.FriendId);
                });

            migrationBuilder.CreateTable(
                name: "MeasurementsDb",
                columns: table => new
                {
                    MeasurementId = table.Column<int>(nullable: false)
                        .Annotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn),
                    ProgenyId = table.Column<int>(nullable: false),
                    Weight = table.Column<double>(nullable: false),
                    Height = table.Column<double>(nullable: false),
                    Circumference = table.Column<double>(nullable: false),
                    EyeColor = table.Column<string>(nullable: true),
                    HairColor = table.Column<string>(nullable: true),
                    Date = table.Column<DateTime>(nullable: false),
                    CreatedDate = table.Column<DateTime>(nullable: false),
                    AccessLevel = table.Column<int>(nullable: false),
                    Author = table.Column<string>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MeasurementsDb", x => x.MeasurementId);
                });

            migrationBuilder.CreateTable(
                name: "NotesDb",
                columns: table => new
                {
                    NoteId = table.Column<int>(nullable: false)
                        .Annotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn),
                    Title = table.Column<string>(nullable: true),
                    Content = table.Column<string>(nullable: true),
                    Category = table.Column<string>(nullable: true),
                    CreatedDate = table.Column<DateTime>(nullable: false),
                    AccessLevel = table.Column<int>(nullable: false),
                    ProgenyId = table.Column<int>(nullable: false),
                    Owner = table.Column<string>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NotesDb", x => x.NoteId);
                });

            migrationBuilder.CreateTable(
                name: "SkillsDb",
                columns: table => new
                {
                    SkillId = table.Column<int>(nullable: false)
                        .Annotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn),
                    Name = table.Column<string>(nullable: true),
                    Description = table.Column<string>(nullable: true),
                    Category = table.Column<string>(nullable: true),
                    SkillFirstObservation = table.Column<DateTime>(nullable: true),
                    SkillAddedDate = table.Column<DateTime>(nullable: false),
                    Author = table.Column<string>(nullable: true),
                    ProgenyId = table.Column<int>(nullable: false),
                    AccessLevel = table.Column<int>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SkillsDb", x => x.SkillId);
                    table.ForeignKey(
                        name: "FK_SkillsDb_ProgenyDb_ProgenyId",
                        column: x => x.ProgenyId,
                        principalTable: "ProgenyDb",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SleepDb",
                columns: table => new
                {
                    SleepId = table.Column<int>(nullable: false)
                        .Annotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn),
                    ProgenyId = table.Column<int>(nullable: false),
                    SleepStart = table.Column<DateTime>(nullable: false),
                    SleepEnd = table.Column<DateTime>(nullable: false),
                    CreatedDate = table.Column<DateTime>(nullable: false),
                    SleepRating = table.Column<int>(nullable: false),
                    SleepNotes = table.Column<string>(nullable: true),
                    AccessLevel = table.Column<int>(nullable: false),
                    Author = table.Column<string>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SleepDb", x => x.SleepId);
                });

            migrationBuilder.CreateTable(
                name: "VaccinationsDb",
                columns: table => new
                {
                    VaccinationId = table.Column<int>(nullable: false)
                        .Annotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn),
                    VaccinationName = table.Column<string>(nullable: true),
                    VaccinationDescription = table.Column<string>(nullable: true),
                    VaccinationDate = table.Column<DateTime>(nullable: false),
                    Notes = table.Column<string>(nullable: true),
                    ProgenyId = table.Column<int>(nullable: false),
                    AccessLevel = table.Column<int>(nullable: false),
                    Author = table.Column<string>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VaccinationsDb", x => x.VaccinationId);
                });

            migrationBuilder.CreateTable(
                name: "VocabularyDb",
                columns: table => new
                {
                    WordId = table.Column<int>(nullable: false)
                        .Annotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn),
                    Word = table.Column<string>(nullable: true),
                    Description = table.Column<string>(nullable: true),
                    Language = table.Column<string>(nullable: true),
                    SoundsLike = table.Column<string>(nullable: true),
                    Date = table.Column<DateTime>(nullable: true),
                    DateAdded = table.Column<DateTime>(nullable: false),
                    Author = table.Column<string>(nullable: true),
                    ProgenyId = table.Column<int>(nullable: false),
                    AccessLevel = table.Column<int>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VocabularyDb", x => x.WordId);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SkillsDb_ProgenyId",
                table: "SkillsDb",
                column: "ProgenyId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AddressDb");

            migrationBuilder.DropTable(
                name: "CalendarDb");

            migrationBuilder.DropTable(
                name: "ContactsDb");

            migrationBuilder.DropTable(
                name: "FriendsDb");

            migrationBuilder.DropTable(
                name: "MeasurementsDb");

            migrationBuilder.DropTable(
                name: "NotesDb");

            migrationBuilder.DropTable(
                name: "SkillsDb");

            migrationBuilder.DropTable(
                name: "SleepDb");

            migrationBuilder.DropTable(
                name: "VaccinationsDb");

            migrationBuilder.DropTable(
                name: "VocabularyDb");

            migrationBuilder.RenameColumn(
                name: "AccessId",
                table: "UserAccessDb",
                newName: "Id");
        }
    }
}
