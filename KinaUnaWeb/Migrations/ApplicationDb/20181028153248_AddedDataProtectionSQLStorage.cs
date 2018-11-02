using Microsoft.EntityFrameworkCore.Migrations;

namespace KinaUnaWeb.Migrations.ApplicationDb
{
    public partial class AddedDataProtectionSQLStorage : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "DataProtectionKeys",
                columns: table => new
                {
                    FriendlyName = table.Column<string>(nullable: false),
                    XmlData = table.Column<string>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DataProtectionKeys", x => x.FriendlyName);
                });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DataProtectionKeys");
        }
    }
}
