using Adeeb.Application.Abstractions.Authorization;
using Adeeb.Modules.Identity.Domain.Users;
using Adeeb.Modules.Identity.Infrastructure.Authentication;

namespace Adeeb.Identity.Tests;

public sealed class RolePermissionMappingTests
{
    [Fact]
    public void Finance_and_support_roles_have_separated_commerce_permissions()
    {
        var finance = RolePermissionMapping.GetPermissions(UserRole.FinanceAdmin);
        var support = RolePermissionMapping.GetPermissions(UserRole.SupportAdmin);
        var content = RolePermissionMapping.GetPermissions(UserRole.ContentAdmin);

        Assert.Contains(Permissions.Commerce.ReviewPaymentReceipts, finance);
        Assert.Contains(Permissions.Commerce.ViewPaymentReceipts, support);
        Assert.DoesNotContain(Permissions.Commerce.ReviewPaymentReceipts, support);
        Assert.DoesNotContain(Permissions.Commerce.ViewPaymentReceipts, content);
        Assert.DoesNotContain(Permissions.Commerce.ReviewPaymentReceipts, content);
    }

    [Fact]
    public void SuperAdmin_has_every_registered_permission()
    {
        Assert.Equal(Permissions.All.Order(), RolePermissionMapping.GetPermissions(UserRole.SuperAdmin).Order());
    }

    [Fact]
    public void Mmt_management_is_limited_to_admin_and_super_admin()
    {
        Assert.Contains(Permissions.Mmt.Manage, RolePermissionMapping.GetPermissions(UserRole.Admin));
        Assert.Contains(Permissions.Mmt.Import, RolePermissionMapping.GetPermissions(UserRole.Admin));
        Assert.Contains(Permissions.Mmt.Manage, RolePermissionMapping.GetPermissions(UserRole.SuperAdmin));
        Assert.DoesNotContain(Permissions.Mmt.Manage, RolePermissionMapping.GetPermissions(UserRole.ContentAdmin));
        Assert.DoesNotContain(Permissions.Mmt.Manage, RolePermissionMapping.GetPermissions(UserRole.User));
    }
}
