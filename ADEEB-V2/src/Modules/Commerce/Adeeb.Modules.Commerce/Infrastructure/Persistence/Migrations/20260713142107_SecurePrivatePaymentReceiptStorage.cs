using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Adeeb.Modules.Commerce.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class SecurePrivatePaymentReceiptStorage : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "receipt_image_url",
                schema: "commerce",
                table: "payment_receipts",
                newName: "receipt_image_object_key");

            migrationBuilder.Sql(
                """
                UPDATE commerce.payment_receipts
                SET receipt_image_object_key =
                    'commerce/payment-receipts/legacy/' || regexp_replace(receipt_image_object_key, '^.*/', '')
                WHERE receipt_image_object_key LIKE '/uploads/commerce/receipts/%';
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "receipt_image_object_key",
                schema: "commerce",
                table: "payment_receipts",
                newName: "receipt_image_url");
        }
    }
}
