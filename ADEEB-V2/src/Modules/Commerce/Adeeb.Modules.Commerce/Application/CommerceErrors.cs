using Adeeb.SharedKernel.Errors;

namespace Adeeb.Modules.Commerce.Application;

public static class CommerceErrors
{
    public static readonly Error StudentRequired = Error.Conflict("commerce.student_required", "Commerce.StudentRequired");
}
