using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace SkyBooker.AirlineService.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "airlines",
                columns: table => new
                {
                    airline_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    name = table.Column<string>(type: "text", nullable: false),
                    iata_code = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    icao_code = table.Column<string>(type: "character varying(4)", maxLength: 4, nullable: true),
                    logo_url = table.Column<string>(type: "text", nullable: true),
                    country = table.Column<string>(type: "text", nullable: true),
                    contact_email = table.Column<string>(type: "text", nullable: true),
                    contact_phone = table.Column<string>(type: "text", nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_airlines", x => x.airline_id);
                });

            migrationBuilder.CreateTable(
                name: "airports",
                columns: table => new
                {
                    airport_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    name = table.Column<string>(type: "text", nullable: false),
                    iata_code = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    icao_code = table.Column<string>(type: "character varying(4)", maxLength: 4, nullable: true),
                    city = table.Column<string>(type: "text", nullable: true),
                    country = table.Column<string>(type: "text", nullable: true),
                    latitude = table.Column<double>(type: "double precision", nullable: false),
                    longitude = table.Column<double>(type: "double precision", nullable: false),
                    timezone = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_airports", x => x.airport_id);
                });

            migrationBuilder.CreateTable(
                name: "airline_airports",
                columns: table => new
                {
                    airline_id = table.Column<int>(type: "integer", nullable: false),
                    airport_id = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_airline_airports", x => new { x.airline_id, x.airport_id });
                    table.ForeignKey(
                        name: "FK_airline_airports_airlines_airline_id",
                        column: x => x.airline_id,
                        principalTable: "airlines",
                        principalColumn: "airline_id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_airline_airports_airports_airport_id",
                        column: x => x.airport_id,
                        principalTable: "airports",
                        principalColumn: "airport_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_airline_airports_airport_id",
                table: "airline_airports",
                column: "airport_id");

            migrationBuilder.CreateIndex(
                name: "IX_airlines_iata_code",
                table: "airlines",
                column: "iata_code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_airports_iata_code",
                table: "airports",
                column: "iata_code",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "airline_airports");

            migrationBuilder.DropTable(
                name: "airlines");

            migrationBuilder.DropTable(
                name: "airports");
        }
    }
}
