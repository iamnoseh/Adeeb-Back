using Adeeb.Application.Abstractions.Localization;
using Adeeb.Application.Abstractions.Time;
using Adeeb.Modules.AcademicCatalog.Contracts;
using Adeeb.Modules.QuestionBank.Contracts;
using Adeeb.Modules.QuestionBank.Domain;
using Adeeb.Modules.QuestionBank.Infrastructure.Persistence;
using Adeeb.SharedKernel.Results;
using Microsoft.EntityFrameworkCore;
using QuestionPagedResponse = Adeeb.Modules.QuestionBank.Contracts.PagedResponse<Adeeb.Modules.QuestionBank.Contracts.QuestionResponse>;

namespace Adeeb.Modules.QuestionBank.Application;

public sealed class QuestionBankService(QuestionBankDbContext db, IAcademicCatalogLookup catalogLookup, IDateTimeProvider clock)
{
    public async Task<Result<QuestionPagedResponse>> GetQuestionsAsync(QuestionListQuery query, SupportedLanguage language, CancellationToken ct)
    {
        var page = Math.Max(1, query.Page);
        var pageSize = Math.Clamp(query.PageSize, 1, 100);
        var questions = FullQuery().AsNoTracking();
        if (query.SubjectId.HasValue)
        {
            questions = questions.Where(x => x.SubjectId == query.SubjectId.Value);
        }
        if (query.TopicId.HasValue)
        {
            questions = questions.Where(x => x.TopicId == query.TopicId.Value);
        }
        if (!string.IsNullOrWhiteSpace(query.Topic))
        {
            var topic = query.Topic.Trim().ToLower();
            questions = questions.Where(x => x.Topic != null && x.Topic.ToLower().Contains(topic));
        }
        if (query.Type.HasValue && Enum.IsDefined(typeof(QuestionType), query.Type.Value))
        {
            var type = (QuestionType)query.Type.Value;
            questions = questions.Where(x => x.Type == type);
        }
        if (query.Difficulty.HasValue && Enum.IsDefined(typeof(DifficultyLevel), query.Difficulty.Value))
        {
            var difficulty = (DifficultyLevel)query.Difficulty.Value;
            questions = questions.Where(x => x.Difficulty == difficulty);
        }
        if (query.Status.HasValue && Enum.IsDefined(typeof(QuestionStatus), query.Status.Value))
        {
            var status = (QuestionStatus)query.Status.Value;
            questions = questions.Where(x => x.Status == status);
        }
        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var search = query.Search.Trim().ToLower();
            questions = questions.Where(x => x.Translations.Any(t => t.Content.ToLower().Contains(search)));
        }

        questions = query.Sort?.ToLowerInvariant() switch
        {
            "difficulty" => questions.OrderBy(x => x.Difficulty).ThenByDescending(x => x.CreatedAtUtc),
            "type" => questions.OrderBy(x => x.Type).ThenByDescending(x => x.CreatedAtUtc),
            _ => questions.OrderByDescending(x => x.CreatedAtUtc)
        };

