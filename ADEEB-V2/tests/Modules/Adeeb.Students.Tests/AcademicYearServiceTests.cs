using Adeeb.Application.Abstractions.Time;
using Adeeb.Modules.Students.Application;

namespace Adeeb.Students.Tests;

public sealed class AcademicYearServiceTests
{
    [Theory]
    [InlineData(2026, 8, 31, 2025, 2026)]
    [InlineData(2026, 9, 1, 2026, 2027)]
    [InlineData(2027, 1, 10, 2026, 2027)]
    public void Resolve_uses_September_as_the_start_of_an_academic_year(int year, int month, int day, int expectedStart, int expectedEnd)
    {
        var result = AcademicYearService.Resolve(new DateOnly(year, month, day));

        Assert.Equal((expectedStart, expectedEnd), result);
    }

    [Fact]
    public void Current_uses_Dushanbe_time_instead_of_utc_date()
    {
        var service = new AcademicYearService(new FixedClock(
            new DateTimeOffset(2026, 8, 31, 21, 0, 0, TimeSpan.Zero),
            new DateTimeOffset(2026, 9, 1, 2, 0, 0, TimeSpan.FromHours(5))));

        Assert.Equal((2026, 2027), service.Current());
    }

    [Theory]
    [InlineData((short)1, 2037)]
    [InlineData((short)11, 2027)]
    public void Expected_graduation_year_is_based_on_the_current_grade(short grade, int expected)
    {
        Assert.Equal(expected, AcademicYearService.ExpectedGraduationYear(2027, grade));
    }

    private sealed class FixedClock(DateTimeOffset utcNow, DateTimeOffset dushanbeNow) : IDateTimeProvider
    {
        public DateTimeOffset UtcNow => utcNow;
        public DateTimeOffset DushanbeNow => dushanbeNow;
        public DateTimeOffset ToDushanbeTime(DateTimeOffset value) => value.ToOffset(TimeSpan.FromHours(5));
    }
}
