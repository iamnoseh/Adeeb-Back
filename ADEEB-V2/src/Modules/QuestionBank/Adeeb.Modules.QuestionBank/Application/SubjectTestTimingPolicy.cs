using Adeeb.Application.Abstractions.AcademicCatalog;
using Adeeb.Application.Abstractions.Localization;
using Microsoft.Extensions.Options;

namespace Adeeb.Modules.QuestionBank.Application;

public interface ISubjectTestTimingPolicy
{
    Task<int> DurationMinutesAsync(Guid subjectId, int questionCount, SupportedLanguage language, CancellationToken ct);
}

internal sealed class SubjectTestTimingPolicy(
    IAcademicSubjectLookup subjects,
    IOptions<StudentTestingOptions> options) : ISubjectTestTimingPolicy
{
    public async Task<int> DurationMinutesAsync(
        Guid subjectId,
        int questionCount,
        SupportedLanguage language,
        CancellationToken ct)
    {
        var subject = (await subjects.GetActiveSubjectsAsync([subjectId], language, ct)).SingleOrDefault();
        var configuredCodes = (options.Value.ExtendedTimeSubjectCodes ?? [])
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Select(NormalizeCode)
            .ToHashSet(StringComparer.Ordinal);
        var minutesPerQuestion = subject is not null && configuredCodes.Contains(NormalizeCode(subject.Code))
            ? options.Value.ExtendedMinutesPerSubjectQuestion
            : options.Value.MinutesPerSubjectQuestion;
        return checked(questionCount * minutesPerQuestion);
    }

    private static string NormalizeCode(string code) => code.Trim().ToUpperInvariant();
}
