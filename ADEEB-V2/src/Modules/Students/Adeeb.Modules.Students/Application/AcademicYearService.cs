using Adeeb.Application.Abstractions.Time;

namespace Adeeb.Modules.Students.Application;

public sealed class AcademicYearService(IDateTimeProvider clock)
{
    public (int Start, int End) Current()
    {
        var local = clock.DushanbeNow;
        return Resolve(DateOnly.FromDateTime(local.DateTime));
    }

    public static (int Start, int End) Resolve(DateOnly date) =>
        date.Month >= 9 ? (date.Year, date.Year + 1) : (date.Year - 1, date.Year);

    public static int ExpectedGraduationYear(int academicYearEnd, short grade) => academicYearEnd + (11 - grade);
}
