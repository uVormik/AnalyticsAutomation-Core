using System;

using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace BuildingBlocks.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddAuthFoundation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "auth_permissions",
                schema: "app",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    code = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_auth_permissions", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "auth_roles",
                schema: "app",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    code = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_auth_roles", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "auth_users",
                schema: "app",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    login = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    normalized_login = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    display_name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    password_hash = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    current_group_node_id = table.Column<Guid>(type: "uuid", nullable: true),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    last_sign_in_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_auth_users", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "auth_role_permissions",
                schema: "app",
                columns: table => new
                {
                    role_id = table.Column<Guid>(type: "uuid", nullable: false),
                    permission_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_auth_role_permissions", x => new { x.role_id, x.permission_id });
                    table.ForeignKey(
                        name: "fk_auth_role_permissions_permissions_permission_id",
                        column: x => x.permission_id,
                        principalSchema: "app",
                        principalTable: "auth_permissions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_auth_role_permissions_roles_role_id",
                        column: x => x.role_id,
                        principalSchema: "app",
                        principalTable: "auth_roles",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "auth_last_active_device_accounts",
                schema: "app",
                columns: table => new
                {
                    device_id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    is_offline_restricted = table.Column<bool>(type: "boolean", nullable: false),
                    marked_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_auth_last_active_device_accounts", x => x.device_id);
                    table.ForeignKey(
                        name: "fk_auth_last_active_device_accounts_users_user_id",
                        column: x => x.user_id,
                        principalSchema: "app",
                        principalTable: "auth_users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "auth_sessions",
                schema: "app",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    device_id = table.Column<Guid>(type: "uuid", nullable: true),
                    access_token_hash = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    refresh_token_hash = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    is_offline_restricted = table.Column<bool>(type: "boolean", nullable: false),
                    issued_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    expires_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    refresh_expires_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    revoked_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_auth_sessions", x => x.id);
                    table.ForeignKey(
                        name: "fk_auth_sessions_users_user_id",
                        column: x => x.user_id,
                        principalSchema: "app",
                        principalTable: "auth_users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "auth_user_roles",
                schema: "app",
                columns: table => new
                {
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    role_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_auth_user_roles", x => new { x.user_id, x.role_id });
                    table.ForeignKey(
                        name: "fk_auth_user_roles_roles_role_id",
                        column: x => x.role_id,
                        principalSchema: "app",
                        principalTable: "auth_roles",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_auth_user_roles_users_user_id",
                        column: x => x.user_id,
                        principalSchema: "app",
                        principalTable: "auth_users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                schema: "app",
                table: "auth_permissions",
                columns: new[] { "id", "code", "name" },
                values: new object[,]
                {
                    { new Guid("34d52b4e-d652-4d27-9f4a-6d6f706ce3c0"), "auth.last_active_device_account.read", "Auth Last Active Device Account Read" },
                    { new Guid("402957af-25c0-455b-a753-1576a5ecfa91"), "auth.session.sign_in", "Auth Session Sign In" },
                    { new Guid("6a6f1e27-b895-423a-af08-f0a1d8e77b0d"), "auth.session.refresh", "Auth Session Refresh" }
                });

            migrationBuilder.InsertData(
                schema: "app",
                table: "auth_roles",
                columns: new[] { "id", "code", "name" },
                values: new object[] { new Guid("7a9170bc-6a0a-4a03-a948-0f6af0e38e78"), "platform_owner", "Platform Owner" });

            migrationBuilder.InsertData(
                schema: "app",
                table: "auth_role_permissions",
                columns: new[] { "permission_id", "role_id" },
                values: new object[,]
                {
                    { new Guid("34d52b4e-d652-4d27-9f4a-6d6f706ce3c0"), new Guid("7a9170bc-6a0a-4a03-a948-0f6af0e38e78") },
                    { new Guid("402957af-25c0-455b-a753-1576a5ecfa91"), new Guid("7a9170bc-6a0a-4a03-a948-0f6af0e38e78") },
                    { new Guid("6a6f1e27-b895-423a-af08-f0a1d8e77b0d"), new Guid("7a9170bc-6a0a-4a03-a948-0f6af0e38e78") }
                });

            migrationBuilder.CreateIndex(
                name: "IX_auth_last_active_device_accounts_user_id",
                schema: "app",
                table: "auth_last_active_device_accounts",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "ux_auth_permissions_code",
                schema: "app",
                table: "auth_permissions",
                column: "code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_auth_role_permissions_permission_id",
                schema: "app",
                table: "auth_role_permissions",
                column: "permission_id");

            migrationBuilder.CreateIndex(
                name: "ux_auth_roles_code",
                schema: "app",
                table: "auth_roles",
                column: "code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_auth_sessions_user_id",
                schema: "app",
                table: "auth_sessions",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "ux_auth_sessions_access_token_hash",
                schema: "app",
                table: "auth_sessions",
                column: "access_token_hash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ux_auth_sessions_refresh_token_hash",
                schema: "app",
                table: "auth_sessions",
                column: "refresh_token_hash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_auth_user_roles_role_id",
                schema: "app",
                table: "auth_user_roles",
                column: "role_id");

            migrationBuilder.CreateIndex(
                name: "ux_auth_users_normalized_login",
                schema: "app",
                table: "auth_users",
                column: "normalized_login",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "auth_last_active_device_accounts",
                schema: "app");

            migrationBuilder.DropTable(
                name: "auth_role_permissions",
                schema: "app");

            migrationBuilder.DropTable(
                name: "auth_sessions",
                schema: "app");

            migrationBuilder.DropTable(
                name: "auth_user_roles",
                schema: "app");

            migrationBuilder.DropTable(
                name: "auth_permissions",
                schema: "app");

            migrationBuilder.DropTable(
                name: "auth_roles",
                schema: "app");

            migrationBuilder.DropTable(
                name: "auth_users",
                schema: "app");
        }
    }
}