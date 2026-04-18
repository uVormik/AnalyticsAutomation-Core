using System;

using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BuildingBlocks.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddVideoUploadPreUploadCheckFoundation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "video_upload_pre_upload_checks",
                schema: "app",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    device_id = table.Column<Guid>(type: "uuid", nullable: true),
                    group_node_id = table.Column<Guid>(type: "uuid", nullable: true),
                    business_object_key = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    file_name = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    size_bytes = table.Column<long>(type: "bigint", nullable: false),
                    byte_sha256 = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    content_type = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    captured_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    decision = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    can_upload_to_site = table.Column<bool>(type: "boolean", nullable: false),
                    reason_code = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    existing_pre_upload_check_id = table.Column<Guid>(type: "uuid", nullable: true),
                    required_next_steps = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    site_provider = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    external_video_id = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    storage_key = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    checked_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_video_upload_pre_upload_checks", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_video_upload_pre_upload_checks_checked_at_utc",
                schema: "app",
                table: "video_upload_pre_upload_checks",
                column: "checked_at_utc");

            migrationBuilder.CreateIndex(
                name: "ix_video_upload_pre_upload_checks_fingerprint",
                schema: "app",
                table: "video_upload_pre_upload_checks",
                columns: new[] { "byte_sha256", "size_bytes" });

            migrationBuilder.CreateIndex(
                name: "ix_video_upload_pre_upload_checks_user_business_object",
                schema: "app",
                table: "video_upload_pre_upload_checks",
                columns: new[] { "user_id", "business_object_key" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "video_upload_pre_upload_checks",
                schema: "app");
        }
    }
}