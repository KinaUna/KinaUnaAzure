using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

namespace KinaUnaProgenyApi.Migrations
{
    public partial class AddedLocationsDb : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "LocationsDb",
                columns: table => new
                {
                    LocationId = table.Column<int>(nullable: false)
                        .Annotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn),
                    ProgenyId = table.Column<int>(nullable: false),
                    Name = table.Column<string>(nullable: true),
                    Latitude = table.Column<double>(nullable: false),
                    Longitude = table.Column<double>(nullable: false),
                    StreetName = table.Column<string>(nullable: true),
                    HouseNumber = table.Column<string>(nullable: true),
                    City = table.Column<string>(nullable: true),
                    District = table.Column<string>(nullable: true),
                    County = table.Column<string>(nullable: true),
                    State = table.Column<string>(nullable: true),
                    Country = table.Column<string>(nullable: true),
                    PostalCode = table.Column<string>(nullable: true),
                    Date = table.Column<DateTime>(nullable: true),
                    Notes = table.Column<string>(nullable: true),
                    AccessLevel = table.Column<int>(nullable: false),
                    Tags = table.Column<string>(nullable: true),
                    DateAdded = table.Column<DateTime>(nullable: true),
                    Author = table.Column<string>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LocationsDb", x => x.LocationId);
                });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "LocationsDb");
        }
    }
}
