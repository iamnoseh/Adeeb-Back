using Adeeb.Application.Abstractions.Localization;
using Adeeb.Application.Abstractions.Authorization;
using Adeeb.Modules.Commerce.Application;
using Adeeb.Modules.Commerce.Application.Entitlements;
using Adeeb.Modules.Commerce.Application.PaymentReceipts;
using Adeeb.Modules.Commerce.Application.Tariffs;
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
            TariffUseCases useCases,
            HttpContext context,
            IMessageLocalizer localizer,
            CancellationToken ct) =>
            (await useCases.ListAsync(admin: false, ct)).ToHttpResult(context, localizer));

        group.MapGet("/me/entitlements", async (
            EntitlementUseCases useCases,
            HttpContext context,
            IMessageLocalizer localizer,
            CancellationToken ct) =>
            (await useCases.GetCurrentAsync(context.User, ct)).ToHttpResult(context, localizer))
            .RequireAuthorization();

        group.MapGet("/me/payment-receipts", async (
            [AsParameters] StudentPaymentReceiptQuery query,
            PaymentReceiptUseCases useCases,
            HttpContext context,
            IMessageLocalizer localizer,
            CancellationToken ct) =>
            (await useCases.ListCurrentAsync(context.User, query, ct)).ToHttpResult(context, localizer))
            .RequireAuthorization();

        group.MapPost("/tariffs/{tariffId:guid}/payment-receipts", async (
            Guid tariffId,
            [FromForm] SubmitPaymentReceiptFormRequest form,
            PaymentReceiptUseCases useCases,
            HttpContext context,
            IMessageLocalizer localizer,
            CancellationToken ct) =>
        {
            await using var image = form.ReceiptImage?.OpenReadStream();
            return (await useCases.SubmitAsync(
                context.User,
                tariffId,
                form,
                image,
                form.ReceiptImage?.Length ?? 0,
                ct)).ToHttpResult(context, localizer);
        })
        .RequireAuthorization()
        .Accepts<SubmitPaymentReceiptFormRequest>("multipart/form-data")
        .DisableAntiforgery();

        var admin = app.MapGroup("/api/v2/admin/commerce")
            .WithTags("Commerce Admin")
            .RequireAuthorization();

        admin.MapGet("/tariffs", async (
            TariffUseCases useCases,
            HttpContext context,
            IMessageLocalizer localizer,
            CancellationToken ct) =>
            (await useCases.ListAsync(admin: true, ct)).ToHttpResult(context, localizer))
            .RequireAuthorization(Permissions.Commerce.ViewTariffs);

        admin.MapPost("/tariffs", async (
            [FromForm] TariffFormRequest form,
            CommerceImageStorage images,
            TariffUseCases useCases,
            HttpContext context,
            IMessageLocalizer localizer,
            CancellationToken ct) =>
        {
            var saved = await images.SaveQrAsync(form.QrImage, ct);
            if (saved.IsFailure)
            {
                return saved.ToHttpResult(context, localizer);
            }

            return (await useCases.CreateAsync(form, saved.Value, ct)).ToHttpResult(context, localizer);
        })
        .Accepts<TariffFormRequest>("multipart/form-data")
        .DisableAntiforgery()
        .RequireAuthorization(Permissions.Commerce.ManageTariffs);

        admin.MapPut("/tariffs/{tariffId:guid}", async (
            Guid tariffId,
            [FromForm] TariffFormRequest form,
            CommerceImageStorage images,
            TariffUseCases useCases,
            HttpContext context,
            IMessageLocalizer localizer,
            CancellationToken ct) =>
        {
            var saved = await images.SaveQrAsync(form.QrImage, ct);
            if (saved.IsFailure)
            {
                return saved.ToHttpResult(context, localizer);
            }

            return (await useCases.UpdateAsync(tariffId, form, saved.Value, ct)).ToHttpResult(context, localizer);
        })
        .Accepts<TariffFormRequest>("multipart/form-data")
        .DisableAntiforgery()
        .RequireAuthorization(Permissions.Commerce.ManageTariffs);

        admin.MapPost("/tariffs/{tariffId:guid}/archive", async (
            Guid tariffId,
            TariffUseCases useCases,
            HttpContext context,
            IMessageLocalizer localizer,
            CancellationToken ct) =>
            (await useCases.ArchiveAsync(tariffId, ct)).ToHttpResult(context, localizer))
            .RequireAuthorization(Permissions.Commerce.ManageTariffs);

        admin.MapGet("/payment-receipts", async (
            [AsParameters] AdminPaymentReceiptQuery query,
            PaymentReceiptUseCases useCases,
            HttpContext context,
            IMessageLocalizer localizer,
            CancellationToken ct) =>
            (await useCases.ListAdminAsync(query, ct)).ToHttpResult(context, localizer))
            .RequireAuthorization(Permissions.Commerce.ViewPaymentReceipts);

        admin.MapGet("/payment-receipts/{receiptId:guid}/image", async (
            Guid receiptId,
            PaymentReceiptUseCases useCases,
            HttpContext context,
            IMessageLocalizer localizer,
            CancellationToken ct) =>
        {
            var result = await useCases.OpenImageAsync(receiptId, ct);
            return result.IsFailure
                ? result.ToHttpResult(context, localizer)
                : Results.Stream(result.Value!.Content, result.Value.ContentType, enableRangeProcessing: true);
        })
        .RequireAuthorization(Permissions.Commerce.ViewPaymentReceipts);

        admin.MapPost("/payment-receipts/{receiptId:guid}/approve", async (
            Guid receiptId,
            ReviewPaymentReceiptRequest request,
            PaymentReceiptUseCases useCases,
            HttpContext context,
            IMessageLocalizer localizer,
            CancellationToken ct) =>
            (await useCases.ApproveAsync(receiptId, context.User, request, ct)).ToHttpResult(context, localizer))
            .RequireAuthorization(Permissions.Commerce.ReviewPaymentReceipts);

        admin.MapPost("/payment-receipts/{receiptId:guid}/reject", async (
            Guid receiptId,
            ReviewPaymentReceiptRequest request,
            PaymentReceiptUseCases useCases,
            HttpContext context,
            IMessageLocalizer localizer,
            CancellationToken ct) =>
            (await useCases.RejectAsync(receiptId, context.User, request, ct)).ToHttpResult(context, localizer))
            .RequireAuthorization(Permissions.Commerce.ReviewPaymentReceipts);

        admin.MapPost("/students/{studentId:guid}/premium-grants", async (
            Guid studentId,
            GrantPremiumEntitlementRequest request,
            EntitlementUseCases useCases,
            HttpContext context,
            IMessageLocalizer localizer,
            CancellationToken ct) =>
            (await useCases.GrantPremiumAsync(studentId, request, ct)).ToHttpResult(context, localizer))
            .RequireAuthorization(Permissions.Commerce.GrantPremium);

        admin.MapPost("/entitlements/{entitlementId:guid}/revoke", async (
            Guid entitlementId,
            RevokeEntitlementRequest request,
            EntitlementUseCases useCases,
            HttpContext context,
            IMessageLocalizer localizer,
            CancellationToken ct) =>
            (await useCases.RevokeAsync(entitlementId, request, ct)).ToHttpResult(context, localizer))
            .RequireAuthorization(Permissions.Commerce.GrantPremium);

        return app;
    }
}
