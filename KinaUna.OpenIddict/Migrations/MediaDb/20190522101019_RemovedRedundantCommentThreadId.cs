using Microsoft.EntityFrameworkCore.Migrations;

namespace KinaUna.OpenIddict.Migrations.MediaDb
{
    public partial class RemovedRedundantCommentThreadId : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CommentThreadId",
                table: "CommentThreadsDb");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "CommentThreadId",
                table: "CommentThreadsDb",
                nullable: false,
                defaultValue: 0);
        }
    }
}
