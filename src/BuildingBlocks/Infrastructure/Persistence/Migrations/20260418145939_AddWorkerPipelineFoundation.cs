using System;

using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BuildingBlocks.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddWorkerPipelineFoundation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "worker_pipeline_job_audit_records",
                schema: "app",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    job_id = table.Column<Guid>(type: "uuid", nullable: false),
                    action = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    payload_json = table.Column<string>(type: "character varying(4096)", maxLength: 4096, nullable: false),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_worker_pipeline_job_audit_records", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "worker_pipeline_jobs",
                schema: "app",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    job_type = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    status = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    upload_receipt_id = table.Column<Guid>(type: "uuid", nullable: true),
                    payload_json = table.Column<string>(type: "character varying(8192)", maxLength: 8192, nullable: false),
                    result_json = table.Column<string>(type: "character varying(8192)", maxLength: 8192, nullable: true),
                    attempts = table.Column<int>(type: "integer", nullable: false),
                    max_attempts = table.Column<int>(type: "integer", nullable: false),
                    last_error = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: true),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    available_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    started_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    completed_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    failed_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_worker_pipeline_jobs", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_worker_pipeline_job_audit_records_job_id",
                schema: "app",
                table: "worker_pipeline_job_audit_records",
                column: "job_id");

            migrationBuilder.CreateIndex(
                name: "ix_worker_pipeline_jobs_status",
                schema: "app",
                table: "worker_pipeline_jobs",
                column: "status");

            migrationBuilder.CreateIndex(
                name: "ix_worker_pipeline_jobs_status_available_at",
                schema: "app",
                table: "worker_pipeline_jobs",
                columns: new[] { "status", "available_at_utc" });

            migrationBuilder.CreateIndex(
                name: "ux_worker_pipeline_jobs_type_upload_receipt",
                schema: "app",
                table: "worker_pipeline_jobs",
                columns: new[] { "job_type", "upload_receipt_id" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "worker_pipeline_job_audit_records",
                schema: "app");

            migrationBuilder.DropTable(
                name: "worker_pipeline_jobs",
                schema: "app");
        }
    }
}