using Adeeb.Modules.Commerce.Contracts;
using Adeeb.SharedKernel.Results;

namespace Adeeb.Modules.Commerce.Application.Tariffs;

public sealed class TariffUseCases(CommerceService service)
{
    public Task<Result<IReadOnlyList<TariffResponse>>> ListAsync(bool admin, CancellationToken cancellationToken) =>
        service.GetTariffsAsync(admin, cancellationToken);

    public Task<Result<TariffResponse>> CreateAsync(
        TariffFormRequest request,
        string? qrImageUrl,
        CancellationToken cancellationToken) =>
        service.CreateTariffAsync(request, qrImageUrl, cancellationToken);

    public Task<Result<TariffResponse>> UpdateAsync(
        Guid tariffId,
        TariffFormRequest request,
        string? qrImageUrl,
        CancellationToken cancellationToken) =>
        service.UpdateTariffAsync(tariffId, request, qrImageUrl, cancellationToken);

    public Task<Result<TariffResponse>> ArchiveAsync(Guid tariffId, CancellationToken cancellationToken) =>
        service.ArchiveTariffAsync(tariffId, cancellationToken);
}
