using System;

using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BuildingBlocks.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddVideoUploadReceiptFoundation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "video_upload_receipt_analysis_jobs",
                schema: "app",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    upload_receipt_id = table.Column<Guid>(type: "uuid", nullable: false),
                    pre_upload_check_id = table.Column<Guid>(type: "uuid", nullable: false),
                    command_name = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    status = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    enqueued_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_video_upload_receipt_analysis_jobs", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "video_upload_receipt_audit_records",
                schema: "app",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    upload_receipt_id = table.Column<Guid>(type: "uuid", nullable: false),
                    category = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    action = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    correlation_id = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    payload_json = table.Column<string>(type: "character varying(4096)", maxLength: 4096, nullable: false),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_video_upload_receipt_audit_records", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "video_upload_receipts",
                schema: "app",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    pre_upload_check_id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    device_id = table.Column<Guid>(type: "uuid", nullable: true),
                    group_node_id = table.Column<Guid>(type: "uuid", nullable: true),
                    external_video_id = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    storage_key = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    site_status = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    size_bytes = table.Column<long>(type: "bigint", nullable: false),
                    byte_sha256 = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    idempotency_key = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    receipt_status = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    analysis_job_status = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    uploaded_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    received_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_video_upload_receipts", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_video_upload_receipt_analysis_jobs_status",
                schema: "app",
                table: "video_upload_receipt_analysis_jobs",
                column: "status");

            migrationBuilder.CreateIndex(
                name: "ix_video_upload_receipt_analysis_jobs_upload_receipt_id",
                schema: "app",
                table: "video_upload_receipt_analysis_jobs",
                column: "upload_receipt_id");

            migrationBuilder.CreateIndex(
                name: "ix_video_upload_receipt_audit_records_correlation_id",
                schema: "app",
                table: "video_upload_receipt_audit_records",
                column: "correlation_id");

            migrationBuilder.CreateIndex(
                name: "ix_video_upload_receipt_audit_records_upload_receipt_id",
                schema: "app",
                table: "video_upload_receipt_audit_records",
                column: "upload_receipt_id");

            migrationBuilder.CreateIndex(
                name: "ix_video_upload_receipts_fingerprint",
                schema: "app",
                table: "video_upload_receipts",
                columns: new[] { "byte_sha256", "size_bytes" });

            migrationBuilder.CreateIndex(
                name: "ix_video_upload_receipts_pre_upload_check_id",
                schema: "app",
                table: "video_upload_receipts",
                column: "pre_upload_check_id");

            migrationBuilder.CreateIndex(
                name: "ux_video_upload_receipts_idempotency_key",
                schema: "app",
                table: "video_upload_receipts",
                column: "idempotency_key",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "video_upload_receipt_analysis_jobs",
                schema: "app");

            migrationBuilder.DropTable(
                name: "video_upload_receipt_audit_records",
                schema: "app");

            migrationBuilder.DropTable(
                name: "video_upload_receipts",
                schema: "app");
        }
    }
}