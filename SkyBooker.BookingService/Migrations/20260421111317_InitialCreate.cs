using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SkyBooker.BookingService.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "bookings",
                columns: table => new
                {
                    booking_id = table.Column<string>(type: "text", nullable: false),
                    user_id = table.Column<int>(type: "integer", nullable: false),
                    flight_id = table.Column<int>(type: "integer", nullable: false),
                    return_flight_id = table.Column<int>(type: "integer", nullable: true),
                    pnr_code = table.Column<string>(type: "text", nullable: false),
                    trip_type = table.Column<string>(type: "text", nullable: false),
                    status = table.Column<string>(type: "text", nullable: false),
                    base_fare = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    taxes = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    ancillary_cost = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    total_fare = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    meal_preference = table.Column<string>(type: "text", nullable: true),
                    luggage_kg = table.Column<int>(type: "integer", nullable: false),
                    contact_email = table.Column<string>(type: "text", nullable: false),
                    contact_phone = table.Column<string>(type: "text", nullable: true),
                    payment_id = table.Column<string>(type: "text", nullable: true),
                    booked_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    seat_ids = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_bookings", x => x.booking_id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_bookings_flight_id",
                table: "bookings",
                column: "flight_id");

            migrationBuilder.CreateIndex(
                name: "IX_bookings_pnr_code",
                table: "bookings",
                column: "pnr_code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_bookings_user_id",
                table: "bookings",
                column: "user_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "bookings");
        }
    }
}
