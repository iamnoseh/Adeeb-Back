namespace Adeeb.Api.Configuration;

public class ProxyOptions
{
    public string[] KnownProxies { get; set; } = [];
    public string[] KnownNetworks { get; set; } = [];
    public int? ForwardLimit { get; set; }
}