        var total = await questions.CountAsync(ct);
        var items = await questions.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(ct);
        return Result<QuestionPagedResponse>.Success(new(items.Select(x => ToResponse(x, language)).ToList(), page, pageSize, total));
    }

    public async Task<Result<QuestionResponse>> GetQuestionAsync(Guid id, SupportedLanguage language, CancellationToken ct)
    {
        var question = await FullQuery().SingleOrDefaultAsync(x => x.Id == id, ct);
        return question is null
            ? Result<QuestionResponse>.Failure(QuestionBankErrors.QuestionNotFound)
            : Result<QuestionResponse>.Success(ToResponse(question, language));
    }

    public async Task<Result<QuestionResponse>> CreateQuestionAsync(QuestionUpsertRequest request, SupportedLanguage language, CancellationToken ct)
    {
        var validation = await ValidateRequestAsync(request, ct);
        if (validation.IsFailure)
        {
            return validation.ValidationErrors is not null
                ? Result<QuestionResponse>.ValidationFailure(validation.ValidationErrors)
                : Result<QuestionResponse>.Failure(validation.Error!);
        }

        var question = CreateQuestionEntity(request);
        db.Questions.Add(question);
        await db.SaveChangesAsync(ct);
        return Result<QuestionResponse>.Success(ToResponse(question, language));
    }

    public async Task<Result<QuestionResponse>> UpdateQuestionAsync(Guid id, QuestionUpsertRequest request, SupportedLanguage language, CancellationToken ct)
    {
        var validation = await ValidateRequestAsync(request, ct);
        if (validation.IsFailure)
        {
            return validation.ValidationErrors is not null
                ? Result<QuestionResponse>.ValidationFailure(validation.ValidationErrors)
                : Result<QuestionResponse>.Failure(validation.Error!);
        }

        var question = await FullQuery().SingleOrDefaultAsync(x => x.Id == id, ct);
        if (question is null)
        {
            return Result<QuestionResponse>.Failure(QuestionBankErrors.QuestionNotFound);
        }

        ParseEnums(request, out var type, out var difficulty, out var status);
        var imageUrl = request.ImageUrl ?? question.ImageUrl;
        question.Update(request.SubjectId, request.TopicId, request.Topic, type, difficulty, status, imageUrl, clock.UtcNow);
        question.ReplaceContent(ToQuestionTranslations(question.Id, request.Translations), ToAnswerOptions(question.Id, request.AnswerOptions));
        await db.SaveChangesAsync(ct);
        return Result<QuestionResponse>.Success(ToResponse(question, language));
    }

    public async Task<Result> ArchiveQuestionAsync(Guid id, CancellationToken ct)
    {
        var question = await db.Questions.SingleOrDefaultAsync(x => x.Id == id, ct);
        if (question is null)
        {
            return Result.Failure(QuestionBankErrors.QuestionNotFound);
        }

        question.Archive(clock.UtcNow, "admin_archive");
        await db.SaveChangesAsync(ct);
        return Result.Success();
    }

    public async Task<Result> DeleteQuestionAsync(Guid id, CancellationToken ct)
    {
        var question = await db.Questions.SingleOrDefaultAsync(x => x.Id == id, ct);
        if (question is null)
        {
            return Result.Failure(QuestionBankErrors.QuestionNotFound);
        }

        if (question.Status == QuestionStatus.Draft)
        {
            db.Questions.Remove(question);
        }
        else
        {
            question.Archive(clock.UtcNow, "delete_requested_but_in_use");
        }

        await db.SaveChangesAsync(ct);
        return Result.Success();
    }

    public async Task<Result<IReadOnlyList<QuestionResponse>>> CreateQuestionsAsync(IReadOnlyList<QuestionUpsertRequest> requests, SupportedLanguage language, CancellationToken ct)
    {
        foreach (var request in requests)
        {
            var validation = await ValidateRequestAsync(request, ct);
            if (validation.IsFailure)
            {
                return validation.ValidationErrors is not null
                    ? Result<IReadOnlyList<QuestionResponse>>.ValidationFailure(validation.ValidationErrors)
                    : Result<IReadOnlyList<QuestionResponse>>.Failure(validation.Error!);
            }
        }

        await using var transaction = await db.Database.BeginTransactionAsync(ct);
        var questions = new List<Question>();
        foreach (var request in requests)
        {
            var question = CreateQuestionEntity(request);
            db.Questions.Add(question);
            questions.Add(question);
        }

        await db.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);
        return Result<IReadOnlyList<QuestionResponse>>.Success(questions.Select(x => ToResponse(x, language)).ToList());
    }

    public async Task<IReadOnlyList<string>> GetQuestionTextsAsync(Guid subjectId, Guid? topicId, CancellationToken ct)
    {
        var query = db.Questions.Include(x => x.Translations).AsNoTracking().Where(x => x.SubjectId == subjectId);
        if (topicId.HasValue)
        {
            query = query.Where(x => x.TopicId == topicId.Value);
        }

        return await query.SelectMany(x => x.Translations.Select(t => t.Content)).ToListAsync(ct);
    }

    private async Task<Result> ValidateRequestAsync(QuestionUpsertRequest request, CancellationToken ct)
    {
        var validation = Validation.ValidateQuestion(request);
        if (validation.IsFailure)
        {
            return validation;
        }

        if (!await catalogLookup.SubjectExistsAsync(request.SubjectId, ct))
        {
            return Result.Failure(QuestionBankErrors.SubjectNotFound);
        }

        if (request.TopicId.HasValue && !await catalogLookup.TopicBelongsToSubjectAsync(request.TopicId.Value, request.SubjectId, ct))
        {
            return Result.Failure(QuestionBankErrors.TopicNotFound);
        }

        return Result.Success();
    }

    private Question CreateQuestionEntity(QuestionUpsertRequest request)
    {
        ParseEnums(request, out var type, out var difficulty, out var status);
        var now = clock.UtcNow;
        var question = new Question(Guid.NewGuid(), request.SubjectId, request.TopicId, request.Topic, type, difficulty, request.ImageUrl, now);
        question.Update(request.SubjectId, request.TopicId, request.Topic, type, difficulty, status, request.ImageUrl, now);
        question.ReplaceContent(ToQuestionTranslations(question.Id, request.Translations), ToAnswerOptions(question.Id, request.AnswerOptions));
        return question;
    }

    private IQueryable<Question> FullQuery() =>
        db.Questions
            .Include(x => x.Translations)
            .Include(x => x.AnswerOptions).ThenInclude(x => x.Translations);

    private static void ParseEnums(QuestionUpsertRequest request, out QuestionType type, out DifficultyLevel difficulty, out QuestionStatus status)
    {
        type = (QuestionType)request.Type;
        difficulty = (DifficultyLevel)request.Difficulty;
        status = (QuestionStatus)request.Status;
    }

    private static IReadOnlyList<QuestionTranslation> ToQuestionTranslations(Guid questionId, IEnumerable<QuestionTranslationRequest> translations) =>
        translations.Select(x =>
        {
            Validation.TryParseLanguage(x.Language, out var language);
            return new QuestionTranslation(questionId, language, x.Content, x.Explanation);
        }).ToList();

    private static IReadOnlyList<AnswerOption> ToAnswerOptions(Guid questionId, IEnumerable<AnswerOptionRequest> options) =>
        options.OrderBy(x => x.DisplayOrder).Select(x =>
        {
            var option = new AnswerOption(Guid.NewGuid(), questionId, x.DisplayOrder, x.IsCorrect);
            option.ReplaceTranslations(x.Translations.Select(t =>
            {
                Validation.TryParseLanguage(t.Language, out var language);
                return new AnswerOptionTranslation(option.Id, language, t.Text, t.MatchPairText);
            }).ToList());
            return option;
        }).ToList();

    private static QuestionResponse ToResponse(Question question, SupportedLanguage language) =>
        new(
            question.Id,
            question.SubjectId,
            question.TopicId,
            question.Topic,
            (int)question.Type,
            (int)question.Difficulty,
            (int)question.Status,
            question.ContentFor(language),
            question.ImageUrl,
            question.Translations.Select(ToQuestionTranslationResponse).ToList(),
            question.AnswerOptions.OrderBy(x => x.DisplayOrder).Select(ToAnswerOptionResponse).ToList());

    private static QuestionTranslationResponse ToQuestionTranslationResponse(QuestionTranslation translation) =>
        new((int)translation.Language, translation.Content, translation.Explanation);

    private static AnswerOptionResponse ToAnswerOptionResponse(AnswerOption option) =>
        new(option.Id, option.DisplayOrder, option.IsCorrect, option.Translations.Select(ToAnswerTranslationResponse).ToList());

    private static AnswerOptionTranslationResponse ToAnswerTranslationResponse(AnswerOptionTranslation translation) =>
        new((int)translation.Language, translation.Text, translation.MatchPairText);
}
