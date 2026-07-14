using Adeeb.Modules.Commerce.Application.Caching;
using Adeeb.Modules.Commerce.Contracts;
using Microsoft.Extensions.Caching.Memory;

namespace Adeeb.Modules.Commerce.Infrastructure.Caching;

internal sealed class ActiveTariffCache(IMemoryCache cache) : IActiveTariffCache
{
    private const string CacheKey = "commerce:tariffs:active:v1";
    private static readonly TimeSpan Lifetime = TimeSpan.FromMinutes(2);

    public bool TryGet(out IReadOnlyList<TariffResponse>? tariffs) => cache.TryGetValue(CacheKey, out tariffs);

    public void Set(IReadOnlyList<TariffResponse> tariffs) =>
        cache.Set(CacheKey, tariffs, new MemoryCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = Lifetime,
            Size = 1
        });

    public void Invalidate() => cache.Remove(CacheKey);
}
