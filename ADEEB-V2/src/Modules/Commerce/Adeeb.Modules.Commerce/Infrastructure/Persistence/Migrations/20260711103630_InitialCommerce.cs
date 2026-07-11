using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Adeeb.Modules.Commerce.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialCommerce : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "commerce");

            migrationBuilder.CreateTable(
                name: "payment_receipts",
                schema: "commerce",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    student_id = table.Column<Guid>(type: "uuid", nullable: false),
                    tariff_id = table.Column<Guid>(type: "uuid", nullable: false),
                    receipt_image_url = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    idempotency_key = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    status = table.Column<int>(type: "integer", nullable: false),
                    admin_note = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    reviewed_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    reviewed_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_payment_receipts", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "student_entitlements",
                schema: "commerce",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    student_id = table.Column<Guid>(type: "uuid", nullable: false),
                    kind = table.Column<int>(type: "integer", nullable: false),
                    status = table.Column<int>(type: "integer", nullable: false),
                    source = table.Column<int>(type: "integer", nullable: false),
                    starts_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    expires_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    idempotency_key = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    revoke_reason = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    revoked_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_student_entitlements", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "tariffs",
                schema: "commerce",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    price = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    duration_days = table.Column<short>(type: "smallint", nullable: false),
                    qr_image_url = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    status = table.Column<int>(type: "integer", nullable: false),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tariffs", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_commerce_payment_receipts_student_status",
                schema: "commerce",
                table: "payment_receipts",
                columns: new[] { "student_id", "status" });

            migrationBuilder.CreateIndex(
                name: "ix_commerce_payment_receipts_tariff_status",
                schema: "commerce",
                table: "payment_receipts",
                columns: new[] { "tariff_id", "status" });

            migrationBuilder.CreateIndex(
                name: "ux_commerce_payment_receipts_idempotency_key",
                schema: "commerce",
                table: "payment_receipts",
                column: "idempotency_key",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_commerce_student_entitlements_student_kind_status",
                schema: "commerce",
                table: "student_entitlements",
                columns: new[] { "student_id", "kind", "status" });

            migrationBuilder.CreateIndex(
                name: "ux_commerce_student_entitlements_idempotency_key",
                schema: "commerce",
                table: "student_entitlements",
                column: "idempotency_key",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_commerce_tariffs_status",
                schema: "commerce",
                table: "tariffs",
                column: "status");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "payment_receipts",
                schema: "commerce");

            migrationBuilder.DropTable(
                name: "student_entitlements",
                schema: "commerce");

            migrationBuilder.DropTable(
                name: "tariffs",
                schema: "commerce");
        }
    }
}
