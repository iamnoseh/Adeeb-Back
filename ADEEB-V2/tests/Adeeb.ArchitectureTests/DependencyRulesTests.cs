using Adeeb.Modules.Identity.Domain.Users;
using Adeeb.SharedKernel.Results;
using NetArchTest.Rules;

namespace Adeeb.ArchitectureTests;

public sealed class DependencyRulesTests
{
    [Fact]
    public void Identity_domain_must_not_depend_on_infrastructure_or_frameworks()
    {
        var result = Types.InAssembly(typeof(User).Assembly)
            .That()
            .ResideInNamespaceMatching(@"Adeeb\.Modules\.Identity\.Domain.*")
            .ShouldNot()
            .HaveDependencyOnAny(
                "Microsoft.EntityFrameworkCore",
                "Microsoft.AspNetCore",
                "Npgsql",
                "Adeeb.Modules.Identity.Infrastructure")
            .GetResult();

        Assert.True(result.IsSuccessful, string.Join(Environment.NewLine, result.FailingTypeNames ?? []));
    }

    [Fact]
    public void Shared_kernel_must_not_depend_on_framework_or_module_infrastructure()
    {
        var result = Types.InAssembly(typeof(Result).Assembly)
            .ShouldNot()
            .HaveDependencyOnAny(
                "Microsoft.EntityFrameworkCore",
                "Microsoft.AspNetCore",
                "Npgsql",
                "Adeeb.Modules.Identity.Infrastructure")
            .GetResult();

        Assert.True(result.IsSuccessful, string.Join(Environment.NewLine, result.FailingTypeNames ?? []));
    }
}
