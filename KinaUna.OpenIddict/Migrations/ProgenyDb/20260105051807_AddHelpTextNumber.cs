using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KinaUna.OpenIddict.Migrations.ProgenyDb
{
    /// <inheritdoc />
    public partial class AddHelpTextNumber : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "HelpTextNumbersDb",
                columns: table => new
                {
                    TextId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HelpTextNumbersDb", x => x.TextId);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "HelpTextNumbersDb");
        }
    }
}
