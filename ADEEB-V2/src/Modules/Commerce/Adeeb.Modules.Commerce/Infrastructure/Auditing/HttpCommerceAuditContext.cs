using System.Security.Claims;
using Adeeb.Modules.Commerce.Application.Auditing;
using Microsoft.AspNetCore.Http;

namespace Adeeb.Modules.Commerce.Infrastructure.Auditing;

public sealed class HttpCommerceAuditContext(IHttpContextAccessor accessor) : ICommerceAuditContext
{
    public CommerceAuditActor Current
    {
        get
        {
            var context = accessor.HttpContext;
            var idValue = context?.User.FindFirstValue(ClaimTypes.NameIdentifier) ?? context?.User.FindFirstValue("sub");
            return new CommerceAuditActor(
                Guid.TryParse(idValue, out var userId) ? userId : null,
                context?.Connection.RemoteIpAddress?.ToString(),
                context?.Request.Headers.UserAgent.ToString(),
                context?.TraceIdentifier);
        }
    }
}
