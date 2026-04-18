using System;

using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BuildingBlocks.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddFraudSignalsFoundation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "fraud_signal_audit_records",
                schema: "app",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    signal_id = table.Column<Guid>(type: "uuid", nullable: true),
                    incident_id = table.Column<Guid>(type: "uuid", nullable: true),
                    category = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    action = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    payload_json = table.Column<string>(type: "character varying(4096)", maxLength: 4096, nullable: false),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_fraud_signal_audit_records", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "fraud_signal_records",
                schema: "app",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    upload_receipt_id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    device_id = table.Column<Guid>(type: "uuid", nullable: true),
                    uploader_group_node_id = table.Column<Guid>(type: "uuid", nullable: false),
                    duplicate_candidate_id = table.Column<Guid>(type: "uuid", nullable: true),
                    source_video_asset_id = table.Column<Guid>(type: "uuid", nullable: true),
                    matched_video_asset_id = table.Column<Guid>(type: "uuid", nullable: true),
                    business_object_key = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    signal_type = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    severity = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    attempts_in_window = table.Column<int>(type: "integer", nullable: false),
                    duplicate_candidates_in_window = table.Column<int>(type: "integer", nullable: false),
                    score = table.Column<int>(type: "integer", nullable: false),
                    reason_code = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    observed_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_fraud_signal_records", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "fraud_suspicion_incident_assignments",
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
                    table.PrimaryKey("PK_fraud_suspicion_incident_assignments", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "fraud_suspicion_incident_decisions",
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
                    table.PrimaryKey("PK_fraud_suspicion_incident_decisions", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "fraud_suspicion_incidents",
                schema: "app",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    signal_id = table.Column<Guid>(type: "uuid", nullable: false),
                    upload_receipt_id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    device_id = table.Column<Guid>(type: "uuid", nullable: true),
                    uploader_group_node_id = table.Column<Guid>(type: "uuid", nullable: false),
                    duplicate_candidate_id = table.Column<Guid>(type: "uuid", nullable: true),
                    business_object_key = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    status = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    severity = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    score = table.Column<int>(type: "integer", nullable: false),
                    reason_code = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    escalated_higher = table.Column<bool>(type: "boolean", nullable: false),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_fraud_suspicion_incidents", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_fraud_signal_audit_records_incident_id",
                schema: "app",
                table: "fraud_signal_audit_records",
                column: "incident_id");

            migrationBuilder.CreateIndex(
                name: "ix_fraud_signal_audit_records_signal_id",
                schema: "app",
                table: "fraud_signal_audit_records",
                column: "signal_id");

            migrationBuilder.CreateIndex(
                name: "ix_fraud_signal_records_duplicate_candidate_id",
                schema: "app",
                table: "fraud_signal_records",
                column: "duplicate_candidate_id");

            migrationBuilder.CreateIndex(
                name: "ix_fraud_signal_records_user_business_observed",
                schema: "app",
                table: "fraud_signal_records",
                columns: new[] { "user_id", "business_object_key", "observed_at_utc" });

            migrationBuilder.CreateIndex(
                name: "ux_fraud_signal_records_upload_receipt_id",
                schema: "app",
                table: "fraud_signal_records",
                column: "upload_receipt_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_fraud_suspicion_incident_assignments_admin_user_id",
                schema: "app",
                table: "fraud_suspicion_incident_assignments",
                column: "assigned_admin_user_id");

            migrationBuilder.CreateIndex(
                name: "ix_fraud_suspicion_incident_assignments_incident_id",
                schema: "app",
                table: "fraud_suspicion_incident_assignments",
                column: "incident_id");

            migrationBuilder.CreateIndex(
                name: "ux_fraud_suspicion_incident_assignments_incident_admin",
                schema: "app",
                table: "fraud_suspicion_incident_assignments",
                columns: new[] { "incident_id", "assigned_admin_user_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_fraud_suspicion_incident_decisions_incident_id",
                schema: "app",
                table: "fraud_suspicion_incident_decisions",
                column: "incident_id");

            migrationBuilder.CreateIndex(
                name: "ix_fraud_suspicion_incidents_status",
                schema: "app",
                table: "fraud_suspicion_incidents",
                column: "status");

            migrationBuilder.CreateIndex(
                name: "ix_fraud_suspicion_incidents_uploader_group_node_id",
                schema: "app",
                table: "fraud_suspicion_incidents",
                column: "uploader_group_node_id");

            migrationBuilder.CreateIndex(
                name: "ux_fraud_suspicion_incidents_signal_id",
                schema: "app",
                table: "fraud_suspicion_incidents",
                column: "signal_id",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "fraud_signal_audit_records",
                schema: "app");

            migrationBuilder.DropTable(
                name: "fraud_signal_records",
                schema: "app");

            migrationBuilder.DropTable(
                name: "fraud_suspicion_incident_assignments",
                schema: "app");

            migrationBuilder.DropTable(
                name: "fraud_suspicion_incident_decisions",
                schema: "app");

            migrationBuilder.DropTable(
                name: "fraud_suspicion_incidents",
                schema: "app");
        }
    }
}