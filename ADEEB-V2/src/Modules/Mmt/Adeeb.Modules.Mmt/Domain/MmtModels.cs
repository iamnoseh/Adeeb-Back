using System.Globalization;
using System.Text;
using Adeeb.Application.Abstractions.Localization;
using Adeeb.SharedKernel.Domain;

namespace Adeeb.Modules.Mmt.Domain;

public enum UniversityType { Public = 0, Private = 1, Other = 2 }
public enum AdmissionType { Budget = 0, Contract = 1 }
public enum StudyForm { FullTime = 0, PartTime = 1, Distance = 2, Other = 3 }
public enum StudyLanguage { Tajik = 0, Russian = 1, English = 2, Other = 3 }
public enum ExistingScoreMode { SkipExisting = 0, UpdateExisting = 1, FailOnExisting = 2 }

public static class MmtNormalization
{
    public static string Code(string value) => Collapse(value).ToUpperInvariant();
    public static string Name(string value) => Collapse(value);
    public static string NameKey(string value) => Collapse(value).ToUpperInvariant();

    private static string Collapse(string value)
    {
        var normalized = value.Trim().Normalize(NormalizationForm.FormC);
        return string.Join(' ', normalized.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));
    }
}

public sealed class MmtCluster : Entity
{
    private MmtCluster() { }
    public MmtCluster(Guid id, string name, string code, string? description, DateTimeOffset now)
    {
        Id = id;
        CreatedAtUtc = now;
        Update(name, code, description, true, now);
    }

    public string Name { get; private set; } = string.Empty;
    public string NameRu { get; private set; } = string.Empty;
    public string Code { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    public string? DescriptionRu { get; private set; }
    public bool IsActive { get; private set; }
    public DateTimeOffset CreatedAtUtc { get; private set; }
    public DateTimeOffset UpdatedAtUtc { get; private set; }
    public void Update(string name, string code, string? description, bool isActive, DateTimeOffset now)
        => UpdateTranslation(SupportedLanguage.Tajik, name, description, code, isActive, now, initializeMissing: true);

    public void UpdateTranslation(SupportedLanguage language, string name, string? description, string code, bool isActive, DateTimeOffset now, bool initializeMissing = false)
    {
        var normalizedName = MmtNormalization.Name(name);
        var normalizedDescription = string.IsNullOrWhiteSpace(description) ? null : description.Trim();
        if (language == SupportedLanguage.Russian) { NameRu = normalizedName; DescriptionRu = normalizedDescription; }
        else { Name = normalizedName; Description = normalizedDescription; }
        if (initializeMissing || string.IsNullOrWhiteSpace(NameRu)) { NameRu = normalizedName; DescriptionRu = normalizedDescription; }
        Code = MmtNormalization.Code(code);
        IsActive = isActive;
        UpdatedAtUtc = now;
    }
    public string NameFor(SupportedLanguage language) => language == SupportedLanguage.Russian && !string.IsNullOrWhiteSpace(NameRu) ? NameRu : Name;
    public string? DescriptionFor(SupportedLanguage language) => language == SupportedLanguage.Russian ? DescriptionRu ?? Description : Description;
    public void SetActive(bool active, DateTimeOffset now) { IsActive = active; UpdatedAtUtc = now; }
}

public sealed class University : Entity
{
    private University() { }
    public University(Guid id, string fullName, string? shortName, string city, UniversityType type, string? logoUrl, DateTimeOffset now)
    {
        Id = id;
        CreatedAtUtc = now;
        Update(fullName, shortName, city, type, logoUrl, true, now);
    }

