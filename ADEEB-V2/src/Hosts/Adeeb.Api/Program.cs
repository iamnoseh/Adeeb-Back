using Adeeb.Api.Configuration;
using Adeeb.Application.Abstractions.Localization;
using Adeeb.Api.Documentation;
using Adeeb.Api.Documentation.Endpoints;
using Adeeb.Infrastructure;
using Adeeb.Modules.AcademicCatalog;
using Adeeb.Modules.AcademicCatalog.Endpoints;
using Adeeb.Modules.AcademicCatalog.Infrastructure.Persistence;
using Adeeb.Modules.Commerce;
using Adeeb.Modules.Commerce.Endpoints;
using Adeeb.Modules.Commerce.Infrastructure.Persistence;
using Adeeb.Modules.Commerce.Infrastructure.Files;
using Adeeb.Modules.Identity;
using Adeeb.Modules.Identity.Endpoints;
using Adeeb.Modules.Identity.Infrastructure.Persistence;
using Adeeb.Modules.Mmt;
using Adeeb.Modules.Mmt.Endpoints;
using Adeeb.Modules.Mmt.Infrastructure.Persistence;
using Adeeb.Modules.QuestionBank;
using Adeeb.Modules.QuestionBank.Endpoints;
using Adeeb.Modules.QuestionBank.Infrastructure.Persistence;
using Adeeb.Modules.Progression;
using Adeeb.Modules.Progression.Endpoints;
using Adeeb.Modules.Progression.Infrastructure.Persistence;
using Adeeb.Modules.Students;
using Adeeb.Modules.Students.Endpoints;
using Adeeb.Modules.Students.Infrastructure.Persistence;
using Adeeb.Modules.Vocabulary;
using Adeeb.Modules.Vocabulary.Endpoints;
using Adeeb.Modules.Vocabulary.Infrastructure.Persistence;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Localization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using System.Globalization;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddAdeebInfrastructure();
builder.Services.AddProductionHttp(builder.Configuration, builder.Environment);
builder.Services.AddProxyConfiguration(builder.Configuration);
builder.Services.AddAdeebLocalization();
builder.Services.AddAdeebRateLimiting(builder.Configuration, builder.Environment);
builder.Services.AddAdeebSwagger();
builder.Services.AddAdeebOpenTelemetry(builder.Configuration, builder.Environment);
builder.Services.AddAdeebHealthChecks(builder.Configuration);
builder.Services.AddDatabaseInitializationOptions(builder.Configuration);

builder.Services.AddIdentityModule(builder.Configuration);
builder.Services.AddAcademicCatalogModule(builder.Configuration);
builder.Services.AddQuestionBankModule(builder.Configuration);
builder.Services.AddProgressionModule(builder.Configuration);
builder.Services.AddStudentsModule(builder.Configuration);
builder.Services.AddVocabularyModule(builder.Configuration);
builder.Services.AddCommerceModule(builder.Configuration);
builder.Services.AddMmtModule(builder.Configuration);
builder.Services.AddAdeebDocumentation(builder.Configuration);
builder.Services.AddAdeebAuthorization();
builder.Services.AddProblemDetails(options =>
{
    options.CustomizeProblemDetails = context =>
    {
        if (context.ProblemDetails.Status != StatusCodes.Status500InternalServerError)
        {
            return;
        }

        var localizer = context.HttpContext.RequestServices.GetRequiredService<IMessageLocalizer>();
        var previousCulture = CultureInfo.CurrentUICulture;
        var requestCulture = context.HttpContext.Features.Get<IRequestCultureFeature>()?.RequestCulture.UICulture;
        if (requestCulture is not null)
        {
            CultureInfo.CurrentUICulture = requestCulture;
        }
        context.ProblemDetails.Type = "https://api.adeeb.tj/errors/common/unexpected-error";
        context.ProblemDetails.Title = localizer["Common.UnexpectedError"];
        CultureInfo.CurrentUICulture = previousCulture;
        context.ProblemDetails.Extensions["code"] = "common.unexpected_error";
        context.ProblemDetails.Extensions["traceId"] = context.HttpContext.TraceIdentifier;
    };
});

var app = builder.Build();

app.UseForwardedHeaders();
app.UseExceptionHandler();
app.UseProductionHttp();
app.UseAuthentication();
app.UseRequestLocalization();

var dbInitOptions = app.Services.GetRequiredService<IOptions<DatabaseInitializationOptions>>().Value;
if (dbInitOptions.AutoMigrate)
{
    await IdentityDatabaseInitializer.MigrateAsync(app.Services);
    await AcademicCatalogDatabaseInitializer.MigrateAsync(app.Services);
    await QuestionBankDatabaseInitializer.MigrateAsync(app.Services);
    await ProgressionDatabaseInitializer.MigrateAsync(app.Services);
    await StudentsDatabaseInitializer.MigrateAsync(app.Services);
    await VocabularyDatabaseInitializer.MigrateAsync(app.Services);
    await CommerceDatabaseInitializer.MigrateAsync(app.Services);
    await MmtDatabaseInitializer.MigrateAsync(app.Services);
}

if (dbInitOptions.Seed)
{
    await IdentitySeeder.SeedSuperAdminAsync(app.Services);
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger(options =>
    {
        options.RouteTemplate = "openapi/{documentName}.json";
    });
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/openapi/v2.json", "ADEEB V2 API");
        options.RoutePrefix = "swagger";
    });
}

app.UseLegacyReceiptPublicAccessBlock();
app.UseStaticFiles();
app.UseAuthorization();
app.UseRateLimiter();

app.MapHealthChecks("/health/live", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
{
    Predicate = _ => false
});
app.MapHealthChecks("/health/ready", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
{
    Predicate = r => r.Tags.Contains("ready"),
    ResultStatusCodes =
    {
        [Microsoft.Extensions.Diagnostics.HealthChecks.HealthStatus.Healthy] = StatusCodes.Status200OK,
        [Microsoft.Extensions.Diagnostics.HealthChecks.HealthStatus.Degraded] = StatusCodes.Status503ServiceUnavailable,
        [Microsoft.Extensions.Diagnostics.HealthChecks.HealthStatus.Unhealthy] = StatusCodes.Status503ServiceUnavailable
    }
});

app.MapIdentityEndpoints();
app.MapAcademicCatalogEndpoints();
app.MapQuestionBankEndpoints();
app.MapStudentTestingEndpoints();
app.MapProgressionEndpoints();
app.MapStudentEndpoints();
app.MapVocabularyEndpoints();
app.MapCommerceEndpoints();
app.MapMmtEndpoints();
app.MapAdeebDocumentation();

if (app.Environment.IsEnvironment("Testing"))
{
    app.MapGet("/__test/culture", (HttpContext context) => Results.Json(new
    {
        culture = CultureInfo.CurrentCulture.Name,
        uiCulture = CultureInfo.CurrentUICulture.Name,
        langClaim = context.User.FindFirst("lang")?.Value,
        sub = context.User.FindFirst("sub")?.Value
    }));

    app.MapPost("/__test/rate-limit-auth", (HttpContext context) => Results.Json(new
    {
        sub = context.User.FindFirst("sub")?.Value,
        remoteIp = context.Connection.RemoteIpAddress?.ToString()
    }))
    .RequireAuthorization()
    .RequireRateLimiting("auth-change-password");
}

app.Run();

public partial class Program;
