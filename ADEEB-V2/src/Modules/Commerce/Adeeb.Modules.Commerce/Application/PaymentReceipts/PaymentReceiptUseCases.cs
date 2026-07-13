using System.Security.Claims;
using Adeeb.Application.Abstractions.Storage;
using Adeeb.Modules.Commerce.Contracts;
using Adeeb.Modules.Commerce.Application.Observability;
using Adeeb.SharedKernel.Results;
using System.Diagnostics;

namespace Adeeb.Modules.Commerce.Application.PaymentReceipts;

public sealed class PaymentReceiptUseCases(CommerceService service)
{
    public Task<Result<CursorPageResponse<PaymentReceiptListItemResponse>>> ListCurrentAsync(
        ClaimsPrincipal principal,
        StudentPaymentReceiptQuery query,
        CancellationToken cancellationToken) =>
        service.GetCurrentPaymentReceiptsPageAsync(principal, query, cancellationToken);

    public Task<Result<CursorPageResponse<PaymentReceiptListItemResponse>>> ListAdminAsync(
        AdminPaymentReceiptQuery query,
        CancellationToken cancellationToken) =>
        service.GetPaymentReceiptsPageAsync(query, cancellationToken);

    public Task<Result<PaymentReceiptResponse>> SubmitAsync(
        ClaimsPrincipal principal,
        Guid tariffId,
        SubmitPaymentReceiptFormRequest request,
        Stream? image,
        long imageLength,
        CancellationToken cancellationToken) =>
        ObserveAsync(
            "submit",
            () => service.SubmitCurrentReceiptAsync(principal, tariffId, request, image, imageLength, cancellationToken));

    public Task<Result<PrivateFileReadResult>> OpenImageAsync(Guid receiptId, CancellationToken cancellationToken) =>
        service.OpenReceiptImageAsync(receiptId, cancellationToken);

    public Task<Result<PaymentReceiptResponse>> ApproveAsync(
        Guid receiptId,
        ClaimsPrincipal reviewer,
        ReviewPaymentReceiptRequest request,
        CancellationToken cancellationToken) =>
        ObserveAsync("approve", () => service.ApproveReceiptAsync(receiptId, reviewer, request, cancellationToken));

    public Task<Result<PaymentReceiptResponse>> RejectAsync(
        Guid receiptId,
        ClaimsPrincipal reviewer,
        ReviewPaymentReceiptRequest request,
        CancellationToken cancellationToken) =>
        ObserveAsync("reject", () => service.RejectReceiptAsync(receiptId, reviewer, request, cancellationToken));

    private static async Task<Result<PaymentReceiptResponse>> ObserveAsync(
        string operation,
        Func<Task<Result<PaymentReceiptResponse>>> action)
    {
        using var activity = CommerceTelemetry.Activities.StartActivity($"commerce.receipt.{operation}");
        var started = Stopwatch.GetTimestamp();
        try
        {
            var result = await action();
            var outcome = result.IsSuccess ? "success" : "failure";
            CommerceTelemetry.ReceiptOperations.Add(1, new KeyValuePair<string, object?>("operation", operation), new("outcome", outcome));
            activity?.SetTag("commerce.outcome", outcome);
            activity?.SetTag("commerce.error.code", result.Error?.Code);
            return result;
        }
        catch (Exception exception)
        {
            CommerceTelemetry.ReceiptOperations.Add(1, new KeyValuePair<string, object?>("operation", operation), new("outcome", "exception"));
            activity?.SetStatus(ActivityStatusCode.Error, exception.GetType().Name);
            throw;
        }
        finally
        {
            CommerceTelemetry.ReceiptOperationDuration.Record(
                Stopwatch.GetElapsedTime(started).TotalMilliseconds,
                new KeyValuePair<string, object?>("operation", operation));
        }
    }
}
