namespace Adeeb.Modules.Commerce.Infrastructure.Persistence;

internal static class CommerceDatabaseConstraints
{
    public const string TariffPriceValid = "ck_commerce_tariffs_price_valid";
    public const string ReceiptPriceSnapshotValid = "ck_commerce_receipts_price_snapshot_valid";
    public const string StudentEntitlementIdempotencyKeyUnique = "ux_commerce_student_entitlements_idempotency_key";
    public const string StudentEntitlementStudentKindStatus = "ix_commerce_student_entitlements_student_kind_status";
    public const string PaymentReceiptIdempotencyScopeUnique = "ux_commerce_payment_receipts_student_idempotency_key";
    public const string StudentEntitlementSourcePaymentReceiptUnique = "ux_commerce_entitlements_source_payment_receipt_id";
}
