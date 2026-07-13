using Amazon;
using Amazon.Runtime;
using Amazon.S3;
using Adeeb.Application.Abstractions.Storage;
using Adeeb.Modules.Commerce.Application;
using Adeeb.Modules.Commerce.Application.Storage;
using Adeeb.Modules.Commerce.Application.Auditing;
using Adeeb.Modules.Commerce.Application.Caching;
using Adeeb.Modules.Commerce.Application.Entitlements;
using Adeeb.Modules.Commerce.Application.PaymentReceipts;
using Adeeb.Modules.Commerce.Application.Tariffs;
using Adeeb.Modules.Commerce.Infrastructure.Caching;
using Adeeb.Modules.Commerce.Infrastructure.Auditing;
using Adeeb.Modules.Commerce.Infrastructure.Files;
using Adeeb.Modules.Commerce.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Adeeb.Modules.Commerce;

public static class DependencyInjection
{
    public static IServiceCollection AddCommerceModule(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("Commerce")
            ?? configuration.GetConnectionString("Default")
            ?? configuration.GetConnectionString("Identity");
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException("Commerce database connection string is required.");
        }

        services.AddDbContext<CommerceDbContext>(options => options.UseNpgsql(connectionString));
        services.AddMemoryCache();
        services.AddSingleton<IActiveTariffCache, ActiveTariffCache>();
        var storage = configuration.GetSection(PrivateFileStorageOptions.SectionName).Get<PrivateFileStorageOptions>() ?? new();
        services.AddOptions<PrivateFileStorageOptions>()
            .Bind(configuration.GetSection(PrivateFileStorageOptions.SectionName))
            .Validate(x => x.Provider is "Local" or "S3", "PrivateFileStorage:Provider must be Local or S3.")
            .ValidateOnStart();
        if (string.Equals(storage.Provider, "S3", StringComparison.Ordinal))
        {
            if (string.IsNullOrWhiteSpace(storage.Bucket) ||
                string.IsNullOrWhiteSpace(storage.AccessKey) ||
                string.IsNullOrWhiteSpace(storage.SecretKey))
            {
                throw new InvalidOperationException("S3 private storage requires bucket and credentials.");
            }

            services.AddSingleton<IAmazonS3>(_ =>
            {
                var config = new AmazonS3Config
                {
                    ForcePathStyle = storage.ForcePathStyle,
                    ServiceURL = storage.ServiceUrl,
                    RegionEndpoint = string.IsNullOrWhiteSpace(storage.Region) ? null : RegionEndpoint.GetBySystemName(storage.Region)
                };
                return new AmazonS3Client(new BasicAWSCredentials(storage.AccessKey, storage.SecretKey), config);
            });
            services.AddScoped<S3PrivateFileStorage>();
            services.AddScoped<IPrivateFileStorage>(sp => sp.GetRequiredService<S3PrivateFileStorage>());
            services.AddScoped<IPrivateFileMaintenance>(sp => sp.GetRequiredService<S3PrivateFileStorage>());
        }
        else
        {
            services.AddScoped<LocalPrivateFileStorage>();
            services.AddScoped<IPrivateFileStorage>(sp => sp.GetRequiredService<LocalPrivateFileStorage>());
            services.AddScoped<IPrivateFileMaintenance>(sp => sp.GetRequiredService<LocalPrivateFileStorage>());
        }

        services.AddScoped<IReceiptImageProcessor, ReceiptImageProcessor>();
        services.AddHttpContextAccessor();
        services.AddScoped<ICommerceAuditContext, HttpCommerceAuditContext>();
        services.AddScoped<ICommerceAuditWriter, CommerceAuditWriter>();
        services.AddScoped<CommerceService>();
        services.AddScoped<TariffUseCases>();
        services.AddScoped<PaymentReceiptUseCases>();
        services.AddScoped<EntitlementUseCases>();
        services.AddScoped<CommerceImageStorage>();
        services.AddHealthChecks()
            .AddCheck<PrivateFileStorageHealthCheck>("private-file-storage", tags: ["storage", "ready"]);
        services.AddHostedService<OrphanReceiptFileCleanupService>();
        return services;
    }
}
