using Adeeb.Api.Configuration;

namespace Adeeb.IntegrationTests;

public sealed class ConfigurationValidationTests
{
    [Theory]
    [InlineData("127.0.0.1", true)]
    [InlineData("::1", true)]
    [InlineData("not-an-ip", false)]
    public void Proxy_known_proxy_values_are_validated(string value, bool expected)
    {
        Assert.Equal(expected, ForwardedHeadersExtensions.TryParseProxy(value, out _));
    }

    [Theory]
    [InlineData("10.0.0.0/8", true)]
    [InlineData("192.168.0.0/16", true)]
    [InlineData("2001:db8::/32", true)]
    [InlineData("10.0.0.0/99", false)]
    [InlineData("not-a-network", false)]
    public void Proxy_known_network_values_are_validated_as_cidr(string value, bool expected)
    {
        Assert.Equal(expected, ForwardedHeadersExtensions.TryParseNetwork(value, out _));
    }
}
