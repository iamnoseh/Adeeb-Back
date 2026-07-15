using Adeeb.Modules.Mmt.Contracts;
using Adeeb.Modules.Mmt.Domain;
using Adeeb.SharedKernel.Errors;
using Adeeb.SharedKernel.Results;

namespace Adeeb.Modules.Mmt.Application;

internal static class MmtValidation
{
    public static Result ValidateCluster(string name, string code, string? description) => ValidateText([
        ("name", name, 160, true), ("code", code, 40, true), ("description", description, 2000, false)]);

    public static Result ValidateUniversity(string fullName, string? shortName, string city, int type, string? logoUrl)
    {
        var errors = TextErrors([("fullName", fullName, 300, true), ("shortName", shortName, 120, false), ("city", city, 120, true), ("logoUrl", logoUrl, 512, false)]);
        AddEnum<UniversityType>(errors, "type", type);
        if (!string.IsNullOrWhiteSpace(logoUrl) && (!Uri.TryCreate(logoUrl, UriKind.RelativeOrAbsolute, out _) || logoUrl.Contains('\n') || logoUrl.Contains('\r')))
            Add(errors, "logoUrl", "mmt.logo_url.invalid", "Validation.InvalidUrl");
        return Finish(errors);
    }

    public static Result ValidateSpecialty(string code, string name, string? description) => ValidateText([
        ("code", code, 60, true), ("name", name, 240, true), ("description", description, 2000, false)]);

    public static Result ValidateProgram(CreateAdmissionProgramDto r) => ValidateProgramValues(r.UniversityId, r.SpecialtyId, r.MmtClusterId,
        r.AdmissionType, r.StudyForm, r.StudyLanguage, r.AdmissionYear, r.SeatsCount);
    public static Result ValidateProgram(UpdateAdmissionProgramDto r) => ValidateProgramValues(r.UniversityId, r.SpecialtyId, r.MmtClusterId,
        r.AdmissionType, r.StudyForm, r.StudyLanguage, r.AdmissionYear, r.SeatsCount);

    public static Result ValidateScore(int year, decimal score, int? seats, string? source, string? note, int distributionRound)
    {
        var errors = TextErrors([("source", source, 500, false), ("note", note, 2000, false)]);
        if (year is < 2000 or > 2100) Add(errors, "year", "mmt.year.invalid", "MMT.YearInvalid");
        if (score <= 0 || score > 1000 || Scale(score) > 2) Add(errors, "passingScore", "mmt.score.invalid", "MMT.ScoreInvalid");
        if (seats < 0) Add(errors, "seatsCount", "mmt.seats.invalid", "MMT.SeatsInvalid");
        AddEnum<DistributionRound>(errors, "distributionRound", distributionRound);
        return Finish(errors);
    }

    public static bool IsYear(int year) => year is >= 2000 and <= 2100;
    public static int Scale(decimal value) => (decimal.GetBits(value)[3] >> 16) & 0xFF;

    private static Result ValidateProgramValues(Guid university, Guid specialty, Guid cluster, int admissionType, int form, int language, int year, int? seats)
    {
        var errors = new Dictionary<string, IReadOnlyList<Error>>();
        if (university == Guid.Empty) Add(errors, "universityId", "mmt.university.required", "Validation.Required");
        if (specialty == Guid.Empty) Add(errors, "specialtyId", "mmt.specialty.required", "Validation.Required");
        if (cluster == Guid.Empty) Add(errors, "mmtClusterId", "mmt.cluster.required", "Validation.Required");
        AddEnum<AdmissionType>(errors, "admissionType", admissionType);
        AddEnum<StudyForm>(errors, "studyForm", form);
        AddEnum<StudyLanguage>(errors, "studyLanguage", language);
        if (!IsYear(year)) Add(errors, "admissionYear", "mmt.year.invalid", "MMT.YearInvalid");
        if (seats < 0) Add(errors, "seatsCount", "mmt.seats.invalid", "MMT.SeatsInvalid");
        return Finish(errors);
    }

    private static Result ValidateText((string Key, string? Value, int Max, bool Required)[] fields) => Finish(TextErrors(fields));
    private static Dictionary<string, IReadOnlyList<Error>> TextErrors((string Key, string? Value, int Max, bool Required)[] fields)
    {
        var errors = new Dictionary<string, IReadOnlyList<Error>>();
        foreach (var (key, value, max, required) in fields)
        {
            if (required && string.IsNullOrWhiteSpace(value)) Add(errors, key, $"mmt.{key}.required", "Validation.Required");
            else if (value?.Trim().Length > max) Add(errors, key, $"mmt.{key}.too_long", "MMT.ValueTooLong");
        }
        return errors;
    }
    private static void AddEnum<T>(Dictionary<string, IReadOnlyList<Error>> errors, string key, int value) where T : struct, Enum
    { if (!Enum.IsDefined(typeof(T), value)) Add(errors, key, $"mmt.{key}.invalid", "MMT.EnumInvalid"); }
    private static void Add(Dictionary<string, IReadOnlyList<Error>> errors, string key, string code, string message) => errors[key] = [Error.Validation(code, message)];
    private static Result Finish(Dictionary<string, IReadOnlyList<Error>> errors) => errors.Count == 0 ? Result.Success() : Result.ValidationFailure(errors);
}
