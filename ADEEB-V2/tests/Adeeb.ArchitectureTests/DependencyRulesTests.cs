using Adeeb.Modules.Identity.Domain.Users;
using Adeeb.Modules.AcademicCatalog.Domain;
using Adeeb.Modules.QuestionBank.Domain;
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

    [Fact]
    public void Academic_catalog_domain_must_not_depend_on_infrastructure_or_frameworks()
    {
        var result = Types.InAssembly(typeof(Subject).Assembly)
            .That()
            .ResideInNamespaceMatching(@"Adeeb\.Modules\.AcademicCatalog\.Domain.*")
            .ShouldNot()
            .HaveDependencyOnAny("Microsoft.EntityFrameworkCore", "Microsoft.AspNetCore", "Npgsql", "Adeeb.Modules.AcademicCatalog.Infrastructure")
            .GetResult();

        Assert.True(result.IsSuccessful, string.Join(Environment.NewLine, result.FailingTypeNames ?? []));
    }

    [Fact]
    public void Question_bank_domain_must_not_depend_on_infrastructure_or_frameworks()
    {
        var result = Types.InAssembly(typeof(Question).Assembly)
            .That()
            .ResideInNamespaceMatching(@"Adeeb\.Modules\.QuestionBank\.Domain.*")
            .ShouldNot()
            .HaveDependencyOnAny("Microsoft.EntityFrameworkCore", "Microsoft.AspNetCore", "Npgsql", "Adeeb.Modules.QuestionBank.Infrastructure")
            .GetResult();

        Assert.True(result.IsSuccessful, string.Join(Environment.NewLine, result.FailingTypeNames ?? []));
    }

    [Fact]
    public void Question_bank_must_not_depend_on_academic_catalog_infrastructure()
    {
        var result = Types.InAssembly(typeof(Question).Assembly)
            .ShouldNot()
            .HaveDependencyOn("Adeeb.Modules.AcademicCatalog.Infrastructure")
            .GetResult();

        Assert.True(result.IsSuccessful, string.Join(Environment.NewLine, result.FailingTypeNames ?? []));
    }
}
