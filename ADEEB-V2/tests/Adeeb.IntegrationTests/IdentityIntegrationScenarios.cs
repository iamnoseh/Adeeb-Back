namespace Adeeb.IntegrationTests;

public sealed class IdentityIntegrationScenarios
{
    [Fact(Skip = "Requires Docker/Testcontainers PostgreSQL. Docker CLI is not installed in this environment.")]
    public Task Register_duplicate_language_and_password_policy_scenarios() => Task.CompletedTask;

    [Fact(Skip = "Requires Docker/Testcontainers PostgreSQL. Docker CLI is not installed in this environment.")]
    public Task Login_valid_wrong_password_unknown_user_and_blocked_user_scenarios() => Task.CompletedTask;

    [Fact(Skip = "Requires Docker/Testcontainers PostgreSQL. Docker CLI is not installed in this environment.")]
    public Task Refresh_rotation_reuse_expired_and_revoked_session_scenarios() => Task.CompletedTask;

    [Fact(Skip = "Requires Docker/Testcontainers PostgreSQL. Docker CLI is not installed in this environment.")]
    public Task Concurrent_refresh_requests_cannot_both_succeed() => Task.CompletedTask;

    [Fact(Skip = "Requires Docker/Testcontainers PostgreSQL. Docker CLI is not installed in this environment.")]
    public Task Multi_device_revoke_logout_and_idor_scenarios() => Task.CompletedTask;

    [Fact(Skip = "Requires Docker/Testcontainers PostgreSQL. Docker CLI is not installed in this environment.")]
    public Task Localization_returns_tajik_russian_and_english_errors() => Task.CompletedTask;
}