    public string FullName { get; private set; } = string.Empty;
    public string FullNameRu { get; private set; } = string.Empty;
    public string NormalizedFullName { get; private set; } = string.Empty;
    public string? ShortName { get; private set; }
    public string? ShortNameRu { get; private set; }
    public string City { get; private set; } = string.Empty;
    public string CityRu { get; private set; } = string.Empty;
    public UniversityType Type { get; private set; }
    public string? LogoUrl { get; private set; }
    public bool IsActive { get; private set; }
    public DateTimeOffset CreatedAtUtc { get; private set; }
    public DateTimeOffset UpdatedAtUtc { get; private set; }
    public void Update(string fullName, string? shortName, string city, UniversityType type, string? logoUrl, bool isActive, DateTimeOffset now)
        => UpdateTranslation(SupportedLanguage.Tajik, fullName, shortName, city, type, logoUrl, isActive, now, initializeMissing: true);

    public void UpdateTranslation(SupportedLanguage language, string fullName, string? shortName, string city, UniversityType type, string? logoUrl, bool isActive, DateTimeOffset now, bool initializeMissing = false)
    {
        var normalizedFullName = MmtNormalization.Name(fullName);
        var normalizedShortName = string.IsNullOrWhiteSpace(shortName) ? null : MmtNormalization.Name(shortName);
        var normalizedCity = MmtNormalization.Name(city);
        if (language == SupportedLanguage.Russian) { FullNameRu = normalizedFullName; ShortNameRu = normalizedShortName; CityRu = normalizedCity; }
        else { FullName = normalizedFullName; ShortName = normalizedShortName; City = normalizedCity; NormalizedFullName = MmtNormalization.NameKey(fullName); }
        if (initializeMissing || string.IsNullOrWhiteSpace(FullNameRu)) { FullNameRu = normalizedFullName; ShortNameRu = normalizedShortName; CityRu = normalizedCity; }
        Type = type;
        LogoUrl = string.IsNullOrWhiteSpace(logoUrl) ? null : logoUrl.Trim();
        IsActive = isActive;
        UpdatedAtUtc = now;
    }
    public string FullNameFor(SupportedLanguage language) => language == SupportedLanguage.Russian && !string.IsNullOrWhiteSpace(FullNameRu) ? FullNameRu : FullName;
    public string? ShortNameFor(SupportedLanguage language) => language == SupportedLanguage.Russian ? ShortNameRu ?? ShortName : ShortName;
    public string CityFor(SupportedLanguage language) => language == SupportedLanguage.Russian && !string.IsNullOrWhiteSpace(CityRu) ? CityRu : City;
    public void SetActive(bool active, DateTimeOffset now) { IsActive = active; UpdatedAtUtc = now; }
}

public sealed class Specialty : Entity
{
    private Specialty() { }
    public Specialty(Guid id, string code, string name, string? description, DateTimeOffset now)
    {
        Id = id;
        CreatedAtUtc = now;
        Update(code, name, description, true, now);
    }

