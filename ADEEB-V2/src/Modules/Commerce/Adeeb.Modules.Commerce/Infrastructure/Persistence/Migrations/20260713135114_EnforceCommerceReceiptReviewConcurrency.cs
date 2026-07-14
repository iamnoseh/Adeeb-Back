using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Adeeb.Modules.Commerce.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class EnforceCommerceReceiptReviewConcurrency : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "source_payment_receipt_id",
                schema: "commerce",
                table: "student_entitlements",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "ux_commerce_entitlements_source_payment_receipt_id",
                schema: "commerce",
                table: "student_entitlements",
                column: "source_payment_receipt_id",
                unique: true,
                filter: "source_payment_receipt_id IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ux_commerce_entitlements_source_payment_receipt_id",
                schema: "commerce",
                table: "student_entitlements");

            migrationBuilder.DropColumn(
                name: "source_payment_receipt_id",
                schema: "commerce",
                table: "student_entitlements");

        }
    }
}
