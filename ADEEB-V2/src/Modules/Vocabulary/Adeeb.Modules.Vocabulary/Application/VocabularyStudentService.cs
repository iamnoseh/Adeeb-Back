using System.Globalization;
using System.Security.Claims;
using System.Text.Json;
using Adeeb.Application.Abstractions.Time;
using Adeeb.Modules.Students.Contracts;
using Adeeb.Modules.Vocabulary.Contracts;
using Adeeb.Modules.Vocabulary.Domain;
using Adeeb.Modules.Vocabulary.Infrastructure.Persistence;
using Adeeb.SharedKernel.Errors;
using Adeeb.SharedKernel.Results;
using Microsoft.EntityFrameworkCore;

namespace Adeeb.Modules.Vocabulary.Application;

public sealed class VocabularyStudentService(VocabularyDbContext db, IStudentLookup students, IDateTimeProvider clock)
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    public async Task<Result<IReadOnlyList<LearningLanguageDto>>> GetLanguagesAsync(ClaimsPrincipal principal, CancellationToken ct)
    {
        var access = await StudentAsync(principal, ct); if (access.IsFailure) return Result<IReadOnlyList<LearningLanguageDto>>.Failure(access.Error!);
        var items = await db.Languages.AsNoTracking().Where(x => x.IsActive).OrderBy(x => x.DisplayOrder).ThenBy(x => x.Code).ToListAsync(ct);
        return Result<IReadOnlyList<LearningLanguageDto>>.Success(items.Select(x => new LearningLanguageDto(x.Id, x.Code, Russian ? x.NameRu : x.NameTg, x.NameTg, x.NameRu, x.DisplayOrder, x.IsActive)).ToList());
    }

    public async Task<Result<StudentVocabularyCourseDto>> GetCourseAsync(ClaimsPrincipal principal, CancellationToken ct)
    {
        var access = await StudentAsync(principal, ct); if (access.IsFailure) return Result<StudentVocabularyCourseDto>.Failure(access.Error!);
        var course = await db.Courses.AsNoTracking().SingleOrDefaultAsync(x => x.StudentId == access.Value!.StudentId, ct); if (course is null) return Result<StudentVocabularyCourseDto>.Failure(VocabularyErrors.CourseRequired);
        return Result<StudentVocabularyCourseDto>.Success(await ToCourseAsync(course, ct));
    }

    public async Task<Result<StudentVocabularyCourseDto>> SetCourseAsync(ClaimsPrincipal principal, StudentVocabularyCourseRequest request, CancellationToken ct)
    {
        var access = await StudentAsync(principal, ct); if (access.IsFailure) return Result<StudentVocabularyCourseDto>.Failure(access.Error!);
        var student = access.Value!;
        if (!Enum.IsDefined(typeof(VocabularyLevel), request.Level)) return Invalid<StudentVocabularyCourseDto>("level");
        var language = await db.Languages.SingleOrDefaultAsync(x => x.Id == request.LanguageId && x.IsActive, ct); if (language is null) return Result<StudentVocabularyCourseDto>.Failure(VocabularyErrors.LanguageNotFound);
        var course = await db.Courses.SingleOrDefaultAsync(x => x.StudentId == student.StudentId, ct);
        if (course is null) { course = new(student.StudentId, request.LanguageId, (VocabularyLevel)request.Level, clock.UtcNow); db.Courses.Add(course); } else course.Change(request.LanguageId, (VocabularyLevel)request.Level, clock.UtcNow);
        await db.SaveChangesAsync(ct); return Result<StudentVocabularyCourseDto>.Success(ToCourse(course, language));
    }

    public async Task<Result<DailyWordDto>> GetTodayAsync(ClaimsPrincipal principal, CancellationToken ct)
    {
        var context = await ContextAsync(principal, ct); if (context.IsFailure) return Result<DailyWordDto>.Failure(context.Error!);
        return await ResolveDailyWordAsync(context.Value!.Course, context.Value.LocalDate, ct);
    }

    public async Task<Result<StudentVocabularyDashboardDto>> GetDashboardAsync(ClaimsPrincipal principal, CancellationToken ct)
    {
        var context = await ContextAsync(principal, ct); if (context.IsFailure) return Result<StudentVocabularyDashboardDto>.Failure(context.Error!);
        var daily = await ResolveDailyWordAsync(context.Value!.Course, context.Value.LocalDate, ct); if (daily.IsFailure) return Result<StudentVocabularyDashboardDto>.Failure(daily.Error!);
        var studentId = context.Value.Student.StudentId; var languageId = context.Value.Course.LanguageId;
        var progress = db.WordProgress.AsNoTracking().Where(x => x.StudentId == studentId && db.Words.Any(w => w.Id == x.WordId && w.LanguageId == languageId));
        var mastered = await progress.CountAsync(x => x.MasteryLevel >= 5, ct); var due = await progress.CountAsync(x => x.NextReviewDate <= context.Value.LocalDate, ct); var total = await progress.CountAsync(ct);
        var completed = await db.Sessions.CountAsync(x => x.StudentId == studentId && x.LanguageId == languageId && x.Status == VocabularySessionStatus.Completed, ct);
        return Result<StudentVocabularyDashboardDto>.Success(new(await ToCourseAsync(context.Value.Course, ct), daily.Value!, mastered, due, completed, total));
    }

    public async Task<Result<VocabularySessionDto>> StartSessionAsync(ClaimsPrincipal principal, StartVocabularySessionRequest request, CancellationToken ct)
    {
        var context = await ContextAsync(principal, ct); if (context.IsFailure) return Result<VocabularySessionDto>.Failure(context.Error!);
        var studentContext = context.Value!;
        if (!Enum.IsDefined(typeof(VocabularySessionMode), request.Mode)) return Invalid<VocabularySessionDto>("mode");
        var mode = (VocabularySessionMode)request.Mode; var level = request.Level is null ? studentContext.Course.Level : Enum.IsDefined(typeof(VocabularyLevel), request.Level.Value) ? (VocabularyLevel)request.Level.Value : (VocabularyLevel?)null;
        if (level is null) return Invalid<VocabularySessionDto>("level");
        var count = mode switch { VocabularySessionMode.DailyPractice => 10, VocabularySessionMode.Test => request.QuestionCount ?? 20, VocabularySessionMode.MistakeReview => Math.Clamp(request.QuestionCount ?? 10, 1, 30), _ => Math.Clamp(request.QuestionCount ?? 10, 1, 30) };
        if (mode == VocabularySessionMode.Test && count is not (10 or 20 or 30)) return Invalid<VocabularySessionDto>("questionCount");
        if (request.TopicId is not null && !await db.Topics.AnyAsync(x => x.Id == request.TopicId && x.LanguageId == studentContext.Course.LanguageId && x.Status == VocabularyContentStatus.Published, ct)) return Result<VocabularySessionDto>.Failure(VocabularyErrors.TopicNotFound);
        var existing = await db.Sessions.AsNoTracking().Where(x => x.StudentId == studentContext.Student.StudentId && x.LanguageId == studentContext.Course.LanguageId && x.Mode == mode && x.Status == VocabularySessionStatus.InProgress).OrderByDescending(x => x.StartedAtUtc).FirstOrDefaultAsync(ct);
        if (existing is not null) return await GetSessionCoreAsync(existing, ct);

        if (mode == VocabularySessionMode.DailyPractice)
        {
            var daily = await ResolveDailyWordAsync(studentContext.Course, studentContext.LocalDate, ct);
            if (daily.IsFailure) return Result<VocabularySessionDto>.Failure(daily.Error!);
        }

        var selected = await SelectQuestionsAsync(studentContext.Student.StudentId, studentContext.Course.LanguageId, level.Value, request.TopicId, mode, count, studentContext.LocalDate, ct);
        if ((mode == VocabularySessionMode.DailyPractice && selected.Count < 10) || selected.Count == 0 || (mode == VocabularySessionMode.Test && selected.Count < count)) return Result<VocabularySessionDto>.Failure(VocabularyErrors.NotEnoughQuestions);
        var session = new VocabularySession(Guid.NewGuid(), studentContext.Student.StudentId, studentContext.Course.LanguageId, mode, level.Value, request.TopicId, studentContext.LocalDate, selected.Count, clock.UtcNow); db.Sessions.Add(session);
        var order = 0; foreach (var question in selected) db.SessionQuestions.Add(Snapshot(session.Id, question, order++));
        try { await db.SaveChangesAsync(ct); }
        catch (DbUpdateException)
        {
            db.ChangeTracker.Clear();
            var raced = await db.Sessions.AsNoTracking().Where(x => x.StudentId == studentContext.Student.StudentId && x.LanguageId == studentContext.Course.LanguageId && x.Mode == mode && x.Status == VocabularySessionStatus.InProgress).OrderByDescending(x => x.StartedAtUtc).FirstAsync(ct);
            return await GetSessionCoreAsync(raced, ct);
        }
        return await GetSessionCoreAsync(session, ct);
    }

    public async Task<Result<VocabularySessionDto>> GetSessionAsync(ClaimsPrincipal principal, Guid sessionId, CancellationToken ct)
    {
        var access = await StudentAsync(principal, ct); if (access.IsFailure) return Result<VocabularySessionDto>.Failure(access.Error!);
        var session = await db.Sessions.AsNoTracking().SingleOrDefaultAsync(x => x.Id == sessionId && x.StudentId == access.Value!.StudentId, ct); return session is null ? Result<VocabularySessionDto>.Failure(VocabularyErrors.SessionNotFound) : await GetSessionCoreAsync(session, ct);
    }

    public async Task<Result<VocabularyAnswerResponse>> SubmitAnswerAsync(ClaimsPrincipal principal, Guid sessionId, SubmitVocabularyAnswerRequest request, CancellationToken ct)
    {
        var context = await ContextAsync(principal, ct); if (context.IsFailure) return Result<VocabularyAnswerResponse>.Failure(context.Error!);
        var session = await db.Sessions.SingleOrDefaultAsync(x => x.Id == sessionId && x.StudentId == context.Value!.Student.StudentId, ct); if (session is null) return Result<VocabularyAnswerResponse>.Failure(VocabularyErrors.SessionNotFound); if (session.Status != VocabularySessionStatus.InProgress) return Result<VocabularyAnswerResponse>.Failure(VocabularyErrors.SessionCompleted);
        var question = await db.SessionQuestions.AsNoTracking().SingleOrDefaultAsync(x => x.SessionId == sessionId && x.QuestionId == request.QuestionId, ct); if (question is null) return Result<VocabularyAnswerResponse>.Failure(VocabularyErrors.QuestionNotFound);
        var existing = await db.SessionAnswers.AsNoTracking().SingleOrDefaultAsync(x => x.SessionId == sessionId && x.QuestionId == request.QuestionId, ct);
        if (existing is not null) return Result<VocabularyAnswerResponse>.Success(new((await GetSessionCoreAsync(session, ct)).Value!, session.Mode == VocabularySessionMode.Test ? null : Feedback(question, existing.IsCorrect)));
        var answer = Evaluate(question, request); if (answer is null) return Invalid<VocabularyAnswerResponse>("answer");
        db.SessionAnswers.Add(new VocabularySessionAnswer(sessionId, question.QuestionId, JsonSerializer.Serialize(request, Json), answer.Value, clock.UtcNow));
        if (session.Mode != VocabularySessionMode.Test) await ApplyProgressAsync(session.StudentId, question.WordId, answer.Value, session.LocalDate, ct);
        try { await db.SaveChangesAsync(ct); }
        catch (DbUpdateException)
        {
            db.ChangeTracker.Clear();
            var raced = await db.SessionAnswers.AsNoTracking().SingleAsync(x => x.SessionId == sessionId && x.QuestionId == question.QuestionId, ct);
            var racedSession = await db.Sessions.AsNoTracking().SingleAsync(x => x.Id == sessionId, ct);
            return Result<VocabularyAnswerResponse>.Success(new((await GetSessionCoreAsync(racedSession, ct)).Value!, racedSession.Mode == VocabularySessionMode.Test ? null : Feedback(question, raced.IsCorrect)));
        }
        return Result<VocabularyAnswerResponse>.Success(new((await GetSessionCoreAsync(session, ct)).Value!, session.Mode == VocabularySessionMode.Test ? null : Feedback(question, answer.Value)));
    }

    public async Task<Result<VocabularySessionResultDto>> CompleteSessionAsync(ClaimsPrincipal principal, Guid sessionId, CancellationToken ct)
    {
        var access = await StudentAsync(principal, ct); if (access.IsFailure) return Result<VocabularySessionResultDto>.Failure(access.Error!);
        var session = await db.Sessions.SingleOrDefaultAsync(x => x.Id == sessionId && x.StudentId == access.Value!.StudentId, ct); if (session is null) return Result<VocabularySessionResultDto>.Failure(VocabularyErrors.SessionNotFound);
        var questions = await db.SessionQuestions.AsNoTracking().Where(x => x.SessionId == sessionId).OrderBy(x => x.Order).ToListAsync(ct); var answers = await db.SessionAnswers.AsNoTracking().Where(x => x.SessionId == sessionId).ToListAsync(ct);
        if (answers.Count != questions.Count) return Result<VocabularySessionResultDto>.ValidationFailure(new Dictionary<string, IReadOnlyList<Error>> { ["answers"] = [Error.Validation("vocabulary.answers.incomplete", "Vocabulary.AnswersIncomplete")] });
        if (session.Status == VocabularySessionStatus.InProgress)
        {
            if (session.Mode == VocabularySessionMode.Test)
            {
                foreach (var question in questions)
                {
                    var answer = answers.Single(x => x.QuestionId == question.QuestionId);
                    await ApplyProgressAsync(session.StudentId, question.WordId, answer.IsCorrect, session.LocalDate, ct);
                }
            }
            session.Complete(answers.Count(x => x.IsCorrect), clock.UtcNow); await db.SaveChangesAsync(ct);
        }
        return Result<VocabularySessionResultDto>.Success(ToResult(session, questions, answers));
    }

    public async Task<Result<VocabularyPage<VocabularyHistoryItemDto>>> GetHistoryAsync(ClaimsPrincipal principal, int page, int pageSize, CancellationToken ct)
    {
        var access = await StudentAsync(principal, ct); if (access.IsFailure) return Result<VocabularyPage<VocabularyHistoryItemDto>>.Failure(access.Error!); var p = Math.Max(1, page); var size = Math.Clamp(pageSize, 1, 50);
        var q = db.Sessions.AsNoTracking().Where(x => x.StudentId == access.Value!.StudentId && x.Status == VocabularySessionStatus.Completed); var total = await q.CountAsync(ct);
        var items = await q.OrderByDescending(x => x.CompletedAtUtc).Skip((p - 1) * size).Take(size).Select(x => new VocabularyHistoryItemDto(x.Id, (int)x.Mode, x.QuestionCount, x.CorrectCount, x.QuestionCount == 0 ? 0 : Math.Round((decimal)x.CorrectCount * 100 / x.QuestionCount, 2), x.CompletedAtUtc!.Value)).ToListAsync(ct);
        return Result<VocabularyPage<VocabularyHistoryItemDto>>.Success(new(items, p, size, total));
    }

    public async Task<Result<VocabularyPage<VocabularyMistakeDto>>> GetMistakesAsync(ClaimsPrincipal principal, int page, int pageSize, CancellationToken ct)
    {
        var context = await ContextAsync(principal, ct); if (context.IsFailure) return Result<VocabularyPage<VocabularyMistakeDto>>.Failure(context.Error!); var p = Math.Max(1, page); var size = Math.Clamp(pageSize, 1, 50);
        var q = from progress in db.WordProgress.AsNoTracking() join word in db.Words.AsNoTracking() on progress.WordId equals word.Id where progress.StudentId == context.Value!.Student.StudentId && word.LanguageId == context.Value.Course.LanguageId && progress.WrongCount > 0 select new { progress, word };
        var total = await q.CountAsync(ct); var rows = await q.OrderBy(x => x.progress.NextReviewDate).ThenByDescending(x => x.progress.WrongCount).Skip((p - 1) * size).Take(size).ToListAsync(ct);
        var items = rows.Select(x => new VocabularyMistakeDto(x.word.Id, x.word.TargetText, Russian ? x.word.TranslationRu : x.word.TranslationTg, x.progress.WrongCount, x.progress.MasteryLevel, x.progress.NextReviewDate)).ToList();
        return Result<VocabularyPage<VocabularyMistakeDto>>.Success(new(items, p, size, total));
    }

    private async Task<Result<DailyWordDto>> ResolveDailyWordAsync(StudentVocabularyCourse course, DateOnly date, CancellationToken ct)
    {
        var current = await db.DailyWords.AsNoTracking().SingleOrDefaultAsync(x => x.LanguageId == course.LanguageId && x.LocalDate == date, ct);
        if (current is null)
        {
            var candidate = await db.Words.AsNoTracking().Where(x => x.LanguageId == course.LanguageId && x.Level == course.Level && x.Status == VocabularyContentStatus.Published && db.Topics.Any(t => t.Id == x.TopicId && t.Status == VocabularyContentStatus.Published))
                .OrderBy(x => db.DailyWords.Count(d => d.WordId == x.Id)).ThenBy(x => x.Id).FirstOrDefaultAsync(ct);
            if (candidate is null) return Result<DailyWordDto>.Failure(VocabularyErrors.NotEnoughQuestions);
            db.DailyWords.Add(new VocabularyDailyWord(course.LanguageId, date, candidate.Id, true, clock.UtcNow));
            try { await db.SaveChangesAsync(ct); } catch (DbUpdateException) { db.ChangeTracker.Clear(); }
            current = await db.DailyWords.AsNoTracking().SingleAsync(x => x.LanguageId == course.LanguageId && x.LocalDate == date, ct);
        }
        var word = await db.Words.AsNoTracking().SingleOrDefaultAsync(x => x.Id == current.WordId && x.Status == VocabularyContentStatus.Published && db.Topics.Any(t => t.Id == x.TopicId && t.Status == VocabularyContentStatus.Published), ct);
        return word is null ? Result<DailyWordDto>.Failure(VocabularyErrors.NotEnoughQuestions) : Result<DailyWordDto>.Success(new(current.LanguageId, current.LocalDate, current.IsAutomatic, VocabularyAdminService.ToWord(word, [], new Dictionary<Guid, VocabularyWord>())));
    }

    private async Task<List<VocabularyQuestion>> SelectQuestionsAsync(Guid studentId, Guid languageId, VocabularyLevel level, Guid? topicId, VocabularySessionMode mode, int count, DateOnly localDate, CancellationToken ct)
    {
        var query = db.Questions.AsNoTracking().Include(x => x.Options).Where(q => q.Status == VocabularyContentStatus.Published && db.Words.Any(w => w.Id == q.WordId && w.LanguageId == languageId && w.Level == level && w.Status == VocabularyContentStatus.Published && db.Topics.Any(t => t.Id == w.TopicId && t.Status == VocabularyContentStatus.Published) && (topicId == null || w.TopicId == topicId)));
        var all = await query.ToListAsync(ct); if (mode == VocabularySessionMode.MistakeReview) { var due = await db.WordProgress.Where(x => x.StudentId == studentId && x.WrongCount > 0 && x.NextReviewDate <= localDate).Select(x => x.WordId).ToListAsync(ct); return all.Where(x => due.Contains(x.WordId)).OrderBy(x => x.Id).Take(count).ToList(); }
        var progress = await db.WordProgress.AsNoTracking().Where(x => x.StudentId == studentId).ToDictionaryAsync(x => x.WordId, ct); var dueIds = progress.Values.Where(x => x.NextReviewDate <= localDate).OrderBy(x => x.NextReviewDate).Select(x => x.WordId).ToList();
        var daily = await db.DailyWords.AsNoTracking().Where(x => x.LanguageId == languageId && x.LocalDate == localDate).Select(x => (Guid?)x.WordId).SingleOrDefaultAsync(ct);
        var preferred = dueIds.Concat(daily is null ? [] : [daily.Value]).Concat(all.Where(x => !progress.ContainsKey(x.WordId)).Select(x => x.WordId)).Distinct().ToList(); var selected = new List<VocabularyQuestion>();
        foreach (var wordId in preferred) { var candidate = all.Where(x => x.WordId == wordId && !selected.Contains(x)).OrderBy(x => x.Type).FirstOrDefault(); if (candidate is not null) selected.Add(candidate); if (selected.Count == count) return selected; }
        selected.AddRange(all.Where(x => !selected.Contains(x)).OrderBy(x => x.Id).Take(count - selected.Count)); return selected;
    }

    private static VocabularySessionQuestion Snapshot(Guid sessionId, VocabularyQuestion q, int order)
    {
        var shuffled = q.Options.OrderBy(_ => Guid.NewGuid()).Select((x, i) => new SnapshotOption(x.Id, Value(x), i)).ToList(); var correct = new CorrectSnapshot(q.Options.SingleOrDefault(x => x.IsCorrect)?.Id, q.CorrectTokenIndex, q.Options.Where(x => x.CorrectOrder is not null).OrderBy(x => x.CorrectOrder).Select(x => x.Id).ToList());
        return new(sessionId, q.Id, q.WordId, order, q.Type, Prompt(q), q.CorrectTokenIndex, JsonSerializer.Serialize(shuffled, Json), JsonSerializer.Serialize(correct, Json));
    }
    private async Task<Result<VocabularySessionDto>> GetSessionCoreAsync(VocabularySession session, CancellationToken ct)
    {
        var questions = await db.SessionQuestions.AsNoTracking().Where(x => x.SessionId == session.Id).OrderBy(x => x.Order).ToListAsync(ct); var answers = await db.SessionAnswers.AsNoTracking().Where(x => x.SessionId == session.Id).ToListAsync(ct); var answered = answers.Select(x => x.QuestionId).ToHashSet();
        var visibleCorrectCount = session.Mode == VocabularySessionMode.Test && session.Status == VocabularySessionStatus.InProgress ? 0 : answers.Count(x => x.IsCorrect);
        var dto = new VocabularySessionDto(session.Id, (int)session.Mode, (int)session.Status, session.LanguageId, (int)session.Level, session.TopicId, session.LocalDate, session.QuestionCount, answers.Count, visibleCorrectCount, session.StartedAtUtc, session.CompletedAtUtc,
            questions.Select(x => new StudentVocabularyQuestionDto(x.QuestionId, x.Order, (int)x.Type, x.Prompt, JsonSerializer.Deserialize<List<SnapshotOption>>(x.OptionsJson, Json)!.Select(o => new StudentVocabularyOptionDto(o.Id, o.Value, o.DisplayOrder)).ToList(), answered.Contains(x.QuestionId))).ToList());
        return Result<VocabularySessionDto>.Success(dto);
    }
    private static bool? Evaluate(VocabularySessionQuestion q, SubmitVocabularyAnswerRequest request)
    {
        var correct = JsonSerializer.Deserialize<CorrectSnapshot>(q.CorrectAnswerJson, Json)!;
        return q.Type switch
        {
            VocabularyQuestionType.WordOrder when request.OrderedOptionIds is { Count: > 0 } => request.OrderedOptionIds.SequenceEqual(correct.CorrectOrder),
            VocabularyQuestionType.OddWordReplacement when request.SelectedOptionId is not null && request.SelectedTokenIndex is not null => request.SelectedOptionId == correct.CorrectOptionId && request.SelectedTokenIndex == correct.CorrectTokenIndex,
            not VocabularyQuestionType.WordOrder and not VocabularyQuestionType.OddWordReplacement when request.SelectedOptionId is not null => request.SelectedOptionId == correct.CorrectOptionId,
            _ => null
        };
    }
    private static VocabularyAnswerFeedbackDto Feedback(VocabularySessionQuestion q, bool isCorrect) { var c = JsonSerializer.Deserialize<CorrectSnapshot>(q.CorrectAnswerJson, Json)!; return new(q.QuestionId, isCorrect, c.CorrectOptionId, c.CorrectTokenIndex, c.CorrectOrder); }
    private static VocabularySessionResultDto ToResult(VocabularySession s, IReadOnlyList<VocabularySessionQuestion> questions, IReadOnlyList<VocabularySessionAnswer> answers) => new(s.Id, (int)s.Mode, s.QuestionCount, answers.Count(x => x.IsCorrect), answers.Count(x => !x.IsCorrect), s.QuestionCount == 0 ? 0 : Math.Round((decimal)answers.Count(x => x.IsCorrect) * 100 / s.QuestionCount, 2), s.CompletedAtUtc!.Value, questions.Select(q => Feedback(q, answers.Single(a => a.QuestionId == q.QuestionId).IsCorrect)).ToList());
    private async Task ApplyProgressAsync(Guid studentId, Guid wordId, bool correct, DateOnly localDate, CancellationToken ct)
    {
        var progress = await db.WordProgress.SingleOrDefaultAsync(x => x.StudentId == studentId && x.WordId == wordId, ct);
        if (progress is null) { progress = new(studentId, wordId); db.WordProgress.Add(progress); }
        progress.Apply(correct, localDate, clock.UtcNow);
    }
    private async Task<Result<StudentReference>> StudentAsync(ClaimsPrincipal principal, CancellationToken ct)
    {
        var identityId = UserId(principal); if (identityId is null) return Result<StudentReference>.Failure(VocabularyErrors.StudentRequired); var student = await students.FindByIdentityUserIdAsync(identityId.Value, ct); if (student is null) return Result<StudentReference>.Failure(VocabularyErrors.StudentRequired); return !string.Equals(student.Status, "Active", StringComparison.OrdinalIgnoreCase) ? Result<StudentReference>.Failure(VocabularyErrors.StudentUnavailable) : Result<StudentReference>.Success(student);
    }
    private async Task<Result<StudentContext>> ContextAsync(ClaimsPrincipal principal, CancellationToken ct)
    {
        var student = await StudentAsync(principal, ct); if (student.IsFailure) return Result<StudentContext>.Failure(student.Error!); var reference = student.Value!; var course = await db.Courses.SingleOrDefaultAsync(x => x.StudentId == reference.StudentId, ct); if (course is null) return Result<StudentContext>.Failure(VocabularyErrors.CourseRequired); if (!await db.Languages.AnyAsync(x => x.Id == course.LanguageId && x.IsActive, ct)) return Result<StudentContext>.Failure(VocabularyErrors.LanguageNotFound); return Result<StudentContext>.Success(new(reference, course, LocalDate(reference.TimeZoneId)));
    }
    private DateOnly LocalDate(string? timeZoneId) { try { var zone = TimeZoneInfo.FindSystemTimeZoneById(string.IsNullOrWhiteSpace(timeZoneId) ? "Asia/Dushanbe" : timeZoneId); return DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(clock.UtcNow, zone).DateTime); } catch { return DateOnly.FromDateTime(clock.DushanbeNow.DateTime); } }
    private async Task<StudentVocabularyCourseDto> ToCourseAsync(StudentVocabularyCourse course, CancellationToken ct) => ToCourse(course, await db.Languages.AsNoTracking().SingleAsync(x => x.Id == course.LanguageId, ct));
    private static StudentVocabularyCourseDto ToCourse(StudentVocabularyCourse c, LearningLanguage l) => new(c.LanguageId, Russian ? l.NameRu : l.NameTg, (int)c.Level, c.UpdatedAtUtc);
    private static Guid? UserId(ClaimsPrincipal p) => Guid.TryParse(p.FindFirstValue(ClaimTypes.NameIdentifier) ?? p.FindFirstValue("sub"), out var id) ? id : null;
    private static bool Russian => CultureInfo.CurrentUICulture.Name.StartsWith("ru", StringComparison.OrdinalIgnoreCase);
    private static string Prompt(VocabularyQuestion q) => q.Type == VocabularyQuestionType.WordOrder ? Russian ? q.PromptRu : q.PromptTg : q.PromptTarget;
    private static string Value(VocabularyQuestionOption o) => o.ValueTarget == o.ValueTg && o.ValueTarget == o.ValueRu ? o.ValueTarget : Russian ? o.ValueRu : o.ValueTg;
    private static Result<T> Invalid<T>(string field) => Result<T>.ValidationFailure(new Dictionary<string, IReadOnlyList<Error>> { [field] = [Error.Validation($"vocabulary.{field}.invalid", "Vocabulary.Invalid")] });
    private sealed record StudentContext(StudentReference Student, StudentVocabularyCourse Course, DateOnly LocalDate);
    private sealed record SnapshotOption(Guid Id, string Value, int DisplayOrder);
    private sealed record CorrectSnapshot(Guid? CorrectOptionId, int? CorrectTokenIndex, IReadOnlyList<Guid> CorrectOrder);
}
