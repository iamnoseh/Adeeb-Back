namespace Adeeb.Modules.Mmt.Application;

internal static class MmtPaging
{
    internal const int DefaultPageSize = 10;
    internal const int MaximumPageSize = 50;

    internal static int Page(int value) => Math.Max(1, value);
    internal static int PageSize(int value) => Math.Clamp(value, 1, MaximumPageSize);
}
