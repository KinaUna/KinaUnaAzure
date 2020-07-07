using Microsoft.EntityFrameworkCore.Migrations;

namespace KinaUna.IDP.Migrations.ProgenyDb
{
    public partial class InitialProgenyDbMigration : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            //migrationBuilder.CreateTable(
            //    name: "AddressDb",
            //    columns: table => new
            //    {
            //        AddressId = table.Column<int>(nullable: false)
            //            .Annotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn),
            //        AddressLine1 = table.Column<string>(nullable: true),
            //        AddressLine2 = table.Column<string>(nullable: true),
            //        City = table.Column<string>(nullable: true),
            //        State = table.Column<string>(nullable: true),
            //        PostalCode = table.Column<string>(nullable: true),
            //        Country = table.Column<string>(nullable: true)
            //    },
            //    constraints: table =>
            //    {
            //        table.PrimaryKey("PK_AddressDb", x => x.AddressId);
            //    });

            //migrationBuilder.CreateTable(
            //    name: "CalendarDb",
            //    columns: table => new
            //    {
            //        EventId = table.Column<int>(nullable: false)
            //            .Annotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn),
            //        ProgenyId = table.Column<int>(nullable: false),
            //        Title = table.Column<string>(nullable: true),
            //        Notes = table.Column<string>(nullable: true),
            //        StartTime = table.Column<DateTime>(nullable: true),
            //        EndTime = table.Column<DateTime>(nullable: true),
            //        Location = table.Column<string>(nullable: true),
            //        Context = table.Column<string>(nullable: true),
            //        AllDay = table.Column<bool>(nullable: false),
            //        AccessLevel = table.Column<int>(nullable: false),
            //        StartString = table.Column<string>(nullable: true),
            //        EndString = table.Column<string>(nullable: true),
            //        Author = table.Column<string>(nullable: true)
            //    },
            //    constraints: table =>
            //    {
            //        table.PrimaryKey("PK_CalendarDb", x => x.EventId);
            //    });

            //migrationBuilder.CreateTable(
            //    name: "ContactsDb",
            //    columns: table => new
            //    {
            //        ContactId = table.Column<int>(nullable: false)
            //            .Annotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn),
            //        Active = table.Column<bool>(nullable: false),
            //        FirstName = table.Column<string>(nullable: true),
            //        MiddleName = table.Column<string>(nullable: true),
            //        LastName = table.Column<string>(nullable: true),
            //        DisplayName = table.Column<string>(nullable: true),
            //        AddressIdNumber = table.Column<int>(nullable: true),
            //        Email1 = table.Column<string>(nullable: true),
            //        Email2 = table.Column<string>(nullable: true),
            //        PhoneNumber = table.Column<string>(nullable: true),
            //        MobileNumber = table.Column<string>(nullable: true),
            //        Context = table.Column<string>(nullable: true),
            //        Notes = table.Column<string>(nullable: true),
            //        PictureLink = table.Column<string>(nullable: true),
            //        Website = table.Column<string>(nullable: true),
            //        AccessLevel = table.Column<int>(nullable: false),
            //        ProgenyId = table.Column<int>(nullable: false),
            //        Tags = table.Column<string>(nullable: true),
            //        DateAdded = table.Column<DateTime>(nullable: true),
            //        Author = table.Column<string>(nullable: true)
            //    },
            //    constraints: table =>
            //    {
            //        table.PrimaryKey("PK_ContactsDb", x => x.ContactId);
            //    });

            //migrationBuilder.CreateTable(
            //    name: "FriendsDb",
            //    columns: table => new
            //    {
            //        FriendId = table.Column<int>(nullable: false)
            //            .Annotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn),
            //        Name = table.Column<string>(nullable: true),
            //        Description = table.Column<string>(nullable: true),
            //        FriendSince = table.Column<DateTime>(nullable: true),
            //        FriendAddedDate = table.Column<DateTime>(nullable: false),
            //        PictureLink = table.Column<string>(nullable: true),
            //        ProgenyId = table.Column<int>(nullable: false),
            //        Type = table.Column<int>(nullable: false),
            //        Context = table.Column<string>(nullable: true),
            //        Notes = table.Column<string>(nullable: true),
            //        Tags = table.Column<string>(nullable: true),
            //        AccessLevel = table.Column<int>(nullable: false),
            //        Author = table.Column<string>(nullable: true)
            //    },
            //    constraints: table =>
            //    {
            //        table.PrimaryKey("PK_FriendsDb", x => x.FriendId);
            //    });

