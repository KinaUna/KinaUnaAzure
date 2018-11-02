using Microsoft.EntityFrameworkCore.Migrations;

namespace KinaUnaMediaApi.Migrations
{
    public partial class AddedLocationToVideos : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Altitude",
                table: "VideoDb",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Latitude",
                table: "VideoDb",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Location",
                table: "VideoDb",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Longtitude",
                table: "VideoDb",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Altitude",
                table: "VideoDb");

            migrationBuilder.DropColumn(
                name: "Latitude",
                table: "VideoDb");

            migrationBuilder.DropColumn(
                name: "Location",
                table: "VideoDb");

            migrationBuilder.DropColumn(
                name: "Longtitude",
                table: "VideoDb");
        }
    }
}
