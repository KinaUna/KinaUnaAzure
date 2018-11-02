using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

namespace KinaUnaWeb.Migrations
{
    public partial class InitialMigration : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
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
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TimeLineDb");
        }
    }
}
