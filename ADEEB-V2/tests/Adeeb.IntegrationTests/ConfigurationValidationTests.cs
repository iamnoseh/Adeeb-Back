using Adeeb.Api.Configuration;
using Adeeb.Application.Abstractions.Students;
using Adeeb.Modules.Students.Application;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;

namespace Adeeb.IntegrationTests;

public sealed class ConfigurationValidationTests
{
    [Theory]
    [InlineData("127.0.0.1", true)]
    [InlineData("::1", true)]
    [InlineData("not-an-ip", false)]
    public void Proxy_known_proxy_values_are_validated(string value, bool expected)
    {
        Assert.Equal(expected, Adeeb.Api.Configuration.ForwardedHeadersExtensions.TryParseProxy(value, out _));
    }

    [Theory]
    [InlineData("10.0.0.0/8", true)]
    [InlineData("192.168.0.0/16", true)]
    [InlineData("2001:db8::/32", true)]
    [InlineData("10.0.0.0/99", false)]
    [InlineData("not-a-network", false)]
    public void Proxy_known_network_values_are_validated_as_cidr(string value, bool expected)
    {
        Assert.Equal(expected, Adeeb.Api.Configuration.ForwardedHeadersExtensions.TryParseNetwork(value, out _));
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
            ["ConnectionStrings__Mmt"] = "Host=localhost;Port=5432;Database=adeeb_tests;Username=postgres",
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

    [Fact]
    public void Production_rejects_wildcard_allowed_hosts()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["AllowedHosts"] = "*" })
            .Build();
        var services = new ServiceCollection();

        var exception = Assert.Throws<InvalidOperationException>(() =>
            services.AddProductionHttp(configuration, new FakeHostEnvironment(Environments.Production)));

        Assert.Contains("AllowedHosts", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Production_http_adds_safe_correlation_and_security_headers()
    {
        var configuration = new ConfigurationBuilder().AddInMemoryCollection().Build();
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddProductionHttp(configuration, new FakeHostEnvironment("Testing"));
        await using var provider = services.BuildServiceProvider();
        var app = new ApplicationBuilder(provider);
        app.UseProductionHttp();
        app.Run(context => context.Response.StartAsync());
        var pipeline = app.Build();
        var context = new DefaultHttpContext { RequestServices = provider };
        context.Request.Headers["X-Correlation-ID"] = "request_123";
        context.Response.Body = new MemoryStream();

        await pipeline(context);

        Assert.Equal("request_123", context.TraceIdentifier);
        Assert.Equal("request_123", context.Response.Headers["X-Correlation-ID"]);
        Assert.Equal("nosniff", context.Response.Headers["X-Content-Type-Options"]);
        Assert.Equal("DENY", context.Response.Headers["X-Frame-Options"]);
        Assert.Contains("frame-ancestors 'none'", context.Response.Headers["Content-Security-Policy"].ToString(), StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("/swagger")]
    [InlineData("/swagger/index.html")]
    [InlineData("/openapi/v2.json")]
    public async Task Production_http_relaxes_csp_for_swagger_and_openapi(string path)
    {
        var configuration = new ConfigurationBuilder().AddInMemoryCollection().Build();
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddProductionHttp(configuration, new FakeHostEnvironment("Testing"));
        await using var provider = services.BuildServiceProvider();
        var app = new ApplicationBuilder(provider);
        app.UseProductionHttp();
        app.Run(context => context.Response.StartAsync());
        var pipeline = app.Build();
        var context = new DefaultHttpContext { RequestServices = provider };
        context.Request.Path = path;
        context.Response.Body = new MemoryStream();

        await pipeline(context);

        var csp = context.Response.Headers["Content-Security-Policy"].ToString();
        Assert.Contains("default-src 'self'", csp, StringComparison.Ordinal);
        Assert.Contains("script-src 'self' 'unsafe-inline'", csp, StringComparison.Ordinal);
        Assert.Contains("style-src 'self' 'unsafe-inline'", csp, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("")]
    [InlineData("contains spaces")]
    [InlineData("contains/slash")]
    public void Unsafe_correlation_ids_are_rejected(string value)
    {
        Assert.False(ProductionHttpExtensions.IsSafeCorrelationId(value));
    }

    private sealed class FakeHostEnvironment(string environmentName) : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = environmentName;
        public string ApplicationName { get; set; } = "Adeeb.IntegrationTests";
        public string ContentRootPath { get; set; } = AppContext.BaseDirectory;
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }
}
