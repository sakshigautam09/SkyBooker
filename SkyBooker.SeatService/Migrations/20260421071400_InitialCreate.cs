using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace SkyBooker.SeatService.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "seats",
                columns: table => new
                {
                    seat_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    flight_id = table.Column<int>(type: "integer", nullable: false),
                    seat_number = table.Column<string>(type: "text", nullable: false),
                    seat_class = table.Column<string>(type: "text", nullable: false),
                    row = table.Column<int>(type: "integer", nullable: false),
                    column = table.Column<string>(type: "text", nullable: false),
                    is_window = table.Column<bool>(type: "boolean", nullable: false),
                    is_aisle = table.Column<bool>(type: "boolean", nullable: false),
                    has_extra_legroom = table.Column<bool>(type: "boolean", nullable: false),
                    status = table.Column<string>(type: "text", nullable: false),
                    price_multiplier = table.Column<decimal>(type: "numeric(5,2)", nullable: false),
                    held_since = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    held_by_user_id = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_seats", x => x.seat_id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_seats_flight_id_seat_class",
                table: "seats",
                columns: new[] { "flight_id", "seat_class" });

            migrationBuilder.CreateIndex(
                name: "IX_seats_flight_id_seat_number",
                table: "seats",
                columns: new[] { "flight_id", "seat_number" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "seats");
        }
    }
}
