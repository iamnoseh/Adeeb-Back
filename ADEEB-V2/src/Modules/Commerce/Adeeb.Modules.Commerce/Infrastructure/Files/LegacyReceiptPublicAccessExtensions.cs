using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;

namespace Adeeb.Modules.Commerce.Infrastructure.Files;

public static class LegacyReceiptPublicAccessExtensions
{
    public static IApplicationBuilder UseLegacyReceiptPublicAccessBlock(this IApplicationBuilder app) =>
        app.Use(async (context, next) =>
        {
            if (context.Request.Path.StartsWithSegments("/uploads/commerce/receipts", StringComparison.OrdinalIgnoreCase))
            {
                context.Response.StatusCode = StatusCodes.Status404NotFound;
                return;
            }

            await next(context);
        });
}
