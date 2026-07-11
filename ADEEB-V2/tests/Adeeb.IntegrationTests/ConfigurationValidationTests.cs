using Adeeb.Api.Configuration;
using Adeeb.Application.Abstractions.Students;
using Adeeb.Modules.Students.Application;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Adeeb.IntegrationTests;

public sealed class ConfigurationValidationTests
{
    [Theory]
    [InlineData("127.0.0.1", true)]
    [InlineData("::1", true)]
    [InlineData("not-an-ip", false)]
    public void Proxy_known_proxy_values_are_validated(string value, bool expected)
    {
        Assert.Equal(expected, ForwardedHeadersExtensions.TryParseProxy(value, out _));
    }

    [Theory]
    [InlineData("10.0.0.0/8", true)]
    [InlineData("192.168.0.0/16", true)]
    [InlineData("2001:db8::/32", true)]
    [InlineData("10.0.0.0/99", false)]
    [InlineData("not-a-network", false)]
    public void Proxy_known_network_values_are_validated_as_cidr(string value, bool expected)
    {
        Assert.Equal(expected, ForwardedHeadersExtensions.TryParseNetwork(value, out _));
    }

    [Fact]
    public void Api_host_resolves_real_students_provisioner()
    {
        var values = new Dictionary<string, string?>
        {
            ["ConnectionStrings__Identity"] = "Host=localhost;Port=5432;Database=adeeb_tests;Username=postgres",
            ["ConnectionStrings__AcademicCatalog"] = "Host=localhost;Port=5432;Database=adeeb_tests;Username=postgres",
            ["ConnectionStrings__QuestionBank"] = "Host=localhost;Port=5432;Database=adeeb_tests;Username=postgres",
            ["ConnectionStrings__Students"] = "Host=localhost;Port=5432;Database=adeeb_tests;Username=postgres",
            ["ConnectionStrings__Commerce"] = "Host=localhost;Port=5432;Database=adeeb_tests;Username=postgres",
            ["DatabaseInitialization__AutoMigrate"] = "false",
            ["DatabaseInitialization__Seed"] = "false",
            ["Jwt__Issuer"] = "https://tests.adeeb.tj",
            ["Jwt__Audience"] = "adeeb-tests",
            ["Jwt__SigningKey"] = "integration-tests-signing-key-32-bytes"
        };
        var previous = values.ToDictionary(x => x.Key, x => Environment.GetEnvironmentVariable(x.Key));
        try
        {
            foreach (var (key, value) in values)
            {
                Environment.SetEnvironmentVariable(key, value);
            }

            using var factory = new WebApplicationFactory<Program>()
                .WithWebHostBuilder(builder => builder.UseEnvironment("Testing"));
            using var scope = factory.Services.CreateScope();
            var provisioner = scope.ServiceProvider.GetRequiredService<IStudentRegistrationProvisioner>();

            Assert.IsType<StudentsService>(provisioner);
        }
        finally
        {
            foreach (var (key, value) in previous)
            {
                Environment.SetEnvironmentVariable(key, value);
            }
        }
    }
}
