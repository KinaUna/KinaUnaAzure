using Microsoft.EntityFrameworkCore.Migrations;

namespace KinaUnaWeb.Migrations
{
    public partial class AddedLinkToWebNotification : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Link",
                table: "WebNotificationsDb",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Link",
                table: "WebNotificationsDb");
        }
    }
}
