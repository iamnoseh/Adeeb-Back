namespace Adeeb.Modules.Students.Domain.Students;

public sealed class StudentProfile
{
    public const int DisplayNameMaxLength = 80;
    public const int AvatarUrlMaxLength = 512;
    public const int RegionMaxLength = 80;
    public const int CityMaxLength = 80;
    public const int SchoolNameMaxLength = 160;
    public const int TimeZoneIdMaxLength = 100;
    public const int GenderMaxLength = 32;
    public const string DefaultTimeZoneId = "Asia/Dushanbe";
    public const short MinGrade = 1;
    public const short MaxGrade = 11;
    public static readonly DateOnly MinimumDateOfBirth = new(1900, 1, 1);

    private StudentProfile() { }

    public StudentProfile(Guid studentId, DateTimeOffset now)
    {
        StudentId = studentId;
        TimeZoneId = DefaultTimeZoneId;
        UpdatedAtUtc = now;
    }

    public Guid StudentId { get; private set; }
    public string? DisplayName { get; private set; }
    public string? AvatarUrl { get; private set; }
    public DateOnly? DateOfBirth { get; private set; }
    public string? Region { get; private set; }
    public string? City { get; private set; }
    public string? SchoolName { get; private set; }
    public short? Grade { get; private set; }
    public string? Gender { get; private set; }
    public string TimeZoneId { get; private set; } = DefaultTimeZoneId;
    public DateTimeOffset UpdatedAtUtc { get; private set; }

    public void ChangeTimeZone(string timeZoneId, DateTimeOffset now)
    {
        if (string.Equals(TimeZoneId, timeZoneId, StringComparison.Ordinal))
        {
            return;
        }

        TimeZoneId = timeZoneId;
        UpdatedAtUtc = now;
    }

    public void Update(
        string? displayName,
        string? avatarUrl,
        DateOnly? dateOfBirth,
        string? region,
        string? city,
        string? schoolName,
        short? grade,
        string? gender,
        DateTimeOffset now)
    {
        DisplayName = NormalizeOptional(displayName);
        AvatarUrl = NormalizeOptional(avatarUrl);
        if (DateOfBirth.HasValue && dateOfBirth.HasValue && DateOfBirth != dateOfBirth)
        {
            throw new InvalidOperationException("Date of birth cannot be changed once set.");
        }
        DateOfBirth = dateOfBirth ?? DateOfBirth;
        Region = NormalizeOptional(region);
        City = NormalizeOptional(city);
        SchoolName = NormalizeOptional(schoolName);
        Grade = grade;
        Gender = NormalizeGender(gender);
        UpdatedAtUtc = now;
    }

    public void ChangeAvatar(string avatarUrl, DateTimeOffset now)
    {
        AvatarUrl = NormalizeOptional(avatarUrl);
        UpdatedAtUtc = now;
    }

    public bool HasMeaningfulData() =>
        !string.IsNullOrWhiteSpace(DisplayName) ||
        !string.IsNullOrWhiteSpace(AvatarUrl) ||
        DateOfBirth.HasValue ||
        !string.IsNullOrWhiteSpace(Region) ||
        !string.IsNullOrWhiteSpace(City) ||
        !string.IsNullOrWhiteSpace(SchoolName) ||
        Grade.HasValue ||
        !string.IsNullOrWhiteSpace(Gender);

    private static string? NormalizeOptional(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static string? NormalizeGender(string? value)
    {
        var normalized = NormalizeOptional(value);
        if (normalized is null) return null;
        if (string.Equals(normalized, "female", StringComparison.OrdinalIgnoreCase)) return "Female";
        if (string.Equals(normalized, "male", StringComparison.OrdinalIgnoreCase)) return "Male";
        return normalized;
    }
}
