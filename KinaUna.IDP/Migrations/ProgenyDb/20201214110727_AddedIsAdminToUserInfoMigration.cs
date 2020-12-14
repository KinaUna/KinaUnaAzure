using Microsoft.EntityFrameworkCore.Migrations;

namespace KinaUna.IDP.Migrations.ProgenyDb
{
    public partial class AddedIsAdminToUserInfoMigration : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsKinaUnaAdmin",
                table: "UserInfoDb",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsPivoqAdmin",
                table: "UserInfoDb",
                nullable: false,
                defaultValue: false);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsKinaUnaAdmin",
                table: "UserInfoDb");

            migrationBuilder.DropColumn(
                name: "IsPivoqAdmin",
                table: "UserInfoDb");
        }
    }
}
