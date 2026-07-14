using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Adeeb.Modules.Commerce.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class EnforceCommerceAmountPrecision : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<decimal>(
                name: "price",
                schema: "commerce",
                table: "tariffs",
                type: "numeric",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(18,2)",
                oldPrecision: 18,
                oldScale: 2);

            migrationBuilder.AlterColumn<decimal>(
                name: "price_snapshot",
                schema: "commerce",
                table: "payment_receipts",
                type: "numeric",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(18,2)",
                oldPrecision: 18,
                oldScale: 2);

            migrationBuilder.AddCheckConstraint(
                name: "ck_commerce_tariffs_price_valid",
                schema: "commerce",
                table: "tariffs",
                sql: "price > 0 AND price <= 9999999999999999.99 AND scale(price) <= 2");

            migrationBuilder.AddCheckConstraint(
                name: "ck_commerce_receipts_price_snapshot_valid",
                schema: "commerce",
                table: "payment_receipts",
                sql: "price_snapshot > 0 AND price_snapshot <= 9999999999999999.99 AND scale(price_snapshot) <= 2");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "ck_commerce_tariffs_price_valid",
                schema: "commerce",
                table: "tariffs");

            migrationBuilder.DropCheckConstraint(
                name: "ck_commerce_receipts_price_snapshot_valid",
                schema: "commerce",
                table: "payment_receipts");

            migrationBuilder.AlterColumn<decimal>(
                name: "price",
                schema: "commerce",
                table: "tariffs",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric");

            migrationBuilder.AlterColumn<decimal>(
                name: "price_snapshot",
                schema: "commerce",
                table: "payment_receipts",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric");
        }
    }
}
