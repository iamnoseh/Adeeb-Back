using Adeeb.Modules.QuestionBank.Domain;
using Adeeb.Modules.QuestionBank.Infrastructure.Persistence;
using Adeeb.SharedKernel.Results;
using Microsoft.EntityFrameworkCore;

namespace Adeeb.Modules.QuestionBank.Application;

public sealed record QuestionPickerRequest(Guid UserId, TestMode Mode, int Count, Guid? SubjectId = null,
    IReadOnlyCollection<Guid>? ClusterSubjectIds = null, bool InjectRedList = false,
    QuestionType? RequiredType = null, IReadOnlyCollection<Guid>? ExcludedQuestionIds = null);

public interface IQuestionPickerService
{
    Task<Result<IReadOnlyList<Question>>> PickAsync(QuestionPickerRequest request, CancellationToken ct);
}

public interface ITestingRandomizer
{
    void Shuffle<T>(IList<T> values);
}

internal sealed class TestingRandomizer : ITestingRandomizer
{
    public void Shuffle<T>(IList<T> values)
    {
        for (var index = values.Count - 1; index > 0; index--)
        {
            var swap = Random.Shared.Next(index + 1);
            (values[index], values[swap]) = (values[swap], values[index]);
        }
    }
}

internal sealed class QuestionPickerService(QuestionBankDbContext db, ITestingRandomizer randomizer) : IQuestionPickerService
{
    public async Task<Result<IReadOnlyList<Question>>> PickAsync(QuestionPickerRequest request, CancellationToken ct)
    {
        var eligible = db.Questions.AsNoTracking()
            .Include(x => x.Translations).Include(x => x.AnswerOptions).ThenInclude(x => x.Translations)
            .Where(x => x.Status == QuestionStatus.Active);

        if (request.Mode == TestMode.SubjectTest)
            eligible = eligible.Where(x => x.SubjectId == request.SubjectId);
        else if (request.Mode is TestMode.MmtPractice or TestMode.MonthlyExam)
        {
            var subjects = request.ClusterSubjectIds?.Distinct().ToArray() ?? [];
            eligible = eligible.Where(x => subjects.Contains(x.SubjectId));
        }
        if (request.RequiredType.HasValue) eligible = eligible.Where(x => x.Type == request.RequiredType.Value);
        if (request.ExcludedQuestionIds is { Count: > 0 })
        {
            var excluded = request.ExcludedQuestionIds.ToArray();
            eligible = eligible.Where(x => !excluded.Contains(x.Id));
        }

        var redItems = await db.StudentRedListItems.AsNoTracking()
            .Where(x => x.UserId == request.UserId && x.Status == RedListStatus.Active
                && x.QuestionType != QuestionType.Matching)
            .ToListAsync(ct);

        if (request.Mode == TestMode.RedListPractice)
        {
            if (redItems.Count < request.Count)
                return Result<IReadOnlyList<Question>>.Failure(StudentTestingErrors.NotEnoughRedListQuestions);
            var ids = redItems.Select(x => x.QuestionId).ToArray();
            var redQuestions = await eligible.Where(x => ids.Contains(x.Id) && x.Type != QuestionType.Matching).ToListAsync(ct);
            if (redQuestions.Count < request.Count)
                return Result<IReadOnlyList<Question>>.Failure(StudentTestingErrors.NotEnoughRedListQuestions);
            randomizer.Shuffle(redQuestions);
            return Result<IReadOnlyList<Question>>.Success(redQuestions.Take(request.Count).ToList());
        }

        var all = await eligible.ToListAsync(ct);
        var selected = new List<Question>(request.Count);
        if (request.InjectRedList && request.Mode is TestMode.SubjectTest or TestMode.MmtPractice)
        {
            var allowedSubjects = request.Mode == TestMode.SubjectTest
                ? new HashSet<Guid>(request.SubjectId.HasValue ? [request.SubjectId.Value] : [])
                : new HashSet<Guid>(request.ClusterSubjectIds ?? []);
            var redIds = redItems.Where(x => allowedSubjects.Contains(x.SubjectId)).Select(x => x.QuestionId).ToHashSet();
            var injectable = all.Where(x => redIds.Contains(x.Id) && x.Type != QuestionType.Matching).ToList();
            randomizer.Shuffle(injectable);
            var injectionCount = Math.Min(4, Math.Max(3, (int)Math.Ceiling(request.Count * 0.20m)));
            selected.AddRange(injectable.Take(Math.Min(injectionCount, request.Count)));
        }

        var remaining = all.Where(x => selected.All(chosen => chosen.Id != x.Id)).ToList();
        randomizer.Shuffle(remaining);
        while (selected.Count < request.Count && remaining.Count > 0)
        {
            var subjectCounts = selected.GroupBy(x => x.SubjectId).ToDictionary(x => x.Key, x => x.Count());
            var topicCounts = selected.GroupBy(x => x.TopicId ?? Guid.Empty).ToDictionary(x => x.Key, x => x.Count());
            var difficultyCounts = selected.GroupBy(x => x.Difficulty).ToDictionary(x => x.Key, x => x.Count());
            var next = remaining.OrderBy(x => subjectCounts.GetValueOrDefault(x.SubjectId) * 100
                + topicCounts.GetValueOrDefault(x.TopicId ?? Guid.Empty) * 10
                + difficultyCounts.GetValueOrDefault(x.Difficulty)).First();
            selected.Add(next); remaining.Remove(next);
        }

        return selected.Count < request.Count
            ? Result<IReadOnlyList<Question>>.Failure(StudentTestingErrors.NotEnoughQuestions)
            : Result<IReadOnlyList<Question>>.Success(selected);
    }
}
