using System.Net;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Adeeb.Api.Configuration;

public static class ForwardedHeadersExtensions
{
    public static IServiceCollection AddProxyConfiguration(this IServiceCollection services, IConfiguration config)
    {
        services.Configure<ForwardedHeadersOptions>(options =>
        {
            options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
            options.KnownProxies.Clear();
            options.KnownProxies.Clear();

            var proxyConfig = config.GetSection("Proxy").Get<ProxyOptions>();
            if (proxyConfig != null)
            {
                if (proxyConfig.ForwardLimit.HasValue)
                {
                    options.ForwardLimit = proxyConfig.ForwardLimit;
                }

                foreach (var proxy in proxyConfig.KnownProxies)
                {
                    if (IPAddress.TryParse(proxy, out var ip))
                    {
                        options.KnownProxies.Add(ip);
                    }
                }
            }
        });

        return services;
    }
}
