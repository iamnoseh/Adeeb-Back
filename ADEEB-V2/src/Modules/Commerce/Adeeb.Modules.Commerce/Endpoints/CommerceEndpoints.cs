using Adeeb.Application.Abstractions.Localization;
using Adeeb.Modules.Commerce.Application;
using Adeeb.Modules.Commerce.Contracts;
using Adeeb.Modules.Commerce.Infrastructure.Files;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

namespace Adeeb.Modules.Commerce.Endpoints;

public static class CommerceEndpoints
{
    public static IEndpointRouteBuilder MapCommerceEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v2/commerce").WithTags("Commerce");

        group.MapGet("/tariffs", async (
            CommerceService service,
            HttpContext context,
            IMessageLocalizer localizer,
            CancellationToken ct) =>
            (await service.GetTariffsAsync(admin: false, ct)).ToHttpResult(context, localizer));

        group.MapGet("/me/entitlements", async (
            CommerceService service,
            HttpContext context,
            IMessageLocalizer localizer,
            CancellationToken ct) =>
            (await service.GetCurrentEntitlementsAsync(context.User, ct)).ToHttpResult(context, localizer))
            .RequireAuthorization();

        group.MapGet("/me/payment-receipts", async (
            int? status,
            CommerceService service,
            HttpContext context,
            IMessageLocalizer localizer,
            CancellationToken ct) =>
            (await service.GetCurrentPaymentReceiptsAsync(context.User, status, ct)).ToHttpResult(context, localizer))
            .RequireAuthorization();

        group.MapPost("/tariffs/{tariffId:guid}/payment-receipts", async (
            Guid tariffId,
            [FromForm] SubmitPaymentReceiptFormRequest form,
            CommerceImageStorage images,
            CommerceService service,
            HttpContext context,
            IMessageLocalizer localizer,
            CancellationToken ct) =>
        {
            var saved = await images.SaveReceiptAsync(form.ReceiptImage, ct);
            if (saved.IsFailure)
            {
                return saved.ToHttpResult(context, localizer);
            }

            return (await service.SubmitCurrentReceiptAsync(context.User, tariffId, form, saved.Value, ct)).ToHttpResult(context, localizer);
        })
        .RequireAuthorization()
        .Accepts<SubmitPaymentReceiptFormRequest>("multipart/form-data")
        .DisableAntiforgery();

        var admin = app.MapGroup("/api/v2/admin/commerce")
            .WithTags("Commerce Admin")
            .RequireAuthorization("ContentAdmin");

        admin.MapGet("/tariffs", async (
            CommerceService service,
            HttpContext context,
            IMessageLocalizer localizer,
            CancellationToken ct) =>
            (await service.GetTariffsAsync(admin: true, ct)).ToHttpResult(context, localizer));

        admin.MapPost("/tariffs", async (
            [FromForm] TariffFormRequest form,
            CommerceImageStorage images,
            CommerceService service,
            HttpContext context,
            IMessageLocalizer localizer,
            CancellationToken ct) =>
        {
            var saved = await images.SaveQrAsync(form.QrImage, ct);
            if (saved.IsFailure)
            {
                return saved.ToHttpResult(context, localizer);
            }

            return (await service.CreateTariffAsync(form, saved.Value, ct)).ToHttpResult(context, localizer);
        })
        .Accepts<TariffFormRequest>("multipart/form-data")
        .DisableAntiforgery();

        admin.MapPut("/tariffs/{tariffId:guid}", async (
            Guid tariffId,
            [FromForm] TariffFormRequest form,
            CommerceImageStorage images,
            CommerceService service,
            HttpContext context,
            IMessageLocalizer localizer,
            CancellationToken ct) =>
        {
            var saved = await images.SaveQrAsync(form.QrImage, ct);
            if (saved.IsFailure)
            {
                return saved.ToHttpResult(context, localizer);
            }

            return (await service.UpdateTariffAsync(tariffId, form, saved.Value, ct)).ToHttpResult(context, localizer);
        })
        .Accepts<TariffFormRequest>("multipart/form-data")
        .DisableAntiforgery();

        admin.MapPost("/tariffs/{tariffId:guid}/archive", async (
            Guid tariffId,
            CommerceService service,
            HttpContext context,
            IMessageLocalizer localizer,
            CancellationToken ct) =>
            (await service.ArchiveTariffAsync(tariffId, ct)).ToHttpResult(context, localizer));

        admin.MapGet("/payment-receipts", async (
            int? status,
            CommerceService service,
            HttpContext context,
            IMessageLocalizer localizer,
            CancellationToken ct) =>
            (await service.GetPaymentReceiptsAsync(status, ct)).ToHttpResult(context, localizer));

        admin.MapPost("/payment-receipts/{receiptId:guid}/approve", async (
            Guid receiptId,
            ReviewPaymentReceiptRequest request,
            CommerceService service,
            HttpContext context,
            IMessageLocalizer localizer,
            CancellationToken ct) =>
            (await service.ApproveReceiptAsync(receiptId, context.User, request, ct)).ToHttpResult(context, localizer));

        admin.MapPost("/payment-receipts/{receiptId:guid}/reject", async (
            Guid receiptId,
            ReviewPaymentReceiptRequest request,
            CommerceService service,
            HttpContext context,
            IMessageLocalizer localizer,
            CancellationToken ct) =>
            (await service.RejectReceiptAsync(receiptId, context.User, request, ct)).ToHttpResult(context, localizer));

        admin.MapPost("/students/{studentId:guid}/premium-grants", async (
            Guid studentId,
            GrantPremiumEntitlementRequest request,
            CommerceService service,
            HttpContext context,
            IMessageLocalizer localizer,
            CancellationToken ct) =>
            (await service.GrantPremiumAsync(studentId, request, ct)).ToHttpResult(context, localizer));

        admin.MapPost("/entitlements/{entitlementId:guid}/revoke", async (
            Guid entitlementId,
            RevokeEntitlementRequest request,
            CommerceService service,
            HttpContext context,
            IMessageLocalizer localizer,
            CancellationToken ct) =>
            (await service.RevokeEntitlementAsync(entitlementId, request, ct)).ToHttpResult(context, localizer));

        return app;
    }
}
