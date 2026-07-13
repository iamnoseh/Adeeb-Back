using Adeeb.Modules.Commerce.Contracts;

namespace Adeeb.Modules.Commerce.Application.Caching;

public interface IActiveTariffCache
{
    bool TryGet(out IReadOnlyList<TariffResponse>? tariffs);
    void Set(IReadOnlyList<TariffResponse> tariffs);
    void Invalidate();
}

internal sealed class NullActiveTariffCache : IActiveTariffCache
{
    public bool TryGet(out IReadOnlyList<TariffResponse>? tariffs)
    {
        tariffs = null;
        return false;
    }

    public void Set(IReadOnlyList<TariffResponse> tariffs)
    {
    }

    public void Invalidate()
    {
    }
}
