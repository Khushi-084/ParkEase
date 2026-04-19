using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ParkingLot.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddUniqueLotNameCityIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ParkingLots",
                columns: table => new
                {
                    LotId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    Address = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    City = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    State = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    PinCode = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    Latitude = table.Column<double>(type: "double precision", nullable: false),
                    Longitude = table.Column<double>(type: "double precision", nullable: false),
                    TotalSpots = table.Column<int>(type: "integer", nullable: false),
                    AvailableSpots = table.Column<int>(type: "integer", nullable: false),
                    PricePerHour = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: false),
                    Status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    ManagerId = table.Column<Guid>(type: "uuid", nullable: false),
                    ImageUrl = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ParkingLots", x => x.LotId);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ParkingLots_City",
                table: "ParkingLots",
                column: "City");

            migrationBuilder.CreateIndex(
                name: "IX_ParkingLots_ManagerId",
                table: "ParkingLots",
                column: "ManagerId");

            migrationBuilder.CreateIndex(
                name: "IX_ParkingLots_Name_City",
                table: "ParkingLots",
                columns: new[] { "Name", "City" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ParkingLots_Status",
                table: "ParkingLots",
                column: "Status");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ParkingLots");
        }
    }
}
