using Microsoft.EntityFrameworkCore.Migrations;

namespace KinaUna.OpenIddict.Migrations.ProgenyDb
{
    /// <inheritdoc />
    public partial class RemovePivoqFromUserInfo : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsKinaUnaUser",
                table: "UserInfo");

            migrationBuilder.DropColumn(
                name: "IsPivoqAdmin",
                table: "UserInfo");

            migrationBuilder.DropColumn(
                name: "IsPivoqUser",
                table: "UserInfo");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsKinaUnaUser",
                table: "UserInfo",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsPivoqAdmin",
                table: "UserInfo",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsPivoqUser",
                table: "UserInfo",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }
    }
}
