using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

namespace KinaUnaMediaApi.Migrations
{
    public partial class InitialMediaMigration : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PicturesDb",
                columns: table => new
                {
                    PictureId = table.Column<int>(nullable: false)
                        .Annotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn),
                    PictureLink = table.Column<string>(maxLength: 400, nullable: false),
                    PictureLink600 = table.Column<string>(nullable: true),
                    PictureLink1200 = table.Column<string>(nullable: true),
                    PictureTime = table.Column<DateTime>(nullable: true),
                    PictureRotation = table.Column<int>(nullable: true),
                    PictureWidth = table.Column<int>(nullable: false),
                    PictureHeight = table.Column<int>(nullable: false),
                    Tags = table.Column<string>(nullable: true),
                    Location = table.Column<string>(nullable: true),
                    Longtitude = table.Column<string>(nullable: true),
                    Latitude = table.Column<string>(nullable: true),
                    Altitude = table.Column<string>(nullable: true),
                    ProgenyId = table.Column<int>(nullable: false),
                    Owners = table.Column<string>(nullable: true),
                    Author = table.Column<string>(nullable: true),
                    AccessLevel = table.Column<int>(nullable: false),
                    CommentThreadNumber = table.Column<int>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PicturesDb", x => x.PictureId);
                });

            migrationBuilder.CreateTable(
                name: "VideoDb",
                columns: table => new
                {
                    VideoId = table.Column<int>(nullable: false)
                        .Annotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn),
                    VideoTime = table.Column<DateTime>(nullable: true),
                    VideoLink = table.Column<string>(nullable: true),
                    ThumbLink = table.Column<string>(nullable: true),
                    ProgenyId = table.Column<int>(nullable: false),
                    Owners = table.Column<string>(nullable: true),
                    AccessLevel = table.Column<int>(nullable: false),
                    CommentThreadNumber = table.Column<int>(nullable: false),
                    VideoType = table.Column<int>(nullable: false),
                    Tags = table.Column<string>(nullable: true),
                    Duration = table.Column<TimeSpan>(nullable: true),
                    Author = table.Column<string>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VideoDb", x => x.VideoId);
                });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PicturesDb");

            migrationBuilder.DropTable(
                name: "VideoDb");
        }
    }
}