    public string Code { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;
    public string NameRu { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    public string? DescriptionRu { get; private set; }
    public bool IsActive { get; private set; }
    public DateTimeOffset CreatedAtUtc { get; private set; }
    public DateTimeOffset UpdatedAtUtc { get; private set; }
    public void Update(string code, string name, string? description, bool isActive, DateTimeOffset now)
        => UpdateTranslation(SupportedLanguage.Tajik, code, name, description, isActive, now, initializeMissing: true);

    public void UpdateTranslation(SupportedLanguage language, string code, string name, string? description, bool isActive, DateTimeOffset now, bool initializeMissing = false)
    {
        Code = MmtNormalization.Code(code);
        var normalizedName = MmtNormalization.Name(name);
        var normalizedDescription = string.IsNullOrWhiteSpace(description) ? null : description.Trim();
        if (language == SupportedLanguage.Russian) { NameRu = normalizedName; DescriptionRu = normalizedDescription; }
        else { Name = normalizedName; Description = normalizedDescription; }
        if (initializeMissing || string.IsNullOrWhiteSpace(NameRu)) { NameRu = normalizedName; DescriptionRu = normalizedDescription; }
        IsActive = isActive;
        UpdatedAtUtc = now;
    }
    public string NameFor(SupportedLanguage language) => language == SupportedLanguage.Russian && !string.IsNullOrWhiteSpace(NameRu) ? NameRu : Name;
    public string? DescriptionFor(SupportedLanguage language) => language == SupportedLanguage.Russian ? DescriptionRu ?? Description : Description;
    public void SetActive(bool active, DateTimeOffset now) { IsActive = active; UpdatedAtUtc = now; }
}

public sealed class AdmissionProgram : Entity
{
    private AdmissionProgram() { }
    public AdmissionProgram(Guid id, Guid universityId, Guid specialtyId, Guid clusterId, AdmissionType admissionType,
        StudyForm studyForm, StudyLanguage studyLanguage, int admissionYear, int? seatsCount, bool publish, DateTimeOffset now)
    {
        Id = id;
        CreatedAtUtc = now;
        Update(universityId, specialtyId, clusterId, admissionType, studyForm, studyLanguage, admissionYear, seatsCount, publish, true, now);
    }

    public Guid UniversityId { get; private set; }
    public Guid SpecialtyId { get; private set; }
    public Guid MmtClusterId { get; private set; }
    public AdmissionType AdmissionType { get; private set; }
    public StudyForm StudyForm { get; private set; }
    public StudyLanguage StudyLanguage { get; private set; }
    public int AdmissionYear { get; private set; }
    public int? SeatsCount { get; private set; }
    public bool IsPublished { get; private set; }
    public bool IsActive { get; private set; }
    public DateTimeOffset CreatedAtUtc { get; private set; }
    public DateTimeOffset UpdatedAtUtc { get; private set; }
    public University University { get; private set; } = null!;
    public Specialty Specialty { get; private set; } = null!;
    public MmtCluster MmtCluster { get; private set; } = null!;
    public IReadOnlyCollection<PassingScoreHistory> PassingScores => _passingScores;
    private readonly List<PassingScoreHistory> _passingScores = [];

    public void Update(Guid universityId, Guid specialtyId, Guid clusterId, AdmissionType admissionType,
        StudyForm studyForm, StudyLanguage studyLanguage, int admissionYear, int? seatsCount, bool publish, bool active, DateTimeOffset now)
    {
        UniversityId = universityId;
        SpecialtyId = specialtyId;
        MmtClusterId = clusterId;
        AdmissionType = admissionType;
        StudyForm = studyForm;
        StudyLanguage = studyLanguage;
        AdmissionYear = admissionYear;
        SeatsCount = seatsCount;
        IsPublished = publish;
        IsActive = active;
        UpdatedAtUtc = now;
    }
    public void SetActive(bool active, DateTimeOffset now) { IsActive = active; if (!active) IsPublished = false; UpdatedAtUtc = now; }
    public void SetPublished(bool published, DateTimeOffset now) { IsPublished = published; UpdatedAtUtc = now; }
}

public sealed class PassingScoreHistory : Entity
{
    private PassingScoreHistory() { }
    public PassingScoreHistory(Guid id, Guid admissionProgramId, int year, decimal passingScore, int? seatsCount, string? source, string? note, DateTimeOffset now)
    {
        Id = id;
        AdmissionProgramId = admissionProgramId;
        CreatedAtUtc = now;
        Update(year, passingScore, seatsCount, source, note, now);
    }
    public Guid AdmissionProgramId { get; private set; }
    public int Year { get; private set; }
    public decimal PassingScore { get; private set; }
    public int? SeatsCount { get; private set; }
    public string? Source { get; private set; }
    public string? Note { get; private set; }
    public DateTimeOffset CreatedAtUtc { get; private set; }
    public DateTimeOffset UpdatedAtUtc { get; private set; }
    public AdmissionProgram AdmissionProgram { get; private set; } = null!;
    public void Update(int year, decimal score, int? seats, string? source, string? note, DateTimeOffset now)
    {
        Year = year;
        PassingScore = score;
        SeatsCount = seats;
        Source = string.IsNullOrWhiteSpace(source) ? null : source.Trim();
        Note = string.IsNullOrWhiteSpace(note) ? null : note.Trim();
        UpdatedAtUtc = now;
    }
}
