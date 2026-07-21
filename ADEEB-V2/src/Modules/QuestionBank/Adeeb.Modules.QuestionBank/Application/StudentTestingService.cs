using System.Data;
using System.Security.Claims;
using System.Text.Json;
using Adeeb.Application.Abstractions.Localization;
using Adeeb.Application.Abstractions.Progression;
using Adeeb.Application.Abstractions.Testing;
using Adeeb.Application.Abstractions.Time;
using Adeeb.Modules.QuestionBank.Application.Assessment;
using Adeeb.Modules.QuestionBank.Contracts;
using Adeeb.Modules.QuestionBank.Domain;
using Adeeb.Modules.QuestionBank.Infrastructure.Persistence;
using Adeeb.SharedKernel.Results;
using Adeeb.SharedKernel.Progression;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Adeeb.Modules.QuestionBank.Application;

public sealed class StudentTestingService(
    QuestionBankDbContext db,
    IQuestionPickerService picker,
    IAnswerEvaluationService evaluator,
    RedListService redList,
    IStudentMmtTestingContext mmtContext,
    IMonthlyExamAvailabilityService monthlyAvailability,
    ISubjectTestTimingPolicy subjectTiming,
    IDateTimeProvider clock,
    IOptions<StudentTestingOptions> options,
    ITestingRandomizer randomizer,
    ITestXpPolicy xpPolicy,
    IStudentXpService studentXpService,
    ILogger<StudentTestingService> logger)
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);
    private static readonly int[] SubjectCounts = [15, 20, 25];

    public async Task<Result<StudentTestingConfigDto>> ConfigAsync(Guid userId, CancellationToken ct)
    {
        var availability = monthlyAvailability.Current();
        var value = options.Value;
        return Result<StudentTestingConfigDto>.Success(new(SubjectCounts, value.RedListMinimumQuestions,
            value.RedListDefaultQuestions, value.MmtPracticeDefaultQuestions, value.MonthlyExamQuestionCount,
            value.MmtDurationMinutes, availability.IsOpen, availability.ClosesAtUtc));
    }

    public async Task<Result<TestAttemptDto>> StartSubjectAsync(Guid userId, StartSubjectTestRequest request,
        SupportedLanguage language, CancellationToken ct)
    {
        if (request.SubjectId == Guid.Empty || !SubjectCounts.Contains(request.QuestionCount))
            return Result<TestAttemptDto>.Failure(StudentTestingErrors.InvalidQuestionCount);
        var duration = await subjectTiming.DurationMinutesAsync(request.SubjectId, request.QuestionCount, language, ct);
        return await StartAsync(userId, TestMode.SubjectTest, request.QuestionCount, request.SubjectId, null, null,
            request.IncludeRedList, language, duration, ct);
    }

    public async Task<Result<TestAttemptDto>> StartMmtPracticeAsync(Guid userId, StartMmtPracticeRequest request,
        SupportedLanguage language, CancellationToken ct)
    {
        var context = await mmtContext.GetAsync(userId, ct);
        if (context is null) return Result<TestAttemptDto>.Failure(StudentTestingErrors.ProfileRequired);
        var count = request.QuestionCount ?? options.Value.MmtPracticeDefaultQuestions;
        if (count is < 1 or > 200) return Result<TestAttemptDto>.Failure(StudentTestingErrors.InvalidQuestionCount);
        return await StartAsync(userId, TestMode.MmtPractice, count, null, context.ClusterId, context.SubjectIds,
            !request.StrictSimulation, language, options.Value.MmtDurationMinutes, ct);
    }

    public async Task<Result<TestAttemptDto>> StartMonthlyExamAsync(Guid userId, SupportedLanguage language, CancellationToken ct)
    {
        var context = await mmtContext.GetAsync(userId, ct);
        if (context is null) return Result<TestAttemptDto>.Failure(StudentTestingErrors.ProfileRequired);
        if (context.AdmissionChoicesCount != 12) return Result<TestAttemptDto>.Failure(StudentTestingErrors.ChoicesRequired);
        var availability = monthlyAvailability.Current();
        if (!availability.IsOpen) return Result<TestAttemptDto>.Failure(StudentTestingErrors.MonthlyExamClosed);
        if (await db.TestAttempts.AsNoTracking().AnyAsync(x => x.UserId == userId && x.Mode == TestMode.MonthlyExam
            && x.MonthlyWindowKey == availability.WindowKey, ct))
            return Result<TestAttemptDto>.Failure(StudentTestingErrors.MonthlyExamAlreadyStarted);
        return await StartAsync(userId, TestMode.MonthlyExam, options.Value.MonthlyExamQuestionCount, null,
            context.ClusterId, context.SubjectIds, false, language, options.Value.MmtDurationMinutes, ct,
            availability.WindowKey);
    }

    public Task<Result<TestAttemptDto>> StartRedListAsync(Guid userId, StartRedListPracticeRequest request,
        SupportedLanguage language, CancellationToken ct)
    {
        var count = request.QuestionCount ?? options.Value.RedListDefaultQuestions;
        if (count < options.Value.RedListMinimumQuestions || count > 50)
            return Task.FromResult(Result<TestAttemptDto>.Failure(StudentTestingErrors.InvalidQuestionCount));
        return StartAsync(userId, TestMode.RedListPractice, count, null, null, null, false, language,
            count * options.Value.MinutesPerSubjectQuestion, ct);
    }

    public async Task<Result<TestAttemptDto>> GetAttemptAsync(Guid userId, Guid attemptId, CancellationToken ct)
    {
        var attempt = await AttemptQuery().SingleOrDefaultAsync(x => x.Id == attemptId && x.UserId == userId, ct);
        return attempt is null ? Result<TestAttemptDto>.Failure(StudentTestingErrors.AttemptNotFound)
            : Result<TestAttemptDto>.Success(ToAttemptDto(attempt));
    }

    public async Task<Result<TestResultDto>> SubmitAsync(Guid userId, Guid attemptId, SubmitAttemptRequest request, CancellationToken ct)
    {
        await using var transaction = await BeginTransactionAsync(ct);
        await TestAttemptFinalizationConcurrency.AcquireAttemptLockAsync(db, attemptId, ct);
        var attempt = await db.TestAttempts.Include(x => x.Questions).Include(x => x.Answers)
            .SingleOrDefaultAsync(x => x.Id == attemptId && x.UserId == userId, ct);
        if (attempt is null) return Result<TestResultDto>.Failure(StudentTestingErrors.AttemptNotFound);
        if (attempt.Status != TestAttemptStatus.InProgress)
        {
            logger.LogWarning("Test attempt completion rejected because the attempt is not in progress. UserId={UserId} AttemptId={AttemptId} Mode={TestMode} Status={Status}",
                userId, attemptId, attempt.Mode, attempt.Status);
            return Result<TestResultDto>.Failure(StudentTestingErrors.AttemptAlreadySubmitted);
        }

        logger.LogInformation("Test attempt completion started. UserId={UserId} AttemptId={AttemptId} TestMode={TestMode}",
            userId, attemptId, attempt.Mode);

        var automatic = clock.UtcNow >= attempt.ExpiresAtUtc;
        var submitted = request.Answers.GroupBy(x => x.QuestionId).ToDictionary(x => x.Key, x => x.Last());
        var storedResults = new List<StoredAnswerResult>(attempt.QuestionCount);
        var redListUpdates = new List<RedListService.AnswerUpdate>(attempt.QuestionCount);
        var correct = 0;

        foreach (var attemptQuestion in attempt.Questions.OrderBy(x => x.DisplayOrder))
        {
            var snapshot = DeserializeQuestion(attemptQuestion.QuestionSnapshotJson);
            submitted.TryGetValue(attemptQuestion.QuestionId, out var answer);
            var input = answer is null ? new AnswerEvaluationInput() : new(answer.SelectedOptionId, answer.TextResponse, answer.MatchingPairs);
            var question = SnapshotQuestion(snapshot);
            var evaluation = evaluator.Evaluate(question, input, snapshot.Language);
            if (evaluation.IsCorrect) correct++;
            var displayAnswer = DisplayAnswer(snapshot, answer, evaluation);
            var answerSnapshot = new StoredAnswerSnapshot(answer?.SelectedOptionId, answer?.TextResponse, answer?.MatchingPairs, displayAnswer);
            db.TestAttemptAnswers.Add(new(Guid.NewGuid(), attempt.Id, attemptQuestion.Id, attemptQuestion.QuestionId,
                JsonSerializer.Serialize(answerSnapshot, Json), evaluation.IsAnswered, evaluation.IsCorrect,
                evaluation.CorrectPairsCount, evaluation.TotalPairsCount, clock.UtcNow));
            redListUpdates.Add(new(snapshot.QuestionId, snapshot.SubjectId, snapshot.TopicId,
                (QuestionType)snapshot.Type, evaluation.IsCorrect));
            storedResults.Add(new(snapshot.QuestionId, snapshot.SubjectId, evaluation.IsAnswered, evaluation.IsCorrect, snapshot.Content,
                displayAnswer, CorrectAnswer(snapshot), snapshot.Explanation, snapshot.TopicId, snapshot.Difficulty,
                evaluation.CorrectPairsCount, evaluation.TotalPairsCount));
        }

        await redList.ApplyAnswersAsync(userId, redListUpdates, ct);

        var wrong = attempt.QuestionCount - correct;
        var percentage = attempt.QuestionCount == 0 ? 0 : decimal.Round(correct * 100m / attempt.QuestionCount, 2);
        var breakdown = storedResults.GroupBy(x => x.TopicId)
            .Select(group => new TopicBreakdownDto(group.Key, group.Count(), group.Count(x => x.IsCorrect), group.Count(x => !x.IsCorrect))).ToList();
        TestXpCalculation xp;
        try
        {
            xp = xpPolicy.Calculate(storedResults
                .Select(x => new TestXpQuestionOutcome((DifficultyLevel)x.Difficulty, x.IsCorrect)).ToList(), isCompleted: true);
        }
        catch (TestXpCalculationException exception)
        {
            logger.LogError(exception,
                "Test XP calculation failed. UserId={UserId} AttemptId={AttemptId} TestMode={TestMode} CorrectCount={CorrectCount}",
                userId, attemptId, attempt.Mode, correct);
            return Result<TestResultDto>.Failure(StudentTestingErrors.RewardCalculationFailed);
        }

        logger.LogInformation(
            "Test XP calculated. UserId={UserId} AttemptId={AttemptId} TestMode={TestMode} CorrectCount={CorrectCount} EasyCorrectCount={EasyCorrectCount} MediumCorrectCount={MediumCorrectCount} HardCorrectCount={HardCorrectCount} TotalXpUnits={TotalXpUnits}",
            userId, attemptId, attempt.Mode, correct, xp.EasyCorrectCount, xp.MediumCorrectCount,
            xp.HardCorrectCount, xp.TotalXpUnits);

        var grant = await studentXpService.GrantAsync(new(
            userId,
            XpSourceType.TestAttempt,
            TestXpSourceIdentity.SourceId(attempt.Id),
            xp.TotalXpUnits,
            TestXpSourceIdentity.IdempotencyKey(attempt.Id),
            xp.TotalXpUnits > 0 ? XpEntryType.Credit : XpEntryType.Settlement,
            new Dictionary<string, string>(StringComparer.Ordinal)
            {
                ["attemptId"] = attempt.Id.ToString("N"),
                ["testMode"] = attempt.Mode.ToString()
            }), ct);
        if (grant.IsFailure || grant.Value!.WasAlreadyProcessed)
        {
            logger.LogWarning(
                "Test XP grant rejected. UserId={UserId} AttemptId={AttemptId} TestMode={TestMode} ErrorCode={ErrorCode} WasAlreadyProcessed={WasAlreadyProcessed}",
                userId, attemptId, attempt.Mode, grant.Error?.Code, grant.Value?.WasAlreadyProcessed);
            return Result<TestResultDto>.Failure(StudentTestingErrors.RewardConflict);
        }

        var settlement = new TestXpSettlement(Guid.NewGuid(), attempt.Id, grant.Value.LedgerEntryId,
            xp.EasyCorrectCount, xp.MediumCorrectCount, xp.HardCorrectCount,
            xp.AnswerXpUnits, xp.CompletionBonusXpUnits, xp.TotalXpUnits, clock.UtcNow);
        db.TestXpSettlements.Add(settlement);

        attempt.Complete(correct, wrong, correct, percentage, clock.UtcNow, automatic);
        db.TestAttemptResults.Add(new(Guid.NewGuid(), attempt.Id, JsonSerializer.Serialize(breakdown, Json),
            JsonSerializer.Serialize(storedResults, Json), clock.UtcNow));
        try
        {
            await db.SaveChangesAsync(ct);
        }
        catch (Exception exception)
        {
            logger.LogError(exception,
                "Test attempt completion transaction failed and will be rolled back. UserId={UserId} AttemptId={AttemptId} TestMode={TestMode}",
                userId, attemptId, attempt.Mode);
            throw;
        }
        if (transaction is not null) await transaction.CommitAsync(ct);
        logger.LogInformation(
            "Test XP reward committed. UserId={UserId} AttemptId={AttemptId} TestMode={TestMode} RewardId={RewardId} TotalXpUnits={TotalXpUnits}",
            userId, attemptId, attempt.Mode, settlement.LedgerEntryId, settlement.TotalXpUnits);
        return Result<TestResultDto>.Success(ToResultDto(attempt, breakdown, storedResults, settlement));
    }

    public async Task<Result<TestResultDto>> GetResultAsync(Guid userId, Guid attemptId, CancellationToken ct)
    {
        var attempt = await db.TestAttempts.AsNoTracking().Include(x => x.Result).Include(x => x.XpSettlement)
            .SingleOrDefaultAsync(x => x.Id == attemptId && x.UserId == userId, ct);
        if (attempt?.Result is null || attempt.Status == TestAttemptStatus.InProgress)
            return Result<TestResultDto>.Failure(StudentTestingErrors.AttemptNotFound);
        var breakdown = JsonSerializer.Deserialize<List<TopicBreakdownDto>>(attempt.Result.TopicBreakdownJson, Json) ?? [];
        var answers = JsonSerializer.Deserialize<List<StoredAnswerResult>>(attempt.Result.ResultSnapshotJson, Json) ?? [];
        return Result<TestResultDto>.Success(ToResultDto(attempt, breakdown, answers, attempt.XpSettlement));
    }

    public async Task<Result<TestingPageDto<TestHistoryItemDto>>> HistoryAsync(Guid userId, TestHistoryQuery filter, CancellationToken ct)
    {
        var page = Math.Max(1, filter.Page); var size = Math.Clamp(filter.PageSize, 1, 50);
        var query = db.TestAttempts.AsNoTracking().Where(x => x.UserId == userId);
        var total = await query.CountAsync(ct);
        var rows = await query.OrderByDescending(x => x.StartedAtUtc).Skip((page - 1) * size).Take(size).ToListAsync(ct);
        return Result<TestingPageDto<TestHistoryItemDto>>.Success(new(rows.Select(x => new TestHistoryItemDto(
            x.Id, (int)x.Mode, (int)x.Status, x.StartedAtUtc, x.SubmittedAtUtc, x.QuestionCount, x.CorrectCount, x.Percentage)).ToList(), page, size, total));
    }

    private async Task<Result<TestAttemptDto>> StartAsync(Guid userId, TestMode mode, int count, Guid? subjectId,
        Guid? clusterId, IReadOnlyCollection<Guid>? clusterSubjects, bool injectRedList, SupportedLanguage language,
        int durationMinutes, CancellationToken ct, string? monthlyWindowKey = null)
    {
        var picked = await picker.PickAsync(new(userId, mode, count, subjectId, clusterSubjects, injectRedList), ct);
        if (picked.IsFailure) return Result<TestAttemptDto>.Failure(picked.Error!);
        var now = clock.UtcNow;
        var attempt = new TestAttempt(Guid.NewGuid(), userId, mode, subjectId, clusterId, monthlyWindowKey,
            count, now, now.AddMinutes(durationMinutes));
        db.TestAttempts.Add(attempt);
        var questions = picked.Value!.ToList(); randomizer.Shuffle(questions);
        for (var index = 0; index < questions.Count; index++)
        {
            var question = questions[index];
            db.TestAttemptQuestions.Add(new(Guid.NewGuid(), attempt.Id, question.Id, index + 1, question.SubjectId,
                question.TopicId, question.Type, question.Difficulty, JsonSerializer.Serialize(Snapshot(question, language), Json)));
        }
        try
        {
            await db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException exception) when (monthlyWindowKey is not null
            && StudentTestingDatabaseNames.IsMonthlyWindowViolation(exception))
        {
            db.ChangeTracker.Clear();
            return Result<TestAttemptDto>.Failure(StudentTestingErrors.MonthlyExamAlreadyStarted);
        }
        return await GetAttemptAsync(userId, attempt.Id, ct);
    }

    private TestQuestionSnapshot Snapshot(Question question, SupportedLanguage language)
    {
        var translation = question.Translations.FirstOrDefault(x => x.Language == language)
            ?? question.Translations.FirstOrDefault(x => x.Language == SupportedLanguage.Tajik)
            ?? question.Translations.First();
        var optionSnapshots = question.AnswerOptions.OrderBy(x => x.DisplayOrder).Select(option => new TestOptionSnapshot(
            option.Id, option.DisplayOrder, option.IsCorrect,
            AssessmentText.TextFor(option.Translations, language, x => x.Text),
            AssessmentText.TextFor(option.Translations, language, x => x.MatchPairText))).ToList();
        var matchingDisplayOrder = question.Type == QuestionType.Matching
            ? optionSnapshots.Where(x => !string.IsNullOrWhiteSpace(x.MatchPairText)).Select(x => x.Id).ToList()
            : null;
        if (matchingDisplayOrder is not null) randomizer.Shuffle(matchingDisplayOrder);
        return new(TestQuestionSnapshot.CurrentVersion, question.Id, question.SubjectId, question.TopicId,
            (int)question.Type, (int)question.Difficulty, translation.Content, translation.Explanation, question.ImageUrl,
            language, optionSnapshots, matchingDisplayOrder);
    }

    private static Question SnapshotQuestion(TestQuestionSnapshot snapshot)
    {
        var question = new Question(snapshot.QuestionId, snapshot.SubjectId, snapshot.TopicId, null,
            (QuestionType)snapshot.Type, (DifficultyLevel)snapshot.Difficulty, snapshot.ImageUrl, DateTimeOffset.UnixEpoch);
        var options = snapshot.Options.Select(value =>
        {
            var option = new AnswerOption(value.Id, snapshot.QuestionId, value.DisplayOrder, value.IsCorrect);
            option.ReplaceTranslations([new AnswerOptionTranslation(value.Id, snapshot.Language, value.Text, value.MatchPairText)]);
            return option;
        }).ToList();
        question.ReplaceContent([new QuestionTranslation(snapshot.QuestionId, snapshot.Language, snapshot.Content, snapshot.Explanation)], options);
        return question;
    }

    private static TestAttemptDto ToAttemptDto(TestAttempt attempt) => new(attempt.Id, (int)attempt.Mode, (int)attempt.Status,
        attempt.SubjectId, attempt.ClusterId, attempt.StartedAtUtc, attempt.ExpiresAtUtc, attempt.SubmittedAtUtc,
        attempt.QuestionCount, attempt.Questions.OrderBy(x => x.DisplayOrder).Select(x =>
        {
            var snapshot = DeserializeQuestion(x.QuestionSnapshotJson);
            var options = snapshot.Options.OrderBy(option => option.DisplayOrder).Select(option => new TestAnswerOptionDto(option.Id, option.Text)).ToList();
            var matchingOptions = MatchingOptions(snapshot);
            return new TestQuestionDto(snapshot.QuestionId, x.DisplayOrder, snapshot.SubjectId, snapshot.TopicId,
                snapshot.Type, snapshot.Difficulty, snapshot.Content, snapshot.ImageUrl, options, matchingOptions);
        }).ToList());

    private static IReadOnlyList<string> MatchingOptions(TestQuestionSnapshot snapshot)
    {
        if (snapshot.Type != (int)QuestionType.Matching) return [];
        var values = snapshot.Options.Where(x => !string.IsNullOrWhiteSpace(x.MatchPairText))
            .ToDictionary(x => x.Id, x => x.MatchPairText!);
        var order = snapshot.MatchingDisplayOrder is { Count: > 0 }
            ? snapshot.MatchingDisplayOrder
            : values.Keys.OrderBy(x => x).ToList();
        return order.Where(values.ContainsKey).Select(x => values[x]).ToList();
    }

    private static TestResultDto ToResultDto(TestAttempt attempt, IReadOnlyList<TopicBreakdownDto> breakdown,
        IReadOnlyList<StoredAnswerResult> answers, TestXpSettlement? settlement)
    {
        var subjects = answers.GroupBy(x => x.SubjectId).Select(group => new SubjectBreakdownDto(
            group.Key, group.Count(), group.Count(x => x.IsCorrect), group.Count(x => !x.IsCorrect),
            Percentage(group.Count(x => x.IsCorrect), group.Count()))).ToList();
        var weakTopics = answers.GroupBy(x => new { x.SubjectId, x.TopicId })
            .Select(group => new WeakTopicDto(group.Key.SubjectId, group.Key.TopicId, group.Count(),
                group.Count(x => x.IsCorrect), Percentage(group.Count(x => x.IsCorrect), group.Count())))
            .Where(x => x.Percentage < 70m).OrderBy(x => x.Percentage).ThenByDescending(x => x.Total).ToList();
        return new(attempt.Id, (int)attempt.Mode, (int)attempt.Status, attempt.QuestionCount, attempt.CorrectCount,
            attempt.WrongCount, attempt.Score, attempt.Percentage, attempt.SubmittedAtUtc!.Value, breakdown, subjects,
            weakTopics, answers.Select(x => new TestAnswerResultDto(x.QuestionId, x.SubjectId, x.IsAnswered,
                x.IsCorrect, x.Content, x.UserAnswer, x.CorrectAnswer, x.Explanation, x.TopicId, x.Difficulty,
                x.CorrectPairsCount, x.TotalPairsCount)).ToList(),
            settlement?.EasyCorrectCount ?? 0, settlement?.MediumCorrectCount ?? 0, settlement?.HardCorrectCount ?? 0,
            ToXp(settlement?.AnswerXpUnits ?? 0), ToXp(settlement?.CompletionBonusXpUnits ?? 0),
            ToXp(settlement?.TotalXpUnits ?? 0), settlement is { TotalXpUnits: > 0 });
    }

    private static decimal ToXp(int units) => units / (decimal)TestXpRewardOptions.UnitsPerXp;

    private static decimal Percentage(int correct, int total) => total == 0 ? 0 : decimal.Round(correct * 100m / total, 2);

    private static string? DisplayAnswer(TestQuestionSnapshot snapshot, SubmitAnswerDto? answer, AnswerEvaluationResult evaluation)
    {
        if (answer?.SelectedOptionId is Guid id) return snapshot.Options.FirstOrDefault(x => x.Id == id)?.Text;
        if (!string.IsNullOrWhiteSpace(answer?.TextResponse)) return answer.TextResponse;
        return answer?.MatchingPairs is { Count: > 0 } pairs ? string.Join(" | ", pairs.Select(x => $"{x.Key}:{x.Value}")) : evaluation.SubmittedAnswerText;
    }

    private static string? CorrectAnswer(TestQuestionSnapshot snapshot) => snapshot.Type == (int)QuestionType.Matching
        ? string.Join(" | ", snapshot.Options.Select(x => $"{x.Text}:{x.MatchPairText}"))
        : snapshot.Options.FirstOrDefault(x => x.IsCorrect)?.Text;
    private static TestQuestionSnapshot DeserializeQuestion(string json) =>
        JsonSerializer.Deserialize<TestQuestionSnapshot>(json, Json) ?? throw new InvalidOperationException("Invalid question snapshot.");
    private IQueryable<TestAttempt> AttemptQuery() => db.TestAttempts.AsNoTracking().Include(x => x.Questions).AsSplitQuery();
    private async ValueTask<IDbContextTransaction?> BeginTransactionAsync(CancellationToken ct) => db.Database.IsRelational()
        ? await db.Database.BeginTransactionAsync(IsolationLevel.ReadCommitted, ct) : null;
}
