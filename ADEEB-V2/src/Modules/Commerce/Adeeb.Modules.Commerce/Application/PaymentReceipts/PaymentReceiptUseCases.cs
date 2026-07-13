using System.Security.Claims;
using Adeeb.Application.Abstractions.Storage;
using Adeeb.Modules.Commerce.Contracts;
using Adeeb.SharedKernel.Results;

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
        service.SubmitCurrentReceiptAsync(principal, tariffId, request, image, imageLength, cancellationToken);

    public Task<Result<PrivateFileReadResult>> OpenImageAsync(Guid receiptId, CancellationToken cancellationToken) =>
        service.OpenReceiptImageAsync(receiptId, cancellationToken);

    public Task<Result<PaymentReceiptResponse>> ApproveAsync(
        Guid receiptId,
        ClaimsPrincipal reviewer,
        ReviewPaymentReceiptRequest request,
        CancellationToken cancellationToken) =>
        service.ApproveReceiptAsync(receiptId, reviewer, request, cancellationToken);

    public Task<Result<PaymentReceiptResponse>> RejectAsync(
        Guid receiptId,
        ClaimsPrincipal reviewer,
        ReviewPaymentReceiptRequest request,
        CancellationToken cancellationToken) =>
        service.RejectReceiptAsync(receiptId, reviewer, request, cancellationToken);
}
