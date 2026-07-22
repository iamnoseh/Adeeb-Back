using System.Text;
using Adeeb.Application.Abstractions.Students;
using Adeeb.Application.Abstractions.Identity;
using Adeeb.Modules.Identity.Application;
using Adeeb.Modules.Identity.Domain.Users;
using Adeeb.Modules.Identity.Infrastructure.Authentication;
using Adeeb.Modules.Identity.Infrastructure.Configuration;
using Adeeb.Modules.Identity.Infrastructure.Passwords;
using Adeeb.Modules.Identity.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.IdentityModel.Tokens;

namespace Adeeb.Modules.Identity;

public static class DependencyInjection
{
    public static IServiceCollection AddIdentityModule(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<JwtOptions>()
            .Bind(configuration.GetSection(JwtOptions.SectionName))
            .ValidateDataAnnotations()
            .Validate(x => x.SigningKey.Length >= 32, "JWT signing key must be at least 32 characters.")
            .Validate(x => JwtOptions.IsAllowedSigningKey(x.SigningKey), "JWT signing key must not use a committed placeholder or obvious default value.")
            .ValidateOnStart();
        services.AddOptions<RefreshTokenOptions>().Bind(configuration.GetSection(RefreshTokenOptions.SectionName)).ValidateDataAnnotations().ValidateOnStart();
        services.AddOptions<PasswordPolicyOptions>().Bind(configuration.GetSection(PasswordPolicyOptions.SectionName)).ValidateDataAnnotations().ValidateOnStart();
        services.AddOptions<SeedSuperAdminOptions>()
            .Bind(configuration.GetSection(SeedSuperAdminOptions.SectionName))
            .Validate(x => !x.Enabled || (!string.IsNullOrWhiteSpace(x.Email) && !string.IsNullOrWhiteSpace(x.Password)), "SuperAdmin seed requires email and password when enabled.")
            .ValidateOnStart();

        var connectionString = configuration.GetConnectionString("Identity")
            ?? configuration.GetConnectionString("Default");
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException("Identity database connection string is required.");
        }

        services.AddDbContext<IdentityDbContext>(options => options.UseNpgsql(connectionString));
        services.AddScoped<PasswordHasher<User>>();
        services.AddScoped<PasswordPolicy>();
        services.AddScoped<IRefreshTokenGenerator, RefreshTokenGenerator>();
        services.AddScoped<IAccessTokenGenerator, JwtTokenGenerator>();
        services.TryAddScoped<IStudentRegistrationProvisioner, MissingStudentRegistrationProvisioner>();
        services.AddScoped<IdentityService>();
        services.AddScoped<IPublicUserProfileDirectory, PublicUserProfileDirectory>();

        var jwt = configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>()
            ?? throw new InvalidOperationException("JWT configuration is required.");
        if (!JwtOptions.IsAllowedSigningKey(jwt.SigningKey))
        {
            throw new InvalidOperationException("JWT signing key must not use a committed placeholder or obvious default value.");
        }
        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.RequireHttpsMetadata = true;
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidIssuer = jwt.Issuer,
                    ValidateAudience = true,
                    ValidAudience = jwt.Audience,
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.SigningKey)),
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.FromSeconds(30),
                    NameClaimType = "sub",
                    RoleClaimType = System.Security.Claims.ClaimTypes.Role
                };
            });

        return services;
    }
}
