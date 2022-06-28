using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KinaUna.IDP.Migrations.ProgenyDb
{
    public partial class AddUserInfoDeleted : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_UserInfoDb",
                table: "UserInfoDb");

            migrationBuilder.RenameTable(
                name: "UserInfoDb",
                newName: "UserInfo");

            migrationBuilder.AddColumn<bool>(
                name: "Deleted",
                table: "UserInfo",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedTime",
                table: "UserInfo",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedTime",
                table: "UserInfo",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddPrimaryKey(
                name: "PK_UserInfo",
                table: "UserInfo",
                column: "Id");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_UserInfo",
                table: "UserInfo");

            migrationBuilder.DropColumn(
                name: "Deleted",
                table: "UserInfo");

            migrationBuilder.DropColumn(
                name: "DeletedTime",
                table: "UserInfo");

            migrationBuilder.DropColumn(
                name: "UpdatedTime",
                table: "UserInfo");

            migrationBuilder.RenameTable(
                name: "UserInfo",
                newName: "UserInfoDb");

            migrationBuilder.AddPrimaryKey(
                name: "PK_UserInfoDb",
                table: "UserInfoDb",
                column: "Id");
        }
    }
}
