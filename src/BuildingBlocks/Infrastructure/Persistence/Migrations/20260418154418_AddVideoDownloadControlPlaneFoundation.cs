using System;

using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BuildingBlocks.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddVideoDownloadControlPlaneFoundation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "video_download_audit_records",
                schema: "app",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    download_intent_id = table.Column<Guid>(type: "uuid", nullable: true),
                    download_receipt_id = table.Column<Guid>(type: "uuid", nullable: true),
                    category = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    action = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    payload_json = table.Column<string>(type: "character varying(4096)", maxLength: 4096, nullable: false),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_video_download_audit_records", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "video_download_intents",
                schema: "app",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    device_id = table.Column<Guid>(type: "uuid", nullable: true),
                    group_node_id = table.Column<Guid>(type: "uuid", nullable: false),
                    video_asset_id = table.Column<Guid>(type: "uuid", nullable: false),
                    external_video_id = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    business_object_key = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    status = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    site_provider = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    download_url = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: false),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    expires_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    consumed_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    rejected_reason = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_video_download_intents", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "video_download_receipts",
                schema: "app",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    download_intent_id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    device_id = table.Column<Guid>(type: "uuid", nullable: true),
                    external_video_id = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    client_receipt_key = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    downloaded_bytes = table.Column<long>(type: "bigint", nullable: true),
                    status = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    downloaded_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    accepted_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_video_download_receipts", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_video_download_audit_records_intent_id",
                schema: "app",
                table: "video_download_audit_records",
                column: "download_intent_id");

            migrationBuilder.CreateIndex(
                name: "ix_video_download_audit_records_receipt_id",
                schema: "app",
                table: "video_download_audit_records",
                column: "download_receipt_id");

            migrationBuilder.CreateIndex(
                name: "ix_video_download_intents_external_video_id",
                schema: "app",
                table: "video_download_intents",
                column: "external_video_id");

            migrationBuilder.CreateIndex(
                name: "ix_video_download_intents_status",
                schema: "app",
                table: "video_download_intents",
                column: "status");

            migrationBuilder.CreateIndex(
                name: "ix_video_download_intents_user_business_created",
                schema: "app",
                table: "video_download_intents",
                columns: new[] { "user_id", "business_object_key", "created_at_utc" });

            migrationBuilder.CreateIndex(
                name: "ix_video_download_receipts_intent_client_key",
                schema: "app",
                table: "video_download_receipts",
                columns: new[] { "download_intent_id", "client_receipt_key" });

            migrationBuilder.CreateIndex(
                name: "ix_video_download_receipts_intent_id",
                schema: "app",
                table: "video_download_receipts",
                column: "download_intent_id");

            migrationBuilder.CreateIndex(
                name: "ix_video_download_receipts_user_accepted",
                schema: "app",
                table: "video_download_receipts",
                columns: new[] { "user_id", "accepted_at_utc" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "video_download_audit_records",
                schema: "app");

            migrationBuilder.DropTable(
                name: "video_download_intents",
                schema: "app");

            migrationBuilder.DropTable(
                name: "video_download_receipts",
                schema: "app");
        }
    }
}