using Microsoft.EntityFrameworkCore.Migrations;

namespace KinaUna.IDP.Migrations.ProgenyDb
{
    public partial class AddIsKinaUnaToUserInfoMigration : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsKinaUnaUser",
                table: "UserInfoDb",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsPivoqUser",
                table: "UserInfoDb",
                nullable: false,
                defaultValue: false);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsKinaUnaUser",
                table: "UserInfoDb");

            migrationBuilder.DropColumn(
                name: "IsPivoqUser",
                table: "UserInfoDb");
        }
    }
}
