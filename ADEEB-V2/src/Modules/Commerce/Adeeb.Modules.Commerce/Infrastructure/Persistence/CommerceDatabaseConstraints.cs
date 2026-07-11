namespace Adeeb.Modules.Commerce.Infrastructure.Persistence;

internal static class CommerceDatabaseConstraints
{
    public const string StudentEntitlementIdempotencyKeyUnique = "ux_commerce_student_entitlements_idempotency_key";
    public const string StudentEntitlementStudentKindStatus = "ix_commerce_student_entitlements_student_kind_status";
}
