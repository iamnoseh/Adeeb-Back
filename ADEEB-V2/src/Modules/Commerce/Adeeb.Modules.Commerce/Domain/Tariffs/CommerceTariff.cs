using Adeeb.SharedKernel.Domain;

namespace Adeeb.Modules.Commerce.Domain.Tariffs;

public sealed class CommerceTariff : Entity
{
    public const int NameMaxLength = 160;
    public const int CurrencyMaxLength = 3;
    public const int QrImageUrlMaxLength = 512;

    private CommerceTariff() { }

    public CommerceTariff(Guid id, string name, decimal price, string currency, short durationDays, string qrImageUrl, DateTimeOffset now)
    {
        if (id == Guid.Empty)
        {
            throw new ArgumentException("Tariff id is required.", nameof(id));
        }

        Id = id;
        Update(name, price, currency, durationDays, qrImageUrl, CommerceTariffStatus.Active, now);
        CreatedAtUtc = now;
    }

    public string Name { get; private set; } = string.Empty;
    public decimal Price { get; private set; }
    public string Currency { get; private set; } = "TJS";
    public short DurationDays { get; private set; }
    public string QrImageUrl { get; private set; } = string.Empty;
    public CommerceTariffStatus Status { get; private set; }
    public DateTimeOffset CreatedAtUtc { get; private set; }
    public DateTimeOffset UpdatedAtUtc { get; private set; }

    public void Update(string name, decimal price, string currency, short durationDays, string qrImageUrl, CommerceTariffStatus status, DateTimeOffset now)
    {
        if (string.IsNullOrWhiteSpace(name) || name.Trim().Length > NameMaxLength)
        {
            throw new ArgumentException("Tariff name is invalid.", nameof(name));
        }

        if (price <= 0)
        {
            throw new ArgumentException("Tariff price must be positive.", nameof(price));
        }

        if (string.IsNullOrWhiteSpace(currency) || currency.Trim().Length != CurrencyMaxLength)
        {
            throw new ArgumentException("Currency is invalid.", nameof(currency));
        }

        if (durationDays <= 0)
        {
            throw new ArgumentException("Duration must be positive.", nameof(durationDays));
        }

        if (string.IsNullOrWhiteSpace(qrImageUrl) || qrImageUrl.Trim().Length > QrImageUrlMaxLength)
        {
            throw new ArgumentException("QR image URL is invalid.", nameof(qrImageUrl));
        }

        Name = name.Trim();
        Price = price;
        Currency = currency.Trim().ToUpperInvariant();
        DurationDays = durationDays;
        QrImageUrl = qrImageUrl.Trim();
        Status = status;
        UpdatedAtUtc = now;
    }

    public void Archive(DateTimeOffset now)
    {
        if (Status == CommerceTariffStatus.Archived)
        {
            return;
        }

        Status = CommerceTariffStatus.Archived;
        UpdatedAtUtc = now;
    }
}
