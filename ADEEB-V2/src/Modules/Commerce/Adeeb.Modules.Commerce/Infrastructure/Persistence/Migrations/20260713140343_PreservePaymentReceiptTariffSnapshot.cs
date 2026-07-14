using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Adeeb.Modules.Commerce.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class PreservePaymentReceiptTariffSnapshot : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "currency_snapshot",
                schema: "commerce",
                table: "payment_receipts",
                type: "character varying(3)",
                maxLength: 3,
                nullable: true);

            migrationBuilder.AddColumn<short>(
                name: "duration_days_snapshot",
                schema: "commerce",
                table: "payment_receipts",
                type: "smallint",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "price_snapshot",
                schema: "commerce",
                table: "payment_receipts",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "tariff_name_snapshot",
                schema: "commerce",
                table: "payment_receipts",
                type: "character varying(160)",
                maxLength: 160,
                nullable: true);

            migrationBuilder.Sql(
                """
                UPDATE commerce.payment_receipts AS receipt
                SET tariff_name_snapshot = tariff.name,
                    price_snapshot = tariff.price,
                    currency_snapshot = upper(trim(tariff.currency)),
                    duration_days_snapshot = tariff.duration_days
                FROM commerce.tariffs AS tariff
                WHERE tariff.id = receipt.tariff_id;
                """);

            migrationBuilder.Sql(
                """
                DO $$
                BEGIN
                    IF EXISTS (
                        SELECT 1
                        FROM commerce.payment_receipts
                        WHERE tariff_name_snapshot IS NULL
                           OR price_snapshot IS NULL
                           OR currency_snapshot IS NULL
                           OR duration_days_snapshot IS NULL)
                    THEN
                        RAISE EXCEPTION 'Cannot backfill payment receipt tariff snapshots';
                    END IF;
                END $$;
                """);

            migrationBuilder.AlterColumn<string>(
                name: "currency_snapshot",
                schema: "commerce",
                table: "payment_receipts",
                type: "character varying(3)",
                maxLength: 3,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(3)",
                oldMaxLength: 3,
                oldNullable: true);

            migrationBuilder.AlterColumn<short>(
                name: "duration_days_snapshot",
                schema: "commerce",
                table: "payment_receipts",
                type: "smallint",
                nullable: false,
                oldClrType: typeof(short),
                oldType: "smallint",
                oldNullable: true);

            migrationBuilder.AlterColumn<decimal>(
                name: "price_snapshot",
                schema: "commerce",
                table: "payment_receipts",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(18,2)",
                oldPrecision: 18,
                oldScale: 2,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "tariff_name_snapshot",
                schema: "commerce",
                table: "payment_receipts",
                type: "character varying(160)",
                maxLength: 160,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(160)",
                oldMaxLength: 160,
                oldNullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "currency_snapshot",
                schema: "commerce",
                table: "payment_receipts");

            migrationBuilder.DropColumn(
                name: "duration_days_snapshot",
                schema: "commerce",
                table: "payment_receipts");

            migrationBuilder.DropColumn(
                name: "price_snapshot",
                schema: "commerce",
                table: "payment_receipts");

            migrationBuilder.DropColumn(
                name: "tariff_name_snapshot",
                schema: "commerce",
                table: "payment_receipts");
        }
    }
}
