using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Adeeb.Modules.Identity.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialIdentity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "identity");

            migrationBuilder.CreateTable(
                name: "auth_sessions",
                schema: "identity",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    family_id = table.Column<Guid>(type: "uuid", nullable: false),
                    device_id = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    device_name = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    platform = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    app_version = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: true),
                    refresh_token_hash = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    expires_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    last_used_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    rotated_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    revoked_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    revoke_reason = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: true),
                    replaced_by_session_id = table.Column<Guid>(type: "uuid", nullable: true),
                    created_by_ip = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    last_used_ip = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    user_agent = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_auth_sessions", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "users",
                schema: "identity",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    email = table.Column<string>(type: "character varying(320)", maxLength: 320, nullable: false),
                    normalized_email = table.Column<string>(type: "character varying(320)", maxLength: 320, nullable: false),
                    phone_number = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    normalized_phone_number = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    password_hash = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    first_name = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    last_name = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    preferred_language = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    role = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    email_verified = table.Column<bool>(type: "boolean", nullable: false),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    last_login_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_users", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_auth_sessions_expires_at_utc_revoked_at_utc",
                schema: "identity",
                table: "auth_sessions",
                columns: new[] { "expires_at_utc", "revoked_at_utc" });

            migrationBuilder.CreateIndex(
                name: "IX_auth_sessions_family_id",
                schema: "identity",
                table: "auth_sessions",
                column: "family_id");

            migrationBuilder.CreateIndex(
                name: "IX_auth_sessions_refresh_token_hash",
                schema: "identity",
                table: "auth_sessions",
                column: "refresh_token_hash");

            migrationBuilder.CreateIndex(
                name: "IX_auth_sessions_user_id_revoked_at_utc",
                schema: "identity",
                table: "auth_sessions",
                columns: new[] { "user_id", "revoked_at_utc" });

            migrationBuilder.CreateIndex(
                name: "IX_users_normalized_email",
                schema: "identity",
                table: "users",
                column: "normalized_email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_users_normalized_phone_number",
                schema: "identity",
                table: "users",
                column: "normalized_phone_number",
                unique: true,
                filter: "normalized_phone_number IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "auth_sessions",
                schema: "identity");

            migrationBuilder.DropTable(
                name: "users",
                schema: "identity");
        }
    }
}
