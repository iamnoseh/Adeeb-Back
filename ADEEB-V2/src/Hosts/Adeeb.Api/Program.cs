using System.Globalization;
using System.IdentityModel.Tokens.Jwt;
using System.Threading.RateLimiting;
using Adeeb.Application.Abstractions.Localization;
using Adeeb.Infrastructure;
using Adeeb.Modules.Identity;
using Adeeb.Modules.Identity.Endpoints;
using Adeeb.Modules.Identity.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Localization;
using Microsoft.EntityFrameworkCore;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

JwtSecurityTokenHandler.DefaultInboundClaimTypeMap.Clear();

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddAdeebInfrastructure();
builder.Services.AddIdentityModule(builder.Configuration);
builder.Services.AddAuthorization();
builder.Services.AddProblemDetails();

builder.Services.Configure<RequestLocalizationOptions>(options =>
{
    var cultures = SupportedLanguageExtensions.SupportedCultures.Select(x => new CultureInfo(x)).ToList();
    options.DefaultRequestCulture = new RequestCulture(SupportedLanguageExtensions.DefaultCulture);
    options.SupportedCultures = cultures;
    options.SupportedUICultures = cultures;
    options.RequestCultureProviders =
    [
        new CustomRequestCultureProvider(context =>
        {
            var value = context.Request.Headers["X-Adeeb-Language"].FirstOrDefault();
            return SupportedLanguageExtensions.TryParseCulture(value, out var language)
                ? Task.FromResult<ProviderCultureResult?>(new ProviderCultureResult(language.ToCultureCode()))
                : Task.FromResult<ProviderCultureResult?>(null);
        }),
        new AcceptLanguageHeaderRequestCultureProvider()
    ];
});

builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.OnRejected = async (context, token) =>
    {
        var localizer = context.HttpContext.RequestServices.GetRequiredService<IMessageLocalizer>();
        await Results.Problem(
            title: localizer["RateLimit.TooManyRequests"],
            statusCode: StatusCodes.Status429TooManyRequests,
            type: "https://api.adeeb.tj/errors/rate-limit",
            extensions: new Dictionary<string, object?>
            {
                ["code"] = "rate_limit.too_many_requests",
                ["traceId"] = context.HttpContext.TraceIdentifier
            }).ExecuteAsync(context.HttpContext);
    };

    options.AddPolicy("auth-login", http => Fixed(http, "login", 5, TimeSpan.FromMinutes(1)));
    options.AddPolicy("auth-register", http => Fixed(http, "register", 3, TimeSpan.FromMinutes(5)));
    options.AddPolicy("auth-refresh", http => Fixed(http, "refresh", 20, TimeSpan.FromMinutes(1)));
    options.AddPolicy("auth-change-password", http => Fixed(http, "change-password", 3, TimeSpan.FromMinutes(10)));
});

builder.Services.AddOpenTelemetry()
    .ConfigureResource(resource => resource.AddService("Adeeb.Api"))
    .WithTracing(tracing => tracing
        .AddAspNetCoreInstrumentation()
        .AddHttpClientInstrumentation());

var app = builder.Build();

app.UseExceptionHandler();

app.UseRequestLocalization();
app.UseAuthentication();
app.Use(async (context, next) =>
{
    if (context.User.Identity?.IsAuthenticated == true)
    {
        var userIdValue = context.User.FindFirst("sub")?.Value
            ?? context.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (Guid.TryParse(userIdValue, out var userId))
        {
            var db = context.RequestServices.GetRequiredService<IdentityDbContext>();
            var language = await db.Users
                .Where(x => x.Id == userId)
                .Select(x => x.PreferredLanguage)
                .SingleOrDefaultAsync(context.RequestAborted);
            var culture = new CultureInfo(language.ToCultureCode());
            CultureInfo.CurrentCulture = culture;
            CultureInfo.CurrentUICulture = culture;
        }
    }

    await next();
});
app.UseAuthorization();
app.UseRateLimiter();

app.MapGet("/health/live", () => Results.Ok(new { status = "live" }));
app.MapGet("/health/ready", async (IdentityDbContext db, CancellationToken ct) =>
    await db.Database.CanConnectAsync(ct)
        ? Results.Ok(new { status = "ready" })
        : Results.Problem(statusCode: StatusCodes.Status503ServiceUnavailable, title: "PostgreSQL is not reachable"));

app.MapIdentityEndpoints();

app.Run();

static RateLimitPartition<string> Fixed(HttpContext http, string name, int permits, TimeSpan window)
{
    var ip = http.Connection.RemoteIpAddress?.ToString() ?? "unknown";
    var user = http.User.Identity?.IsAuthenticated == true
        ? http.User.FindFirst("sub")?.Value ?? "anonymous"
        : "anonymous";
    var partition = $"{name}:{ip}:{user}";
    return RateLimitPartition.GetFixedWindowLimiter(partition, _ => new FixedWindowRateLimiterOptions
    {
        PermitLimit = permits,
        QueueLimit = 0,
        Window = window,
        AutoReplenishment = true
    });
}

public partial class Program;
