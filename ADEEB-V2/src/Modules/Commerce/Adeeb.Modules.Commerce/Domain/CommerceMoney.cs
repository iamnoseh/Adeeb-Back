namespace Adeeb.Modules.Commerce.Domain;

public static class CommerceMoney
{
    public const int Precision = 18;
    public const int Scale = 2;
    public const decimal MaximumAmount = 9_999_999_999_999_999.99m;

    public static bool IsValid(decimal amount)
    {
        var scale = (decimal.GetBits(amount)[3] >> 16) & 0x7F;
        return amount > 0 && amount <= MaximumAmount && scale <= Scale;
    }
}
