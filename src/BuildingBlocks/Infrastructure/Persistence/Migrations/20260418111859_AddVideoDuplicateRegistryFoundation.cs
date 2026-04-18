using System;

using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BuildingBlocks.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddVideoDuplicateRegistryFoundation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "video_duplicate_assets",
                schema: "app",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    upload_receipt_id = table.Column<Guid>(type: "uuid", nullable: false),
                    pre_upload_check_id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    device_id = table.Column<Guid>(type: "uuid", nullable: true),
                    group_node_id = table.Column<Guid>(type: "uuid", nullable: true),
                    business_object_key = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    external_video_id = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    storage_key = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    size_bytes = table.Column<long>(type: "bigint", nullable: false),
                    byte_sha256 = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    uploaded_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    registered_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_video_duplicate_assets", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "video_duplicate_candidates",
                schema: "app",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    source_video_asset_id = table.Column<Guid>(type: "uuid", nullable: false),
                    matched_video_asset_id = table.Column<Guid>(type: "uuid", nullable: false),
                    match_kind = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    reason_code = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    decision = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    detected_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_video_duplicate_candidates", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "video_duplicate_fingerprints",
                schema: "app",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    video_asset_id = table.Column<Guid>(type: "uuid", nullable: false),
                    fingerprint_kind = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    fingerprint_value = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    size_bytes = table.Column<long>(type: "bigint", nullable: false),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_video_duplicate_fingerprints", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "video_duplicate_registry_audit_records",
                schema: "app",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    video_asset_id = table.Column<Guid>(type: "uuid", nullable: true),
                    duplicate_candidate_id = table.Column<Guid>(type: "uuid", nullable: true),
                    category = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    action = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    payload_json = table.Column<string>(type: "character varying(4096)", maxLength: 4096, nullable: false),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_video_duplicate_registry_audit_records", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_video_duplicate_assets_business_object_key",
                schema: "app",
                table: "video_duplicate_assets",
                column: "business_object_key");

            migrationBuilder.CreateIndex(
                name: "ix_video_duplicate_assets_fingerprint",
                schema: "app",
                table: "video_duplicate_assets",
                columns: new[] { "byte_sha256", "size_bytes" });

            migrationBuilder.CreateIndex(
                name: "ux_video_duplicate_assets_upload_receipt_id",
                schema: "app",
                table: "video_duplicate_assets",
                column: "upload_receipt_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_video_duplicate_candidates_matched_asset_id",
                schema: "app",
                table: "video_duplicate_candidates",
                column: "matched_video_asset_id");

            migrationBuilder.CreateIndex(
                name: "ix_video_duplicate_candidates_source_asset_id",
                schema: "app",
                table: "video_duplicate_candidates",
                column: "source_video_asset_id");

            migrationBuilder.CreateIndex(
                name: "ux_video_duplicate_candidates_pair_kind",
                schema: "app",
                table: "video_duplicate_candidates",
                columns: new[] { "source_video_asset_id", "matched_video_asset_id", "match_kind" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_video_duplicate_fingerprints_kind_value_size",
                schema: "app",
                table: "video_duplicate_fingerprints",
                columns: new[] { "fingerprint_kind", "fingerprint_value", "size_bytes" });

            migrationBuilder.CreateIndex(
                name: "ix_video_duplicate_fingerprints_video_asset_id",
                schema: "app",
                table: "video_duplicate_fingerprints",
                column: "video_asset_id");

            migrationBuilder.CreateIndex(
                name: "ix_video_duplicate_registry_audit_records_asset_id",
                schema: "app",
                table: "video_duplicate_registry_audit_records",
                column: "video_asset_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "video_duplicate_assets",
                schema: "app");

            migrationBuilder.DropTable(
                name: "video_duplicate_candidates",
                schema: "app");

            migrationBuilder.DropTable(
                name: "video_duplicate_fingerprints",
                schema: "app");

            migrationBuilder.DropTable(
                name: "video_duplicate_registry_audit_records",
                schema: "app");
        }
    }
}