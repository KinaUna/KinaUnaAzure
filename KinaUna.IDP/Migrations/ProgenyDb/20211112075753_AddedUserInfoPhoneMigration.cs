using Microsoft.EntityFrameworkCore.Migrations;

namespace KinaUna.IDP.Migrations.ProgenyDb
{
    public partial class AddedUserInfoPhoneMigration : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "PhoneNumber",
                table: "UserInfoDb",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PhoneNumber",
                table: "UserInfoDb");
        }
    }
}
