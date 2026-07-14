using Adeeb.Modules.Identity.Domain.Users;
using Adeeb.Modules.AcademicCatalog.Domain;
using Adeeb.Modules.Commerce.Domain.Entitlements;
using Adeeb.Modules.Commerce.Endpoints;
using Adeeb.Modules.QuestionBank.Domain;
using Adeeb.Modules.Students.Domain.Students;
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
                "System.IdentityModel.Tokens.Jwt",
                "Adeeb.Modules.Identity.Infrastructure",
                "Adeeb.Modules.AcademicCatalog.Infrastructure",
                "Adeeb.Modules.QuestionBank.Infrastructure")
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
                "Adeeb.Modules.Identity",
                "Adeeb.Modules.AcademicCatalog",
                "Adeeb.Modules.QuestionBank",
                "Adeeb.Modules.Students",
                "Adeeb.Modules.Commerce")
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
            .HaveDependencyOnAny(
                "Microsoft.EntityFrameworkCore",
                "Microsoft.AspNetCore",
                "Npgsql",
                "System.IdentityModel.Tokens.Jwt",
                "Adeeb.Modules.AcademicCatalog.Infrastructure",
                "Adeeb.Modules.Identity.Infrastructure",
                "Adeeb.Modules.QuestionBank.Infrastructure")
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
            .HaveDependencyOnAny(
                "Microsoft.EntityFrameworkCore",
                "Microsoft.AspNetCore",
                "Npgsql",
                "System.IdentityModel.Tokens.Jwt",
                "Adeeb.Modules.QuestionBank.Infrastructure",
                "Adeeb.Modules.Identity.Infrastructure",
                "Adeeb.Modules.AcademicCatalog.Infrastructure")
            .GetResult();

        Assert.True(result.IsSuccessful, string.Join(Environment.NewLine, result.FailingTypeNames ?? []));
    }

    [Fact]
    public void Students_domain_must_not_depend_on_infrastructure_or_frameworks()
    {
        var result = Types.InAssembly(typeof(Student).Assembly)
            .That()
            .ResideInNamespaceMatching(@"Adeeb\.Modules\.Students\.Domain.*")
            .ShouldNot()
            .HaveDependencyOnAny(
                "Microsoft.EntityFrameworkCore",
                "Microsoft.AspNetCore",
                "Npgsql",
                "System.IdentityModel.Tokens.Jwt",
                "Adeeb.Modules.Students.Infrastructure",
                "Adeeb.Modules.Commerce.Infrastructure",
                "Adeeb.Modules.Identity.Infrastructure",
                "Adeeb.Modules.AcademicCatalog.Infrastructure",
                "Adeeb.Modules.QuestionBank.Infrastructure")
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

    [Fact]
    public void Commerce_domain_must_not_depend_on_infrastructure_or_frameworks()
    {
        var result = Types.InAssembly(typeof(StudentEntitlement).Assembly)
            .That()
            .ResideInNamespaceMatching(@"Adeeb\.Modules\.Commerce\.Domain.*")
            .ShouldNot()
            .HaveDependencyOnAny(
                "Microsoft.EntityFrameworkCore",
                "Microsoft.AspNetCore",
                "Npgsql",
                "System.IdentityModel.Tokens.Jwt",
                "Adeeb.Modules.Commerce.Infrastructure",
                "Adeeb.Modules.Identity.Infrastructure",
                "Adeeb.Modules.AcademicCatalog.Infrastructure",
                "Adeeb.Modules.QuestionBank.Infrastructure",
                "Adeeb.Modules.Students.Infrastructure")
            .GetResult();

        Assert.True(result.IsSuccessful, string.Join(Environment.NewLine, result.FailingTypeNames ?? []));
    }

    [Fact]
    public void Identity_module_must_not_depend_on_other_modules()
    {
        var result = Types.InAssembly(typeof(User).Assembly)
            .ShouldNot()
            .HaveDependencyOnAny("Adeeb.Modules.AcademicCatalog", "Adeeb.Modules.QuestionBank")
            .GetResult();

        Assert.True(result.IsSuccessful, string.Join(Environment.NewLine, result.FailingTypeNames ?? []));
    }

    [Fact]
    public void AcademicCatalog_module_must_not_depend_on_other_modules()
    {
        var result = Types.InAssembly(typeof(Subject).Assembly)
            .ShouldNot()
            .HaveDependencyOnAny("Adeeb.Modules.Identity", "Adeeb.Modules.QuestionBank")
            .GetResult();

        Assert.True(result.IsSuccessful, string.Join(Environment.NewLine, result.FailingTypeNames ?? []));
    }

    [Fact]
    public void QuestionBank_module_must_not_depend_on_Identity_module()
    {
        var result = Types.InAssembly(typeof(Question).Assembly)
            .ShouldNot()
            .HaveDependencyOn("Adeeb.Modules.Identity")
            .GetResult();

        Assert.True(result.IsSuccessful, string.Join(Environment.NewLine, result.FailingTypeNames ?? []));
    }

    [Fact]
    public void Students_module_must_not_depend_on_other_module_infrastructure()
    {
        var result = Types.InAssembly(typeof(Student).Assembly)
            .ShouldNot()
            .HaveDependencyOnAny(
                "Adeeb.Modules.Identity.Infrastructure",
                "Adeeb.Modules.AcademicCatalog.Infrastructure",
                "Adeeb.Modules.QuestionBank.Infrastructure")
            .GetResult();

        Assert.True(result.IsSuccessful, string.Join(Environment.NewLine, result.FailingTypeNames ?? []));
    }

    [Fact]
    public void Other_modules_must_not_depend_on_students_infrastructure()
    {
        var result = Types.InAssemblies([
                typeof(User).Assembly,
                typeof(Subject).Assembly,
                typeof(Question).Assembly,
                typeof(StudentEntitlement).Assembly
            ])
            .ShouldNot()
            .HaveDependencyOn("Adeeb.Modules.Students.Infrastructure")
            .GetResult();

        Assert.True(result.IsSuccessful, string.Join(Environment.NewLine, result.FailingTypeNames ?? []));
    }

    [Fact]
    public void Other_modules_must_not_depend_on_commerce_infrastructure()
    {
        var result = Types.InAssemblies([
                typeof(User).Assembly,
                typeof(Subject).Assembly,
                typeof(Question).Assembly,
                typeof(Student).Assembly
            ])
            .ShouldNot()
            .HaveDependencyOn("Adeeb.Modules.Commerce.Infrastructure")
            .GetResult();

        Assert.True(result.IsSuccessful, string.Join(Environment.NewLine, result.FailingTypeNames ?? []));
    }

    [Fact]
    public void Commerce_endpoints_must_use_focused_use_cases_and_not_persistence()
    {
        var result = Types.InAssembly(typeof(CommerceEndpoints).Assembly)
            .That()
            .ResideInNamespaceMatching(@"Adeeb\.Modules\.Commerce\.Endpoints.*")
            .ShouldNot()
            .HaveDependencyOnAny(
                "Microsoft.EntityFrameworkCore",
                "Adeeb.Modules.Commerce.Infrastructure.Persistence",
                "Adeeb.Modules.Commerce.Application.CommerceService")
            .GetResult();

        Assert.True(result.IsSuccessful, string.Join(Environment.NewLine, result.FailingTypeNames ?? []));
    }

    [Fact]
    public void Commerce_application_must_not_depend_on_endpoints()
    {
        var result = Types.InAssembly(typeof(CommerceEndpoints).Assembly)
            .That()
            .ResideInNamespaceMatching(@"Adeeb\.Modules\.Commerce\.Application.*")
            .ShouldNot()
            .HaveDependencyOn("Adeeb.Modules.Commerce.Endpoints")
            .GetResult();

        Assert.True(result.IsSuccessful, string.Join(Environment.NewLine, result.FailingTypeNames ?? []));
    }

    [Fact]
    public void Focused_commerce_use_cases_must_not_delegate_to_legacy_commerce_service()
    {
        var result = Types.InAssembly(typeof(CommerceEndpoints).Assembly)
            .That()
            .ResideInNamespaceMatching(@"Adeeb\.Modules\.Commerce\.Application\.(Tariffs|Entitlements|PaymentReceipts).*")
            .ShouldNot()
            .HaveDependencyOn("Adeeb.Modules.Commerce.Application.CommerceService")
            .GetResult();

        Assert.True(result.IsSuccessful, string.Join(Environment.NewLine, result.FailingTypeNames ?? []));
    }
}
