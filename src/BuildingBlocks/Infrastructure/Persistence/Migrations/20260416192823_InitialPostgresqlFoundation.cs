using System;

using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BuildingBlocks.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialPostgresqlFoundation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "app");

            migrationBuilder.CreateTable(
                name: "database_bootstrap_markers",
                schema: "app",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    code = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_database_bootstrap_markers", x => x.id);
                });

            migrationBuilder.InsertData(
                schema: "app",
                table: "database_bootstrap_markers",
                columns: new[] { "id", "code", "created_at_utc" },
                values: new object[] { new Guid("1d53b52b-ab0f-4b7f-bac7-1b7f3e5e7707"), "initial_postgresql_foundation", new DateTimeOffset(new DateTime(2026, 4, 16, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) });

            migrationBuilder.CreateIndex(
                name: "ux_database_bootstrap_markers_code",
                schema: "app",
                table: "database_bootstrap_markers",
                column: "code",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "database_bootstrap_markers",
                schema: "app");
        }
    }
}