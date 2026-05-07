using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Slot.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Slots",
                columns: table => new
                {
                    SlotId = table.Column<Guid>(type: "uuid", nullable: false),
                    LotId = table.Column<Guid>(type: "uuid", nullable: false),
                    SlotNumber = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Type = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    PricePerHour = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Slots", x => x.SlotId);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Slots_LotId",
                table: "Slots",
                column: "LotId");

            migrationBuilder.CreateIndex(
                name: "IX_Slots_LotId_SlotNumber",
                table: "Slots",
                columns: new[] { "LotId", "SlotNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Slots_Status",
                table: "Slots",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_Slots_Type",
                table: "Slots",
                column: "Type");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Slots");
        }
    }
}
