using System.Net;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Adeeb.Api.Configuration;

public static class ForwardedHeadersExtensions
{
    public static IServiceCollection AddProxyConfiguration(this IServiceCollection services, IConfiguration config)
    {
        services.AddOptions<ProxyOptions>()
            .Bind(config.GetSection("Proxy"))
            .Validate(options => options.ForwardLimit is null or > 0, "Proxy ForwardLimit must be positive when configured.")
            .Validate(options => options.KnownProxies.All(value => TryParseProxy(value, out _)), "Proxy KnownProxies contains an invalid IP address.")
            .Validate(options => options.KnownNetworks.All(value => TryParseNetwork(value, out _)), "Proxy KnownNetworks contains an invalid CIDR network.")
            .ValidateOnStart();

        services.Configure<ForwardedHeadersOptions>(options =>
        {
            options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
            options.KnownProxies.Clear();
            options.KnownIPNetworks.Clear();

            var proxyConfig = config.GetSection("Proxy").Get<ProxyOptions>();
            if (proxyConfig != null)
            {
                if (proxyConfig.ForwardLimit.HasValue)
                {
                    options.ForwardLimit = proxyConfig.ForwardLimit;
                }

                foreach (var proxy in proxyConfig.KnownProxies)
                {
                    if (TryParseProxy(proxy, out var ip))
                    {
                        options.KnownProxies.Add(ip);
                    }
                }

                foreach (var network in proxyConfig.KnownNetworks)
                {
                    if (TryParseNetwork(network, out var ipNetwork))
                    {
                        options.KnownIPNetworks.Add(ipNetwork);
                    }
                }
            }
        });

        return services;
    }

    internal static bool TryParseProxy(string? value, out IPAddress ipAddress) =>
        IPAddress.TryParse(value, out ipAddress!);

    internal static bool TryParseNetwork(string? value, out System.Net.IPNetwork network)
    {
        network = default!;
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        var parts = value.Split('/', StringSplitOptions.TrimEntries);
        if (parts.Length != 2 ||
            !IPAddress.TryParse(parts[0], out var prefix) ||
            !int.TryParse(parts[1], out var prefixLength))
        {
            return false;
        }

        var maxPrefix = prefix.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork ? 32 : 128;
        if (prefixLength < 0 || prefixLength > maxPrefix)
        {
            return false;
        }

        network = new System.Net.IPNetwork(prefix, prefixLength);
        return true;
    }
}
