using System;

using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BuildingBlocks.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddDuplicateIncidentRoutingFoundation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "duplicate_incident_assignments",
                schema: "app",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    incident_id = table.Column<Guid>(type: "uuid", nullable: false),
                    assigned_admin_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    assignment_group_node_id = table.Column<Guid>(type: "uuid", nullable: false),
                    assignment_reason = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    assigned_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_duplicate_incident_assignments", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "duplicate_incident_audit_records",
                schema: "app",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    incident_id = table.Column<Guid>(type: "uuid", nullable: false),
                    category = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    action = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    payload_json = table.Column<string>(type: "character varying(4096)", maxLength: 4096, nullable: false),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_duplicate_incident_audit_records", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "duplicate_incident_decisions",
                schema: "app",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    incident_id = table.Column<Guid>(type: "uuid", nullable: false),
                    decided_by_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    decision = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    notes = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: true),
                    decided_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_duplicate_incident_decisions", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "duplicate_incidents",
                schema: "app",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    duplicate_candidate_id = table.Column<Guid>(type: "uuid", nullable: false),
                    source_video_asset_id = table.Column<Guid>(type: "uuid", nullable: false),
                    matched_video_asset_id = table.Column<Guid>(type: "uuid", nullable: false),
                    uploader_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    uploader_group_node_id = table.Column<Guid>(type: "uuid", nullable: false),
                    status = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    severity = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    match_kind = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    reason_code = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    escalated_higher = table.Column<bool>(type: "boolean", nullable: false),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_duplicate_incidents", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_duplicate_incident_assignments_admin_user_id",
                schema: "app",
                table: "duplicate_incident_assignments",
                column: "assigned_admin_user_id");

            migrationBuilder.CreateIndex(
                name: "ix_duplicate_incident_assignments_incident_id",
                schema: "app",
                table: "duplicate_incident_assignments",
                column: "incident_id");

            migrationBuilder.CreateIndex(
                name: "ux_duplicate_incident_assignments_incident_admin",
                schema: "app",
                table: "duplicate_incident_assignments",
                columns: new[] { "incident_id", "assigned_admin_user_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_duplicate_incident_audit_records_incident_id",
                schema: "app",
                table: "duplicate_incident_audit_records",
                column: "incident_id");

            migrationBuilder.CreateIndex(
                name: "ix_duplicate_incident_decisions_incident_id",
                schema: "app",
                table: "duplicate_incident_decisions",
                column: "incident_id");

            migrationBuilder.CreateIndex(
                name: "ix_duplicate_incidents_status",
                schema: "app",
                table: "duplicate_incidents",
                column: "status");

            migrationBuilder.CreateIndex(
                name: "ix_duplicate_incidents_uploader_group_node_id",
                schema: "app",
                table: "duplicate_incidents",
                column: "uploader_group_node_id");

            migrationBuilder.CreateIndex(
                name: "ux_duplicate_incidents_duplicate_candidate_id",
                schema: "app",
                table: "duplicate_incidents",
                column: "duplicate_candidate_id",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "duplicate_incident_assignments",
                schema: "app");

            migrationBuilder.DropTable(
                name: "duplicate_incident_audit_records",
                schema: "app");

            migrationBuilder.DropTable(
                name: "duplicate_incident_decisions",
                schema: "app");

            migrationBuilder.DropTable(
                name: "duplicate_incidents",
                schema: "app");
        }
    }
}