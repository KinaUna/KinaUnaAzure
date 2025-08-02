using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KinaUna.IDP.Migrations
{
    /// <inheritdoc />
    public partial class RemovedDataProtectionKeys : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DataProtectionKeys");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "DataProtectionKeys",
                columns: table => new
                {
                    FriendlyName = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    XmlData = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DataProtectionKeys", x => x.FriendlyName);
                });
        }
    }
}
