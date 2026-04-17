using System;

using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace BuildingBlocks.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddGroupTreeAndDevicesFoundation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "device_registrations",
                schema: "app",
                columns: table => new
                {
                    device_id = table.Column<Guid>(type: "uuid", nullable: false),
                    platform = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    device_name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    model = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    os_version = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    is_trusted = table.Column<bool>(type: "boolean", nullable: false),
                    last_known_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    registered_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    last_seen_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_device_registrations", x => x.device_id);
                    table.ForeignKey(
                        name: "fk_device_registrations_auth_users_last_known_user_id",
                        column: x => x.last_known_user_id,
                        principalSchema: "app",
                        principalTable: "auth_users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "group_nodes",
                schema: "app",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    parent_node_id = table.Column<Guid>(type: "uuid", nullable: true),
                    code = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    depth = table.Column<int>(type: "integer", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_group_nodes", x => x.id);
                    table.ForeignKey(
                        name: "fk_group_nodes_parent_node_id",
                        column: x => x.parent_node_id,
                        principalSchema: "app",
                        principalTable: "group_nodes",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "group_admin_assignments",
                schema: "app",
                columns: table => new
                {
                    group_node_id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    assigned_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_group_admin_assignments", x => new { x.group_node_id, x.user_id });
                    table.ForeignKey(
                        name: "fk_group_admin_assignments_auth_users_user_id",
                        column: x => x.user_id,
                        principalSchema: "app",
                        principalTable: "auth_users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_group_admin_assignments_group_nodes_group_node_id",
                        column: x => x.group_node_id,
                        principalSchema: "app",
                        principalTable: "group_nodes",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                schema: "app",
                table: "group_nodes",
                columns: new[] { "id", "code", "depth", "is_active", "name", "parent_node_id" },
                values: new object[,]
                {
                    { new Guid("c15ee7fe-7c9a-4b31-8b7e-c278b59f318c"), "root", 0, true, "Root", null },
                    { new Guid("d4d74008-0ed5-4e46-b0a1-91e0628079c0"), "branch-a", 1, true, "Branch A", new Guid("c15ee7fe-7c9a-4b31-8b7e-c278b59f318c") },
                    { new Guid("d6370f1a-69c0-4d7f-8af5-81cf7d665e92"), "branch-a-sub", 2, true, "Branch A Sub", new Guid("d4d74008-0ed5-4e46-b0a1-91e0628079c0") }
                });

            migrationBuilder.CreateIndex(
                name: "IX_device_registrations_last_known_user_id",
                schema: "app",
                table: "device_registrations",
                column: "last_known_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_group_admin_assignments_user_id",
                schema: "app",
                table: "group_admin_assignments",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "IX_group_nodes_parent_node_id",
                schema: "app",
                table: "group_nodes",
                column: "parent_node_id");

            migrationBuilder.CreateIndex(
                name: "ux_group_nodes_code",
                schema: "app",
                table: "group_nodes",
                column: "code",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "device_registrations",
                schema: "app");

            migrationBuilder.DropTable(
                name: "group_admin_assignments",
                schema: "app");

            migrationBuilder.DropTable(
                name: "group_nodes",
                schema: "app");
        }
    }
}