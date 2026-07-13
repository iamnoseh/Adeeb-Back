using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Adeeb.Modules.Commerce.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class OptimizeCommerceReceiptQueries : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "ix_commerce_receipts_pending_created_id",
                schema: "commerce",
                table: "payment_receipts",
                columns: new[] { "created_at_utc", "id" },
                descending: new bool[0],
                filter: "status = 1");

            migrationBuilder.CreateIndex(
                name: "ix_commerce_receipts_reviewer_reviewed",
                schema: "commerce",
                table: "payment_receipts",
                columns: new[] { "reviewed_by_user_id", "reviewed_at_utc" },
                descending: new[] { false, true },
                filter: "reviewed_by_user_id IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ix_commerce_receipts_status_created_id",
                schema: "commerce",
                table: "payment_receipts",
                columns: new[] { "status", "created_at_utc", "id" },
                descending: new[] { false, true, true });

            migrationBuilder.CreateIndex(
                name: "ix_commerce_receipts_student_created_id",
                schema: "commerce",
                table: "payment_receipts",
                columns: new[] { "student_id", "created_at_utc", "id" },
                descending: new[] { false, true, true });

            migrationBuilder.CreateIndex(
                name: "ix_commerce_receipts_student_status_created_id",
                schema: "commerce",
                table: "payment_receipts",
                columns: new[] { "student_id", "status", "created_at_utc", "id" },
                descending: new[] { false, false, true, true });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_commerce_receipts_pending_created_id",
                schema: "commerce",
                table: "payment_receipts");

            migrationBuilder.DropIndex(
                name: "ix_commerce_receipts_reviewer_reviewed",
                schema: "commerce",
                table: "payment_receipts");

            migrationBuilder.DropIndex(
                name: "ix_commerce_receipts_status_created_id",
                schema: "commerce",
                table: "payment_receipts");

            migrationBuilder.DropIndex(
                name: "ix_commerce_receipts_student_created_id",
                schema: "commerce",
                table: "payment_receipts");

            migrationBuilder.DropIndex(
                name: "ix_commerce_receipts_student_status_created_id",
                schema: "commerce",
                table: "payment_receipts");
        }
    }
}
