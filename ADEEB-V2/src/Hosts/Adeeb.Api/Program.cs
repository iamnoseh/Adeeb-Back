using Adeeb.Api.Configuration;
using Adeeb.Api.Documentation;
using Adeeb.Api.Documentation.Endpoints;
using Adeeb.Infrastructure;
using Adeeb.Modules.AcademicCatalog;
using Adeeb.Modules.AcademicCatalog.Endpoints;
using Adeeb.Modules.AcademicCatalog.Infrastructure.Persistence;
using Adeeb.Modules.Identity;
using Adeeb.Modules.Identity.Endpoints;
using Adeeb.Modules.Identity.Infrastructure.Persistence;
using Adeeb.Modules.QuestionBank;
using Adeeb.Modules.QuestionBank.Endpoints;
using Adeeb.Modules.QuestionBank.Infrastructure.Persistence;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddAdeebInfrastructure();
builder.Services.AddProxyConfiguration(builder.Configuration);
builder.Services.AddAdeebLocalization();
builder.Services.AddAdeebRateLimiting();
builder.Services.AddAdeebSwagger();
builder.Services.AddAdeebOpenTelemetry();
builder.Services.AddAdeebHealthChecks(builder.Configuration);
builder.Services.Configure<DatabaseInitializationOptions>(builder.Configuration.GetSection("DatabaseInitialization"));

builder.Services.AddIdentityModule(builder.Configuration);
builder.Services.AddAcademicCatalogModule(builder.Configuration);
builder.Services.AddQuestionBankModule(builder.Configuration);
builder.Services.AddAdeebDocumentation(builder.Configuration);
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("ContentAdmin", policy => policy.RequireRole("SuperAdmin", "Admin"));
});
builder.Services.AddProblemDetails();

var app = builder.Build();

app.UseForwardedHeaders();
app.UseExceptionHandler();

var dbInitOptions = app.Services.GetRequiredService<IOptions<DatabaseInitializationOptions>>().Value;
if (dbInitOptions.AutoMigrate)
{
    await IdentityDatabaseInitializer.MigrateAsync(app.Services);
    await AcademicCatalogDatabaseInitializer.MigrateAsync(app.Services);
    await QuestionBankDatabaseInitializer.MigrateAsync(app.Services);
    
    if (dbInitOptions.Seed)
    {
        await IdentitySeeder.SeedSuperAdminAsync(app.Services);
    }
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

app.UseRequestLocalization();
app.UseStaticFiles();
app.UseAuthentication();
app.UseAuthorization();
app.UseRateLimiter();

app.MapHealthChecks("/health/live", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
{
    Predicate = r => r.Name.Contains("live")
});
app.MapHealthChecks("/health/ready", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
{
    Predicate = r => r.Tags.Contains("ready")
});

app.MapIdentityEndpoints();
app.MapAcademicCatalogEndpoints();
app.MapQuestionBankEndpoints();
app.MapAdeebDocumentation();

app.Run();

public partial class Program;
