using Adeeb.Modules.Identity.Contracts;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Testcontainers.PostgreSql;
using Npgsql;

namespace Adeeb.IntegrationTests;

public sealed class AdeebApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder("postgres:17-alpine")
        .WithDatabase("adeeb_tests")
        .WithUsername("adeeb")
        .WithPassword("adeeb-tests-password")
        .Build();

    public string ConnectionString => _postgres.GetConnectionString();

    public async Task InitializeAsync() => await _postgres.StartAsync();

    public new async Task DisposeAsync()
    {
        await _postgres.DisposeAsync();
        await base.DisposeAsync();
    }

    public async Task ResetDatabaseAsync(CancellationToken cancellationToken = default)
    {
        const string sql = """
            DO $$
            DECLARE table_list text;
            BEGIN
                SELECT string_agg(format('%I.%I', schemaname, tablename), ', ')
                INTO table_list
                FROM pg_tables
                WHERE schemaname = ANY (ARRAY['identity', 'academic', 'question_bank', 'students', 'commerce', 'mmt', 'vocabulary']);

                IF table_list IS NOT NULL THEN
                    EXECUTE 'TRUNCATE TABLE ' || table_list || ' RESTART IDENTITY CASCADE';
                END IF;
            END $$;
            """;
        await using var connection = new NpgsqlConnection(ConnectionString);
        await connection.OpenAsync(cancellationToken);
        await using var command = new NpgsqlCommand(sql, connection);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.ConfigureAppConfiguration((_, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:Identity"] = ConnectionString,
                ["ConnectionStrings:AcademicCatalog"] = ConnectionString,
                ["ConnectionStrings:QuestionBank"] = ConnectionString,
                ["ConnectionStrings:Students"] = ConnectionString,
                ["ConnectionStrings:Commerce"] = ConnectionString,
                ["ConnectionStrings:Mmt"] = ConnectionString,
                ["ConnectionStrings:Vocabulary"] = ConnectionString,
                ["DatabaseInitialization:AutoMigrate"] = "true",
                ["DatabaseInitialization:Seed"] = "false",
                ["Jwt:Issuer"] = "https://tests.adeeb.tj",
                ["Jwt:Audience"] = "adeeb-tests",
                ["Jwt:SigningKey"] = "integration-tests-signing-key-32-bytes",
                ["Jwt:AccessTokenMinutes"] = "10",
                ["RefreshTokens:LifetimeDays"] = "30",
                ["RefreshTokens:TokenBytes"] = "64",
                ["PrivateFileStorage:Provider"] = "Local",
                ["PrivateFileStorage:LocalRoot"] = Path.Combine(Path.GetTempPath(), "adeeb-integration-private", Guid.NewGuid().ToString("N")),
                ["Proxy:ForwardLimit"] = "1",
                ["Proxy:KnownProxies:0"] = "127.0.0.1",
                ["Proxy:KnownNetworks:0"] = "10.0.0.0/8"
            });
        });
    }

    public static RegisterRequest RegisterRequest(string email, string language = "tg-TJ", string? phone = null) =>
        new(
            email,
            phone,
            "Strong123",
            "Test",
            "User",
            language,
            new DeviceRequest("device-1", "Test Device", "web", "1.0"));

    public static LoginRequest LoginRequest(string email) =>
        new(email, null, "Strong123", new DeviceRequest("device-1", "Test Device", "web", "1.0"));
}
