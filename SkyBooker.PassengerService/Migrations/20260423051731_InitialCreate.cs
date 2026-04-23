using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace SkyBooker.PassengerService.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "passengers",
                columns: table => new
                {
                    passenger_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    booking_id = table.Column<string>(type: "text", nullable: false),
                    title = table.Column<string>(type: "text", nullable: false),
                    first_name = table.Column<string>(type: "text", nullable: false),
                    last_name = table.Column<string>(type: "text", nullable: false),
                    date_of_birth = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    gender = table.Column<string>(type: "text", nullable: false),
                    passport_number = table.Column<string>(type: "text", nullable: false),
                    nationality = table.Column<string>(type: "text", nullable: false),
                    passport_expiry = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    seat_id = table.Column<int>(type: "integer", nullable: true),
                    seat_number = table.Column<string>(type: "text", nullable: true),
                    ticket_number = table.Column<string>(type: "text", nullable: true),
                    passenger_type = table.Column<string>(type: "text", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_passengers", x => x.passenger_id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_passengers_booking_id",
                table: "passengers",
                column: "booking_id");

            migrationBuilder.CreateIndex(
                name: "IX_passengers_booking_id_passport_number",
                table: "passengers",
                columns: new[] { "booking_id", "passport_number" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_passengers_passport_number",
                table: "passengers",
                column: "passport_number");

            migrationBuilder.CreateIndex(
                name: "IX_passengers_ticket_number",
                table: "passengers",
                column: "ticket_number",
                unique: true,
                filter: "ticket_number IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "passengers");
        }
    }
}