            //migrationBuilder.CreateTable(
            //    name: "LocationsDb",
            //    columns: table => new
            //    {
            //        LocationId = table.Column<int>(nullable: false)
            //            .Annotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn),
            //        ProgenyId = table.Column<int>(nullable: false),
            //        Name = table.Column<string>(nullable: true),
            //        Latitude = table.Column<double>(nullable: false),
            //        Longitude = table.Column<double>(nullable: false),
            //        StreetName = table.Column<string>(nullable: true),
            //        HouseNumber = table.Column<string>(nullable: true),
            //        City = table.Column<string>(nullable: true),
            //        District = table.Column<string>(nullable: true),
            //        County = table.Column<string>(nullable: true),
            //        State = table.Column<string>(nullable: true),
            //        Country = table.Column<string>(nullable: true),
            //        PostalCode = table.Column<string>(nullable: true),
            //        Date = table.Column<DateTime>(nullable: true),
            //        Notes = table.Column<string>(nullable: true),
            //        AccessLevel = table.Column<int>(nullable: false),
            //        Tags = table.Column<string>(nullable: true),
            //        DateAdded = table.Column<DateTime>(nullable: true),
            //        Author = table.Column<string>(nullable: true)
            //    },
            //    constraints: table =>
            //    {
            //        table.PrimaryKey("PK_LocationsDb", x => x.LocationId);
            //    });

            //migrationBuilder.CreateTable(
            //    name: "MeasurementsDb",
            //    columns: table => new
            //    {
            //        MeasurementId = table.Column<int>(nullable: false)
            //            .Annotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn),
            //        ProgenyId = table.Column<int>(nullable: false),
            //        Weight = table.Column<double>(nullable: false),
            //        Height = table.Column<double>(nullable: false),
            //        Circumference = table.Column<double>(nullable: false),
            //        EyeColor = table.Column<string>(nullable: true),
            //        HairColor = table.Column<string>(nullable: true),
            //        Date = table.Column<DateTime>(nullable: false),
            //        CreatedDate = table.Column<DateTime>(nullable: false),
            //        AccessLevel = table.Column<int>(nullable: false),
            //        Author = table.Column<string>(nullable: true)
            //    },
            //    constraints: table =>
            //    {
            //        table.PrimaryKey("PK_MeasurementsDb", x => x.MeasurementId);
            //    });

            //migrationBuilder.CreateTable(
            //    name: "NotesDb",
            //    columns: table => new
            //    {
            //        NoteId = table.Column<int>(nullable: false)
            //            .Annotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn),
            //        Title = table.Column<string>(nullable: true),
            //        Content = table.Column<string>(nullable: true),
            //        Category = table.Column<string>(nullable: true),
            //        CreatedDate = table.Column<DateTime>(nullable: false),
            //        AccessLevel = table.Column<int>(nullable: false),
            //        ProgenyId = table.Column<int>(nullable: false),
            //        Owner = table.Column<string>(nullable: true)
            //    },
            //    constraints: table =>
            //    {
            //        table.PrimaryKey("PK_NotesDb", x => x.NoteId);
            //    });

            //migrationBuilder.CreateTable(
            //    name: "ProgenyDb",
            //    columns: table => new
            //    {
            //        Id = table.Column<int>(nullable: false)
            //            .Annotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn),
            //        Name = table.Column<string>(nullable: true),
            //        NickName = table.Column<string>(nullable: true),
            //        BirthDay = table.Column<DateTime>(nullable: true),
            //        TimeZone = table.Column<string>(nullable: true),
            //        PictureLink = table.Column<string>(nullable: true),
            //        Admins = table.Column<string>(nullable: true)
            //    },
            //    constraints: table =>
            //    {
            //        table.PrimaryKey("PK_ProgenyDb", x => x.Id);
            //    });

            //migrationBuilder.CreateTable(
            //    name: "SkillsDb",
            //    columns: table => new
            //    {
            //        SkillId = table.Column<int>(nullable: false)
            //            .Annotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn),
            //        Name = table.Column<string>(nullable: true),
            //        Description = table.Column<string>(nullable: true),
            //        Category = table.Column<string>(nullable: true),
            //        SkillFirstObservation = table.Column<DateTime>(nullable: true),
            //        SkillAddedDate = table.Column<DateTime>(nullable: false),
            //        Author = table.Column<string>(nullable: true),
            //        ProgenyId = table.Column<int>(nullable: false),
            //        AccessLevel = table.Column<int>(nullable: false)
            //    },
            //    constraints: table =>
            //    {
            //        table.PrimaryKey("PK_SkillsDb", x => x.SkillId);
            //    });

            //migrationBuilder.CreateTable(
            //    name: "SleepDb",
            //    columns: table => new
            //    {
            //        SleepId = table.Column<int>(nullable: false)
            //            .Annotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn),
            //        ProgenyId = table.Column<int>(nullable: false),
            //        SleepStart = table.Column<DateTime>(nullable: false),
            //        SleepEnd = table.Column<DateTime>(nullable: false),
            //        CreatedDate = table.Column<DateTime>(nullable: false),
            //        SleepRating = table.Column<int>(nullable: false),
            //        SleepNotes = table.Column<string>(nullable: true),
            //        AccessLevel = table.Column<int>(nullable: false),
            //        Author = table.Column<string>(nullable: true)
            //    },
            //    constraints: table =>
            //    {
            //        table.PrimaryKey("PK_SleepDb", x => x.SleepId);
            //    });

