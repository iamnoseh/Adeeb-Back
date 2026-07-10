using Microsoft.Extensions.DependencyInjection;
using Microsoft.OpenApi;

namespace Adeeb.Api.Configuration;

public static class SwaggerExtensions
{
    public static IServiceCollection AddAdeebSwagger(this IServiceCollection services)
    {
        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen(options =>
        {
            options.SwaggerDoc("v2", new()
            {
                Title = "ADEEB V2 API",
                Version = "v2",
                Description = "ADEEB V2 backend foundation and Identity/Auth API."
            });
            options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
            {
                Name = "Authorization",
                Type = SecuritySchemeType.Http,
                Scheme = "bearer",
                BearerFormat = "JWT",
                In = ParameterLocation.Header,
                Description = "Paste only the JWT access token. Swagger will send it as: Bearer {token}."
            });
            options.AddSecurityRequirement(document => new OpenApiSecurityRequirement
            {
                [new OpenApiSecuritySchemeReference("Bearer", document, null)] = []
            });
        });

        return services;
    }
}
