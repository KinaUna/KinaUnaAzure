using Microsoft.EntityFrameworkCore.Migrations;

namespace KinaUnaProgenyApi.Migrations
{
    public partial class UpdatedUserInfo : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "FirstName",
                table: "UserInfoDb",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LastName",
                table: "UserInfoDb",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "MiddleName",
                table: "UserInfoDb",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ProfilePicture",
                table: "UserInfoDb",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Timezone",
                table: "UserInfoDb",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "UserName",
                table: "UserInfoDb",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "FirstName",
                table: "UserInfoDb");

            migrationBuilder.DropColumn(
                name: "LastName",
                table: "UserInfoDb");

            migrationBuilder.DropColumn(
                name: "MiddleName",
                table: "UserInfoDb");

            migrationBuilder.DropColumn(
                name: "ProfilePicture",
                table: "UserInfoDb");

            migrationBuilder.DropColumn(
                name: "Timezone",
                table: "UserInfoDb");

            migrationBuilder.DropColumn(
                name: "UserName",
                table: "UserInfoDb");
        }
    }
}
