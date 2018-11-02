using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

namespace KinaUnaProgenyApi.Migrations
{
    public partial class InitialMigration : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ProgenyDb",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn),
                    Name = table.Column<string>(nullable: true),
                    NickName = table.Column<string>(nullable: true),
                    BirthDay = table.Column<DateTime>(nullable: true),
                    TimeZone = table.Column<string>(nullable: true),
                    PictureLink = table.Column<string>(nullable: true),
                    Admins = table.Column<string>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProgenyDb", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TimeLineDb",
                columns: table => new
                {
                    TimeLineId = table.Column<int>(nullable: false)
                        .Annotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn),
                    ProgenyId = table.Column<int>(nullable: false),
                    ProgenyTime = table.Column<DateTime>(nullable: false),
                    CreatedTime = table.Column<DateTime>(nullable: false),
                    ItemType = table.Column<int>(nullable: false),
                    ItemId = table.Column<string>(nullable: true),
                    CreatedBy = table.Column<string>(nullable: true),
                    AccessLevel = table.Column<int>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TimeLineDb", x => x.TimeLineId);
                });

            migrationBuilder.CreateTable(
                name: "UserAccessDb",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn),
                    ProgenyId = table.Column<int>(nullable: false),
                    UserId = table.Column<string>(nullable: true),
                    AccessLevel = table.Column<int>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserAccessDb", x => x.Id);
                });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ProgenyDb");

            migrationBuilder.DropTable(
                name: "TimeLineDb");

            migrationBuilder.DropTable(
                name: "UserAccessDb");
        }
    }
}
