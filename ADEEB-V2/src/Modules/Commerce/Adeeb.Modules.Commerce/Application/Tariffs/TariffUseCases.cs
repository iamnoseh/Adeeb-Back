using Adeeb.Application.Abstractions.Time;
using Adeeb.Modules.Commerce.Application.Auditing;
using Adeeb.Modules.Commerce.Application.Caching;
using Adeeb.Modules.Commerce.Contracts;
using Adeeb.Modules.Commerce.Domain.Tariffs;
using Adeeb.Modules.Commerce.Infrastructure.Persistence;
using Adeeb.SharedKernel.Results;
using Microsoft.EntityFrameworkCore;

namespace Adeeb.Modules.Commerce.Application.Tariffs;

public sealed class TariffUseCases(
    CommerceDbContext db,
    IDateTimeProvider clock,
    ICommerceAuditWriter audit,
    IActiveTariffCache cache)
{
    public async Task<Result<IReadOnlyList<TariffResponse>>> ListAsync(bool admin, CancellationToken cancellationToken)
    {
        if (!admin && cache.TryGet(out var cached))
        {
            return Result<IReadOnlyList<TariffResponse>>.Success(cached!);
        }

        var query = db.Tariffs.AsNoTracking();
        if (!admin) query = query.Where(x => x.Status == CommerceTariffStatus.Active);
        IReadOnlyList<TariffResponse> response = (await query.OrderBy(x => x.Price).ThenBy(x => x.Name)
            .ToListAsync(cancellationToken)).Select(ToResponse).ToList();
        if (!admin) cache.Set(response);
        return Result<IReadOnlyList<TariffResponse>>.Success(response);
    }

    public async Task<Result<TariffResponse>> CreateAsync(
        TariffFormRequest request,
        string? qrImageUrl,
        CancellationToken cancellationToken)
    {
        var validation = Validation.ValidateTariff(request, qrImageUrl, requireQrImage: true);
        if (validation.IsFailure) return Result<TariffResponse>.ValidationFailure(validation.ValidationErrors!);
        var now = clock.UtcNow;
        var tariff = new CommerceTariff(
            Guid.NewGuid(), request.Name!, request.Price!.Value, request.Currency!, request.DurationDays!.Value, qrImageUrl!, now);
        tariff.Update(
            request.Name!, request.Price.Value, request.Currency!, request.DurationDays.Value, qrImageUrl!,
            (CommerceTariffStatus)(request.Status ?? (int)CommerceTariffStatus.Active), now);
        db.Tariffs.Add(tariff);
        audit.Write(CommerceAuditActions.TariffCreated, "CommerceTariff", tariff.Id, newValues: Values(tariff));
        await db.SaveChangesAsync(cancellationToken);
        cache.Invalidate();
        return Result<TariffResponse>.Success(ToResponse(tariff));
    }

    public async Task<Result<TariffResponse>> UpdateAsync(
        Guid tariffId,
        TariffFormRequest request,
        string? qrImageUrl,
        CancellationToken cancellationToken)
    {
        var tariff = await db.Tariffs.SingleOrDefaultAsync(x => x.Id == tariffId, cancellationToken);
        if (tariff is null) return Result<TariffResponse>.Failure(CommerceErrors.TariffNotFound);
        var effectiveQr = qrImageUrl ?? tariff.QrImageUrl;
        var validation = Validation.ValidateTariff(request, effectiveQr, requireQrImage: false);
        if (validation.IsFailure) return Result<TariffResponse>.ValidationFailure(validation.ValidationErrors!);
        var oldValues = Values(tariff);
        tariff.Update(
            request.Name!, request.Price!.Value, request.Currency!, request.DurationDays!.Value, effectiveQr,
            (CommerceTariffStatus)(request.Status ?? (int)tariff.Status), clock.UtcNow);
        audit.Write(CommerceAuditActions.TariffUpdated, "CommerceTariff", tariff.Id, oldValues: oldValues, newValues: Values(tariff));
        await db.SaveChangesAsync(cancellationToken);
        cache.Invalidate();
        return Result<TariffResponse>.Success(ToResponse(tariff));
    }

    public async Task<Result<TariffResponse>> ArchiveAsync(Guid tariffId, CancellationToken cancellationToken)
    {
        var tariff = await db.Tariffs.SingleOrDefaultAsync(x => x.Id == tariffId, cancellationToken);
        if (tariff is null) return Result<TariffResponse>.Failure(CommerceErrors.TariffNotFound);
        var previousStatus = tariff.Status.ToString();
        tariff.Archive(clock.UtcNow);
        audit.Write(
            CommerceAuditActions.TariffArchived, "CommerceTariff", tariff.Id,
            oldValues: new Dictionary<string, object?> { ["status"] = previousStatus },
            newValues: new Dictionary<string, object?> { ["status"] = tariff.Status.ToString() });
        await db.SaveChangesAsync(cancellationToken);
        cache.Invalidate();
        return Result<TariffResponse>.Success(ToResponse(tariff));
    }

    private static Dictionary<string, object?> Values(CommerceTariff tariff) => new()
    {
        ["name"] = tariff.Name,
        ["price"] = tariff.Price,
        ["currency"] = tariff.Currency,
        ["durationDays"] = tariff.DurationDays,
        ["status"] = tariff.Status.ToString()
    };

    private static TariffResponse ToResponse(CommerceTariff tariff) => new(
        tariff.Id, tariff.Name, tariff.Price, tariff.Currency, tariff.DurationDays, tariff.QrImageUrl,
        tariff.Status.ToString(), tariff.CreatedAtUtc, tariff.UpdatedAtUtc);
}
