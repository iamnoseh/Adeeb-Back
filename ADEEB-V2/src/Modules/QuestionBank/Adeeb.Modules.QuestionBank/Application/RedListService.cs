using Adeeb.Application.Abstractions.Localization;
using Adeeb.Application.Abstractions.Time;
using Adeeb.Modules.QuestionBank.Contracts;
using Adeeb.Modules.QuestionBank.Domain;
using Adeeb.Modules.QuestionBank.Infrastructure.Persistence;
using Adeeb.SharedKernel.Results;
using Microsoft.EntityFrameworkCore;

namespace Adeeb.Modules.QuestionBank.Application;

public sealed class RedListService(QuestionBankDbContext db, IDateTimeProvider clock)
{
    public enum AnswerAction { None = 0, Added = 1, Progressed = 2, Reset = 3, Mastered = 4 }
    public sealed record AnswerOutcome(Guid? ItemId, AnswerAction Action, int CorrectStreak,
        int RequiredCorrectStreak, int CorrectAnswersRemaining);
    public sealed record AnswerUpdate(Guid QuestionId, Guid SubjectId, Guid? TopicId,
        QuestionType Type, bool IsCorrect);

    public async Task<AnswerOutcome> ApplyAnswerAsync(Guid userId, AnswerUpdate answer, CancellationToken ct)
    {
        if (answer.Type == QuestionType.Matching)
            return new(null, AnswerAction.None, 0, StudentRedListItem.RequiredCorrectStreak,
                StudentRedListItem.RequiredCorrectStreak);

        await RedListConcurrency.AcquireAsync(db, userId, answer.QuestionId, ct);
        var item = await db.StudentRedListItems.SingleOrDefaultAsync(
            x => x.UserId == userId && x.QuestionId == answer.QuestionId, ct);
        var wasActive = item?.Status == RedListStatus.Active;
        AnswerAction action;

        if (!answer.IsCorrect)
        {
            if (item is null)
            {
                item = new StudentRedListItem(Guid.NewGuid(), userId, answer.QuestionId, answer.SubjectId,
                    answer.TopicId, answer.Type, clock.UtcNow);
                db.StudentRedListItems.Add(item);
            }
            else item.RecordWrong(clock.UtcNow);
            action = wasActive ? AnswerAction.Reset : AnswerAction.Added;
        }
        else if (item is not null && item.Status == RedListStatus.Active)
        {
            item.RecordCorrect(clock.UtcNow);
            action = item.Status == RedListStatus.Mastered ? AnswerAction.Mastered : AnswerAction.Progressed;
        }
        else
        {
            return new(item?.Id, AnswerAction.None, item?.CorrectStreak ?? 0,
                StudentRedListItem.RequiredCorrectStreak,
                Math.Max(0, StudentRedListItem.RequiredCorrectStreak - (item?.CorrectStreak ?? 0)));
        }

        return new(item.Id, action, item.CorrectStreak, StudentRedListItem.RequiredCorrectStreak,
            Math.Max(0, StudentRedListItem.RequiredCorrectStreak - item.CorrectStreak));
    }

    public async Task<IReadOnlyList<AnswerOutcome>> ApplyAnswersAsync(Guid userId,
        IReadOnlyCollection<AnswerUpdate> answers, CancellationToken ct)
    {
        var eligible = answers.Where(x => x.Type != QuestionType.Matching).ToList();
        if (eligible.Count == 0) return [];

        var questionIds = eligible.Select(x => x.QuestionId).Distinct().ToArray();
        var existing = await db.StudentRedListItems
            .Where(x => x.UserId == userId && questionIds.Contains(x.QuestionId))
            .ToDictionaryAsync(x => x.QuestionId, ct);

        var outcomes = new List<AnswerOutcome>(eligible.Count);
        foreach (var answer in eligible)
        {
            existing.TryGetValue(answer.QuestionId, out var item);
            var wasActive = item?.Status == RedListStatus.Active;
            AnswerAction action;
            if (!answer.IsCorrect)
            {
                if (item is null)
                {
                    item = new StudentRedListItem(Guid.NewGuid(), userId, answer.QuestionId, answer.SubjectId,
                        answer.TopicId, answer.Type, clock.UtcNow);
                    db.StudentRedListItems.Add(item);
                    existing.Add(answer.QuestionId, item);
                }
                else item.RecordWrong(clock.UtcNow);
                action = wasActive ? AnswerAction.Reset : AnswerAction.Added;
            }
            else if (item is not null && item.Status == RedListStatus.Active)
            {
                item.RecordCorrect(clock.UtcNow);
                action = item.Status == RedListStatus.Mastered ? AnswerAction.Mastered : AnswerAction.Progressed;
            }
            else continue;
            outcomes.Add(new(item.Id, action, item.CorrectStreak, StudentRedListItem.RequiredCorrectStreak,
                Math.Max(0, StudentRedListItem.RequiredCorrectStreak - item.CorrectStreak)));
        }
        return outcomes;
    }

