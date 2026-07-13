using System.Security.Claims;
using Adeeb.Modules.Commerce.Contracts;
using Adeeb.SharedKernel.Results;

namespace Adeeb.Modules.Commerce.Application.Entitlements;

public sealed class EntitlementUseCases(CommerceService service)
{
    public Task<Result<StudentEntitlementSummaryResponse>> GetCurrentAsync(
        ClaimsPrincipal principal,
        CancellationToken cancellationToken) =>
        service.GetCurrentEntitlementsAsync(principal, cancellationToken);

    public Task<Result<StudentEntitlementResponse>> GrantPremiumAsync(
        Guid studentId,
        GrantPremiumEntitlementRequest request,
        CancellationToken cancellationToken) =>
        service.GrantPremiumAsync(studentId, request, cancellationToken);

    public Task<Result<StudentEntitlementResponse>> RevokeAsync(
        Guid entitlementId,
        RevokeEntitlementRequest request,
        CancellationToken cancellationToken) =>
        service.RevokeEntitlementAsync(entitlementId, request, cancellationToken);
}
