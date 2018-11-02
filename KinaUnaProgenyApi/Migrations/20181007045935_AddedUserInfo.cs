using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

namespace KinaUnaProgenyApi.Migrations
{
    public partial class AddedUserInfo : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "CanContribute",
                table: "UserAccessDb",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "UserInfoDb",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn),
                    UserId = table.Column<string>(nullable: true),
                    UserEmail = table.Column<string>(nullable: true),
                    ViewChild = table.Column<int>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserInfoDb", x => x.Id);
                });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "UserInfoDb");

            migrationBuilder.DropColumn(
                name: "CanContribute",
                table: "UserAccessDb");
        }
    }
}
