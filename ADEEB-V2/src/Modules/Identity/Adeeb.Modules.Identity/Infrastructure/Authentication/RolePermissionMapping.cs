using Adeeb.Application.Abstractions.Authorization;
using Adeeb.Modules.Identity.Domain.Users;

namespace Adeeb.Modules.Identity.Infrastructure.Authentication;

public static class RolePermissionMapping
{
    public static IReadOnlyList<string> GetPermissions(UserRole role) => role switch
    {
        UserRole.SuperAdmin => Permissions.All,
        UserRole.Admin =>
        [
            Permissions.Students.View,
            Permissions.Students.Manage,
            Permissions.AcademicCatalog.View,
            Permissions.AcademicCatalog.Manage,
            Permissions.QuestionBank.View,
            Permissions.QuestionBank.Manage,
            Permissions.QuestionBank.Import,
            .. Permissions.Vocabulary.All,
            .. Permissions.Mmt.All
        ],
        UserRole.ContentAdmin =>
        [
            Permissions.AcademicCatalog.View,
            Permissions.AcademicCatalog.Manage,
            Permissions.QuestionBank.View,
            Permissions.QuestionBank.Manage,
            Permissions.QuestionBank.Import,
            .. Permissions.Vocabulary.All
        ],
        UserRole.FinanceAdmin => Permissions.Commerce.All,
        UserRole.SupportAdmin => [Permissions.Commerce.ViewPaymentReceipts],
        _ => []
    };
}
