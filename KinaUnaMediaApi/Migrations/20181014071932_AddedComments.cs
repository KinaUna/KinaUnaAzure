using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

namespace KinaUnaMediaApi.Migrations
{
    public partial class AddedComments : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CommentsDb",
                columns: table => new
                {
                    CommentId = table.Column<int>(nullable: false)
                        .Annotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn),
                    CommentThreadNumber = table.Column<int>(nullable: false),
                    CommentText = table.Column<string>(nullable: true),
                    Author = table.Column<string>(nullable: true),
                    DisplayName = table.Column<string>(nullable: true),
                    Created = table.Column<DateTime>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CommentsDb", x => x.CommentId);
                });

            migrationBuilder.CreateTable(
                name: "CommentThreadsDb",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn),
                    CommentThreadId = table.Column<int>(nullable: false),
                    CommentsCount = table.Column<int>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CommentThreadsDb", x => x.Id);
                });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CommentsDb");

            migrationBuilder.DropTable(
                name: "CommentThreadsDb");
        }
    }
}
