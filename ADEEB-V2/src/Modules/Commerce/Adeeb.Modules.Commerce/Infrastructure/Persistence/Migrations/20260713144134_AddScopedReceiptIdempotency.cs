using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Adeeb.Modules.Commerce.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddScopedReceiptIdempotency : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ux_commerce_payment_receipts_idempotency_key",
                schema: "commerce",
                table: "payment_receipts");

            migrationBuilder.AddColumn<string>(
                name: "request_fingerprint",
                schema: "commerce",
                table: "payment_receipts",
                type: "character varying(128)",
                maxLength: 128,
                nullable: true);

            migrationBuilder.Sql(
                """
                UPDATE commerce.payment_receipts
                SET request_fingerprint = 'legacy:' || id::text
                WHERE request_fingerprint IS NULL;
                """);

            migrationBuilder.AlterColumn<string>(
                name: "request_fingerprint",
                schema: "commerce",
                table: "payment_receipts",
                type: "character varying(128)",
                maxLength: 128,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(128)",
                oldMaxLength: 128,
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "ux_commerce_payment_receipts_student_idempotency_key",
                schema: "commerce",
                table: "payment_receipts",
                columns: new[] { "student_id", "idempotency_key" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ux_commerce_payment_receipts_student_idempotency_key",
                schema: "commerce",
                table: "payment_receipts");

            migrationBuilder.DropColumn(
                name: "request_fingerprint",
                schema: "commerce",
                table: "payment_receipts");

            migrationBuilder.CreateIndex(
                name: "ux_commerce_payment_receipts_idempotency_key",
                schema: "commerce",
                table: "payment_receipts",
                column: "idempotency_key",
                unique: true);
        }
    }
}
