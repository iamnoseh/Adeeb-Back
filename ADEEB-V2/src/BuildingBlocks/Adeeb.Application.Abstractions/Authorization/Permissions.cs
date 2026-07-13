namespace Adeeb.Application.Abstractions.Authorization;

public static class AdeebClaimNames
{
    public const string Permission = "permission";
}

public static class Permissions
{
    public static class Commerce
    {
        public const string ViewTariffs = "commerce.tariffs.view";
        public const string ManageTariffs = "commerce.tariffs.manage";
        public const string ViewPaymentReceipts = "commerce.payment_receipts.view";
        public const string ReviewPaymentReceipts = "commerce.payment_receipts.review";
        public const string GrantPremium = "commerce.entitlements.grant";

        public static readonly IReadOnlyList<string> All =
        [
            ViewTariffs,
            ManageTariffs,
            ViewPaymentReceipts,
            ReviewPaymentReceipts,
            GrantPremium
        ];
    }

    public static class QuestionBank
    {
        public const string View = "question_bank.view";
        public const string Manage = "question_bank.manage";
        public const string Import = "question_bank.import";
    }

    public static class Students
    {
        public const string View = "students.view";
        public const string Manage = "students.manage";
    }

    public static class AcademicCatalog
    {
        public const string View = "academic_catalog.view";
        public const string Manage = "academic_catalog.manage";
    }

    public static readonly IReadOnlyList<string> All =
    [
        .. Commerce.All,
        QuestionBank.View,
        QuestionBank.Manage,
        QuestionBank.Import,
        Students.View,
        Students.Manage,
        AcademicCatalog.View,
        AcademicCatalog.Manage
    ];
}
