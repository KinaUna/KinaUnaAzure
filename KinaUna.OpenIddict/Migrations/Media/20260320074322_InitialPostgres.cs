using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace KinaUna.OpenIddict.Migrations.Media
{
    /// <inheritdoc />
    public partial class InitialPostgres : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CommentsDb",
                columns: table => new
                {
                    CommentId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CommentThreadNumber = table.Column<int>(type: "integer", nullable: false),
                    CommentText = table.Column<string>(type: "character varying(4096)", maxLength: 4096, nullable: true),
                    Author = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    DisplayName = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    Created = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CommentsDb", x => x.CommentId);
                });

            migrationBuilder.CreateTable(
                name: "CommentThreadsDb",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CommentsCount = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CommentThreadsDb", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PicturesDb",
                columns: table => new
                {
                    PictureId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    PictureLink = table.Column<string>(type: "character varying(400)", maxLength: 400, nullable: false),
                    PictureLink600 = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    PictureLink1200 = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    PictureTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    PictureRotation = table.Column<int>(type: "integer", nullable: true),
                    PictureWidth = table.Column<int>(type: "integer", nullable: false),
                    PictureHeight = table.Column<int>(type: "integer", nullable: false),
                    Tags = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    Location = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    Longtitude = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    Latitude = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    Altitude = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    ProgenyId = table.Column<int>(type: "integer", nullable: false),
                    Owners = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: true),
                    Author = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    CommentThreadNumber = table.Column<int>(type: "integer", nullable: false),
                    CreatedBy = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    CreatedTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ModifiedBy = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    ModifiedTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PicturesDb", x => x.PictureId);
                });

            migrationBuilder.CreateTable(
                name: "VideoDb",
                columns: table => new
                {
                    VideoId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    VideoTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    VideoLink = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: true),
                    ThumbLink = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: true),
                    ProgenyId = table.Column<int>(type: "integer", nullable: false),
                    Owners = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: true),
                    CommentThreadNumber = table.Column<int>(type: "integer", nullable: false),
                    VideoType = table.Column<int>(type: "integer", nullable: false),
                    Tags = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: true),
                    Duration = table.Column<TimeSpan>(type: "interval", nullable: true),
                    Author = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    Location = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    Longtitude = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    Latitude = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    Altitude = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    CreatedBy = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    CreatedTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ModifiedBy = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    ModifiedTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VideoDb", x => x.VideoId);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CommentsDb");

            migrationBuilder.DropTable(
                name: "CommentThreadsDb");

            migrationBuilder.DropTable(
                name: "PicturesDb");

            migrationBuilder.DropTable(
                name: "VideoDb");
        }
    }
}
