using System.Globalization;
using System.Text;

namespace Adeeb.Modules.Commerce.Application.Pagination;

internal readonly record struct PaymentReceiptCursor(DateTimeOffset CreatedAtUtc, Guid Id)
{
    public static string Encode(DateTimeOffset createdAtUtc, Guid id)
    {
        var value = $"{createdAtUtc.UtcTicks.ToString(CultureInfo.InvariantCulture)}:{id:N}";
        return Convert.ToBase64String(Encoding.UTF8.GetBytes(value));
    }

    public static bool TryDecode(string? cursor, out PaymentReceiptCursor value)
    {
        value = default;
        if (string.IsNullOrWhiteSpace(cursor))
        {
            return false;
        }

        try
        {
            var decoded = Encoding.UTF8.GetString(Convert.FromBase64String(cursor));
            var parts = decoded.Split(':', StringSplitOptions.TrimEntries);
            if (parts.Length != 2 ||
                !long.TryParse(parts[0], NumberStyles.None, CultureInfo.InvariantCulture, out var ticks) ||
                !Guid.TryParseExact(parts[1], "N", out var id))
            {
                return false;
            }

            value = new PaymentReceiptCursor(new DateTimeOffset(ticks, TimeSpan.Zero), id);
            return true;
        }
        catch (FormatException)
        {
            return false;
        }
        catch (ArgumentOutOfRangeException)
        {
            return false;
        }
    }
}
