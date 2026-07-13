using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace Adeeb.Modules.Commerce.Application.Observability;

internal static class CommerceTelemetry
{
    public const string Name = "Adeeb.Commerce";
    public static readonly ActivitySource Activities = new(Name);
    public static readonly Meter Meter = new(Name);
    public static readonly Counter<long> ReceiptOperations = Meter.CreateCounter<long>("commerce.receipt.operations");
    public static readonly Histogram<double> ReceiptOperationDuration = Meter.CreateHistogram<double>(
        "commerce.receipt.operation.duration",
        unit: "ms");
}
