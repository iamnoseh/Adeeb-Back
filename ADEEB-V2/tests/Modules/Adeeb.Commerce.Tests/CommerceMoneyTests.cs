using Adeeb.Modules.Commerce.Contracts;
using Adeeb.Modules.Commerce.Application;
using Adeeb.Modules.Commerce.Domain;
using Adeeb.Modules.Commerce.Domain.Tariffs;
using Adeeb.Modules.Commerce.Infrastructure.Persistence;
using Adeeb.Modules.Students.Contracts;
using Adeeb.Application.Abstractions.Time;
using Microsoft.EntityFrameworkCore;

namespace Adeeb.Commerce.Tests;

public sealed class CommerceMoneyTests
{
    [Theory]
    [InlineData("0")]
    [InlineData("-0.01")]
    [InlineData("10000000000000000.00")]
    [InlineData("12.345")]
    [InlineData("12.340")]
    public void Unsupported_amounts_are_rejected_by_domain(string value)
    {
        var amount = decimal.Parse(value, System.Globalization.CultureInfo.InvariantCulture);

        Assert.False(CommerceMoney.IsValid(amount));
        Assert.Throws<ArgumentException>(() => new CommerceTariff(
            Guid.NewGuid(), "Premium", amount, "TJS", 30, "private/qr.webp", DateTimeOffset.UtcNow));
    }

    [Theory]
    [InlineData("0.01")]
    [InlineData("12.34")]
    [InlineData("9999999999999999.99")]
    public void Supported_amount_boundaries_are_accepted(string value)
    {
        var amount = decimal.Parse(value, System.Globalization.CultureInfo.InvariantCulture);

        Assert.True(CommerceMoney.IsValid(amount));
        var tariff = new CommerceTariff(
            Guid.NewGuid(), "Premium", amount, "TJS", 30, "private/qr.webp", DateTimeOffset.UtcNow);
        Assert.Equal(amount, tariff.Price);
    }

    [Fact]
    public async Task Application_validation_rejects_fractional_precision()
    {
        await using var db = new CommerceDbContext(
            new DbContextOptionsBuilder<CommerceDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options);
        var service = new CommerceService(db, new EmptyStudentLookup(), new FixedClock());

        var result = await service.CreateTariffAsync(
            new TariffFormRequest { Name = "Premium", Price = 12.345m, Currency = "TJS", DurationDays = 30, Status = 1 },
            "private/qr.webp",
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("commerce.tariff.price.invalid", result.ValidationErrors!["price"].Single().Code);
    }

    private sealed class EmptyStudentLookup : IStudentLookup
    {
        public Task<StudentReference?> FindByIdentityUserIdAsync(Guid identityUserId, CancellationToken cancellationToken) =>
            Task.FromResult<StudentReference?>(null);
        public Task<StudentReference?> FindByStudentIdAsync(Guid studentId, CancellationToken cancellationToken) =>
            Task.FromResult<StudentReference?>(null);
    }

    private sealed class FixedClock : IDateTimeProvider
    {
        public DateTimeOffset UtcNow => DateTimeOffset.Parse("2026-07-14T08:00:00Z");
        public DateTimeOffset DushanbeNow => UtcNow.ToOffset(TimeSpan.FromHours(5));
        public DateTimeOffset ToDushanbeTime(DateTimeOffset utc) => utc.ToOffset(TimeSpan.FromHours(5));
    }
}