    public async Task<Result<TestingPageDto<RedListItemDto>>> GetAsync(Guid userId, RedListQuery filter,
        SupportedLanguage language, CancellationToken ct)
    {
        var page = Math.Max(1, filter.Page); var size = Math.Clamp(filter.PageSize, 1, 50);
        var query = db.StudentRedListItems.AsNoTracking().Where(x => x.UserId == userId);
        if (filter.SubjectId.HasValue) query = query.Where(x => x.SubjectId == filter.SubjectId.Value);
        if (filter.Status.HasValue && Enum.IsDefined(typeof(RedListStatus), filter.Status.Value))
        { var status = (RedListStatus)filter.Status.Value; query = query.Where(x => x.Status == status); }
        var total = await query.CountAsync(ct);
        var rows = await query.OrderByDescending(x => x.Status == RedListStatus.Active).ThenByDescending(x => x.LastPracticedAtUtc)
            .Skip((page - 1) * size).Take(size).ToListAsync(ct);
        var questionIds = rows.Select(x => x.QuestionId).ToArray();
        var questions = await db.Questions.AsNoTracking().Include(x => x.Translations)
            .Where(x => questionIds.Contains(x.Id)).ToDictionaryAsync(x => x.Id, ct);
        var items = rows.Select(x => new RedListItemDto(x.Id, x.QuestionId, x.SubjectId, x.TopicId, (int)x.QuestionType,
            x.WrongCount, x.CorrectStreak, x.LastWrongAtUtc, x.LastPracticedAtUtc, (int)x.Status,
            questions.TryGetValue(x.QuestionId, out var question) ? question.ContentFor(language) : string.Empty)).ToList();
        return Result<TestingPageDto<RedListItemDto>>.Success(new(items, page, size, total));
    }

    public async Task<Result<RedListSummaryDto>> SummaryAsync(Guid userId, CancellationToken ct)
    {
        var values = await db.StudentRedListItems.AsNoTracking().Where(x => x.UserId == userId).ToListAsync(ct);
        return Result<RedListSummaryDto>.Success(new(
            values.Count(x => x.Status == RedListStatus.Active), values.Count(x => x.Status == RedListStatus.Mastered),
            values.Count(x => x.Status == RedListStatus.Archived),
            values.Where(x => x.Status == RedListStatus.Active).GroupBy(x => x.SubjectId)
                .Select(x => new RedListSubjectSummaryDto(x.Key, x.Count())).ToList()));
    }

    public async Task<Result> ArchiveAsync(Guid userId, Guid questionId, CancellationToken ct) =>
        await SetStatusAsync(userId, questionId, restore: false, ct);
    public async Task<Result> RestoreAsync(Guid userId, Guid questionId, CancellationToken ct) =>
        await SetStatusAsync(userId, questionId, restore: true, ct);

    private async Task<Result> SetStatusAsync(Guid userId, Guid questionId, bool restore, CancellationToken ct)
    {
        var item = await db.StudentRedListItems.SingleOrDefaultAsync(x => x.UserId == userId && x.QuestionId == questionId, ct);
        if (item is null) return Result.Failure(StudentTestingErrors.RedListItemNotFound);
        if (restore) item.Restore(); else item.Archive();
        await db.SaveChangesAsync(ct); return Result.Success();
    }
}