            //migrationBuilder.CreateTable(
            //    name: "TimeLineDb",
            //    columns: table => new
            //    {
            //        TimeLineId = table.Column<int>(nullable: false)
            //            .Annotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn),
            //        ProgenyId = table.Column<int>(nullable: false),
            //        ProgenyTime = table.Column<DateTime>(nullable: false),
            //        CreatedTime = table.Column<DateTime>(nullable: false),
            //        ItemType = table.Column<int>(nullable: false),
            //        ItemId = table.Column<string>(nullable: true),
            //        CreatedBy = table.Column<string>(nullable: true),
            //        AccessLevel = table.Column<int>(nullable: false)
            //    },
            //    constraints: table =>
            //    {
            //        table.PrimaryKey("PK_TimeLineDb", x => x.TimeLineId);
            //    });

            //migrationBuilder.CreateTable(
            //    name: "UserAccessDb",
            //    columns: table => new
            //    {
            //        AccessId = table.Column<int>(nullable: false)
            //            .Annotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn),
            //        ProgenyId = table.Column<int>(nullable: false),
            //        UserId = table.Column<string>(nullable: true),
            //        AccessLevel = table.Column<int>(nullable: false),
            //        CanContribute = table.Column<bool>(nullable: false)
            //    },
            //    constraints: table =>
            //    {
            //        table.PrimaryKey("PK_UserAccessDb", x => x.AccessId);
            //    });

            //migrationBuilder.CreateTable(
            //    name: "UserInfoDb",
            //    columns: table => new
            //    {
            //        Id = table.Column<int>(nullable: false)
            //            .Annotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn),
            //        UserId = table.Column<string>(nullable: true),
            //        UserEmail = table.Column<string>(nullable: true),
            //        UserName = table.Column<string>(nullable: true),
            //        FirstName = table.Column<string>(nullable: true),
            //        MiddleName = table.Column<string>(nullable: true),
            //        LastName = table.Column<string>(nullable: true),
            //        ViewChild = table.Column<int>(nullable: false),
            //        Timezone = table.Column<string>(nullable: true),
            //        ProfilePicture = table.Column<string>(nullable: true)
            //    },
            //    constraints: table =>
            //    {
            //        table.PrimaryKey("PK_UserInfoDb", x => x.Id);
            //    });

            //migrationBuilder.CreateTable(
            //    name: "VaccinationsDb",
            //    columns: table => new
            //    {
            //        VaccinationId = table.Column<int>(nullable: false)
            //            .Annotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn),
            //        VaccinationName = table.Column<string>(nullable: true),
            //        VaccinationDescription = table.Column<string>(nullable: true),
            //        VaccinationDate = table.Column<DateTime>(nullable: false),
            //        Notes = table.Column<string>(nullable: true),
            //        ProgenyId = table.Column<int>(nullable: false),
            //        AccessLevel = table.Column<int>(nullable: false),
            //        Author = table.Column<string>(nullable: true)
            //    },
            //    constraints: table =>
            //    {
            //        table.PrimaryKey("PK_VaccinationsDb", x => x.VaccinationId);
            //    });

            //migrationBuilder.CreateTable(
            //    name: "VocabularyDb",
            //    columns: table => new
            //    {
            //        WordId = table.Column<int>(nullable: false)
            //            .Annotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn),
            //        Word = table.Column<string>(nullable: true),
            //        Description = table.Column<string>(nullable: true),
            //        Language = table.Column<string>(nullable: true),
            //        SoundsLike = table.Column<string>(nullable: true),
            //        Date = table.Column<DateTime>(nullable: true),
            //        DateAdded = table.Column<DateTime>(nullable: false),
            //        Author = table.Column<string>(nullable: true),
            //        ProgenyId = table.Column<int>(nullable: false),
            //        AccessLevel = table.Column<int>(nullable: false)
            //    },
            //    constraints: table =>
            //    {
            //        table.PrimaryKey("PK_VocabularyDb", x => x.WordId);
            //    });
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
                name: "LocationsDb");

            migrationBuilder.DropTable(
                name: "MeasurementsDb");

            migrationBuilder.DropTable(
                name: "NotesDb");

            migrationBuilder.DropTable(
                name: "ProgenyDb");

            migrationBuilder.DropTable(
                name: "SkillsDb");

            migrationBuilder.DropTable(
                name: "SleepDb");

            migrationBuilder.DropTable(
                name: "TimeLineDb");

            migrationBuilder.DropTable(
                name: "UserAccessDb");

            migrationBuilder.DropTable(
                name: "UserInfoDb");

            migrationBuilder.DropTable(
                name: "VaccinationsDb");

            migrationBuilder.DropTable(
                name: "VocabularyDb");
        }
    }
}
