using System.Globalization;
using System.Security.Claims;
using System.Text.RegularExpressions;
using Adeeb.Application.Abstractions.Time;
using Adeeb.Modules.Vocabulary.Contracts;
using Adeeb.Modules.Vocabulary.Domain;
using Adeeb.Modules.Vocabulary.Infrastructure.Persistence;
using Adeeb.SharedKernel.Results;
using Microsoft.EntityFrameworkCore;

namespace Adeeb.Modules.Vocabulary.Application;

public sealed class VocabularyAdminService(VocabularyDbContext db, IDateTimeProvider clock)
{
    public async Task<Result<VocabularyPage<LearningLanguageDto>>> GetLanguagesAsync(VocabularyListQuery query, CancellationToken ct)
    {
        var (page, size) = VocabularyValidation.Page(query.Page, query.PageSize); var q = db.Languages.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(query.Search)) { var s = query.Search.Trim().ToLower(); q = q.Where(x => x.Code.Contains(s) || x.NameTg.ToLower().Contains(s) || x.NameRu.ToLower().Contains(s)); }
        var total = await q.CountAsync(ct); var items = await q.OrderBy(x => x.DisplayOrder).ThenBy(x => x.Code).Skip((page - 1) * size).Take(size).ToListAsync(ct);
        return Result<VocabularyPage<LearningLanguageDto>>.Success(new(items.Select(ToLanguage).ToList(), page, size, total));
    }

    public async Task<Result<LearningLanguageDto>> CreateLanguageAsync(LanguageUpsertRequest request, CancellationToken ct)
    {
        var validation = VocabularyValidation.Validate(request); if (validation.IsFailure) return Result<LearningLanguageDto>.ValidationFailure(validation.ValidationErrors!);
        var code = LearningLanguage.NormalizeCode(request.Code); if (await db.Languages.AnyAsync(x => x.Code == code, ct)) return Result<LearningLanguageDto>.Failure(VocabularyErrors.Duplicate);
        var entity = new LearningLanguage(Guid.NewGuid(), code, request.NameTg, request.NameRu, request.DisplayOrder, clock.UtcNow); entity.Update(code, request.NameTg, request.NameRu, request.DisplayOrder, request.IsActive, clock.UtcNow);
        db.Languages.Add(entity); await db.SaveChangesAsync(ct); return Result<LearningLanguageDto>.Success(ToLanguage(entity));
    }

    public async Task<Result<LearningLanguageDto>> UpdateLanguageAsync(Guid id, LanguageUpsertRequest request, CancellationToken ct)
    {
        var validation = VocabularyValidation.Validate(request); if (validation.IsFailure) return Result<LearningLanguageDto>.ValidationFailure(validation.ValidationErrors!);
        var entity = await db.Languages.FindAsync([id], ct); if (entity is null) return Result<LearningLanguageDto>.Failure(VocabularyErrors.LanguageNotFound);
        var code = LearningLanguage.NormalizeCode(request.Code); if (await db.Languages.AnyAsync(x => x.Id != id && x.Code == code, ct)) return Result<LearningLanguageDto>.Failure(VocabularyErrors.Duplicate);
        entity.Update(code, request.NameTg, request.NameRu, request.DisplayOrder, request.IsActive, clock.UtcNow); await db.SaveChangesAsync(ct); return Result<LearningLanguageDto>.Success(ToLanguage(entity));
    }

    public async Task<Result<VocabularyPage<VocabularyTopicDto>>> GetTopicsAsync(VocabularyListQuery query, CancellationToken ct)
    {
        var (page, size) = VocabularyValidation.Page(query.Page, query.PageSize); var q = db.Topics.AsNoTracking();
        if (query.LanguageId is not null) q = q.Where(x => x.LanguageId == query.LanguageId); if (query.Level is not null && Enum.IsDefined(typeof(VocabularyLevel), query.Level.Value)) q = q.Where(x => x.Level == (VocabularyLevel)query.Level.Value);
        if (query.Status is not null && Enum.IsDefined(typeof(VocabularyContentStatus), query.Status.Value)) q = q.Where(x => x.Status == (VocabularyContentStatus)query.Status.Value);
        if (!string.IsNullOrWhiteSpace(query.Search)) { var s = query.Search.Trim().ToLower(); q = q.Where(x => x.NameTg.ToLower().Contains(s) || x.NameRu.ToLower().Contains(s)); }
        var total = await q.CountAsync(ct); var items = await q.OrderBy(x => x.Level).ThenBy(x => x.NameRu).Skip((page - 1) * size).Take(size).ToListAsync(ct);
        return Result<VocabularyPage<VocabularyTopicDto>>.Success(new(items.Select(ToTopic).ToList(), page, size, total));
    }

    public Task<Result<VocabularyTopicDto>> CreateTopicAsync(TopicUpsertRequest request, CancellationToken ct) => UpsertTopicAsync(null, request, ct);
    public Task<Result<VocabularyTopicDto>> UpdateTopicAsync(Guid id, TopicUpsertRequest request, CancellationToken ct) => UpsertTopicAsync(id, request, ct);
    private async Task<Result<VocabularyTopicDto>> UpsertTopicAsync(Guid? id, TopicUpsertRequest request, CancellationToken ct)
    {
        var validation = VocabularyValidation.Validate(request); if (validation.IsFailure) return Result<VocabularyTopicDto>.ValidationFailure(validation.ValidationErrors!);
        var language = await db.Languages.FindAsync([request.LanguageId], ct); if (language is null) return Result<VocabularyTopicDto>.Failure(VocabularyErrors.LanguageNotFound);
        var status = (VocabularyContentStatus)request.Status; if (status == VocabularyContentStatus.Published && !language.IsActive) return Result<VocabularyTopicDto>.Failure(VocabularyErrors.PublishInvalid);
        VocabularyTopic entity;
        if (id is null) { entity = new(Guid.NewGuid(), request.LanguageId, (VocabularyLevel)request.Level, request.NameTg, request.NameRu, request.DescriptionTg, request.DescriptionRu, clock.UtcNow); db.Topics.Add(entity); }
        else { entity = await db.Topics.FindAsync([id.Value], ct) ?? null!; if (entity is null) return Result<VocabularyTopicDto>.Failure(VocabularyErrors.TopicNotFound); }
        entity.Update(request.LanguageId, (VocabularyLevel)request.Level, request.NameTg, request.NameRu, request.DescriptionTg, request.DescriptionRu, status, clock.UtcNow);
        await db.SaveChangesAsync(ct); return Result<VocabularyTopicDto>.Success(ToTopic(entity));
    }

    public async Task<Result<VocabularyPage<VocabularyWordDto>>> GetWordsAsync(VocabularyListQuery query, CancellationToken ct)
    {
        var (page, size) = VocabularyValidation.Page(query.Page, query.PageSize); var q = db.Words.AsNoTracking();
        if (query.LanguageId is not null) q = q.Where(x => x.LanguageId == query.LanguageId); if (query.TopicId is not null) q = q.Where(x => x.TopicId == query.TopicId);
        if (query.Level is not null && Enum.IsDefined(typeof(VocabularyLevel), query.Level.Value)) q = q.Where(x => x.Level == (VocabularyLevel)query.Level.Value);
        if (query.Status is not null && Enum.IsDefined(typeof(VocabularyContentStatus), query.Status.Value)) q = q.Where(x => x.Status == (VocabularyContentStatus)query.Status.Value);
        if (!string.IsNullOrWhiteSpace(query.Search)) { var s = query.Search.Trim().ToLower(); q = q.Where(x => x.TargetText.ToLower().Contains(s) || x.TranslationTg.ToLower().Contains(s) || x.TranslationRu.ToLower().Contains(s)); }
        var total = await q.CountAsync(ct); var words = await q.OrderBy(x => x.TargetText).Skip((page - 1) * size).Take(size).ToListAsync(ct); var ids = words.Select(x => x.Id).ToArray();
        var relations = await db.Relations.AsNoTracking().Where(x => ids.Contains(x.WordId)).ToListAsync(ct); var relatedIds = relations.Select(x => x.RelatedWordId).Distinct().ToArray(); var related = await db.Words.AsNoTracking().Where(x => relatedIds.Contains(x.Id)).ToDictionaryAsync(x => x.Id, ct);
        return Result<VocabularyPage<VocabularyWordDto>>.Success(new(words.Select(x => ToWord(x, relations.Where(r => r.WordId == x.Id), related)).ToList(), page, size, total));
    }

    public async Task<Result<VocabularyWordDto>> GetWordAsync(Guid id, CancellationToken ct)
    {
        var word = await db.Words.AsNoTracking().SingleOrDefaultAsync(x => x.Id == id, ct); if (word is null) return Result<VocabularyWordDto>.Failure(VocabularyErrors.WordNotFound);
        var relations = await db.Relations.AsNoTracking().Where(x => x.WordId == id).ToListAsync(ct); var ids = relations.Select(x => x.RelatedWordId).ToArray(); var related = await db.Words.AsNoTracking().Where(x => ids.Contains(x.Id)).ToDictionaryAsync(x => x.Id, ct);
        return Result<VocabularyWordDto>.Success(ToWord(word, relations, related));
    }

    public Task<Result<VocabularyWordDto>> CreateWordAsync(WordUpsertRequest request, CancellationToken ct) => UpsertWordAsync(null, request, ct);
    public Task<Result<VocabularyWordDto>> UpdateWordAsync(Guid id, WordUpsertRequest request, CancellationToken ct) => UpsertWordAsync(id, request, ct);
    private async Task<Result<VocabularyWordDto>> UpsertWordAsync(Guid? id, WordUpsertRequest request, CancellationToken ct)
    {
        var validation = VocabularyValidation.Validate(request); if (validation.IsFailure) return Result<VocabularyWordDto>.ValidationFailure(validation.ValidationErrors!);
        var language = await db.Languages.FindAsync([request.LanguageId], ct); var topic = await db.Topics.FindAsync([request.TopicId], ct);
        if (language is null) return Result<VocabularyWordDto>.Failure(VocabularyErrors.LanguageNotFound); if (topic is null || topic.LanguageId != request.LanguageId) return Result<VocabularyWordDto>.Failure(VocabularyErrors.TopicNotFound);
        var status = (VocabularyContentStatus)request.Status; if (status == VocabularyContentStatus.Published && (!language.IsActive || topic.Status != VocabularyContentStatus.Published)) return Result<VocabularyWordDto>.Failure(VocabularyErrors.PublishInvalid);
        var normalized = VocabularyWord.Normalize(request.TargetText); if (await db.Words.AnyAsync(x => x.LanguageId == request.LanguageId && x.NormalizedText == normalized && (id == null || x.Id != id), ct)) return Result<VocabularyWordDto>.Failure(VocabularyErrors.Duplicate);
        VocabularyWord entity;
        if (id is null) { entity = new(Guid.NewGuid(), request.LanguageId, request.TopicId, (VocabularyLevel)request.Level, request.TargetText, request.TranslationTg, request.TranslationRu, request.ExplanationTg, request.ExplanationRu, request.ExampleTarget, request.ExampleTg, request.ExampleRu, clock.UtcNow); db.Words.Add(entity); }
        else
        {
            entity = await db.Words.FindAsync([id.Value], ct) ?? null!; if (entity is null) return Result<VocabularyWordDto>.Failure(VocabularyErrors.WordNotFound);
            var contextChanged = entity.LanguageId != request.LanguageId || entity.TopicId != request.TopicId || entity.Level != (VocabularyLevel)request.Level;
            if (contextChanged && await db.Questions.AnyAsync(x => x.WordId == entity.Id && x.Status == VocabularyContentStatus.Published, ct)) return Result<VocabularyWordDto>.Failure(VocabularyErrors.PublishInvalid);
        }
        entity.Update(request.LanguageId, request.TopicId, (VocabularyLevel)request.Level, request.TargetText, request.TranslationTg, request.TranslationRu, request.ExplanationTg, request.ExplanationRu, request.ExampleTarget, request.ExampleTg, request.ExampleRu, status, clock.UtcNow);
        var old = await db.Relations.Where(x => x.WordId == entity.Id).ToListAsync(ct); db.Relations.RemoveRange(old);
        var relationRequests = request.Relations ?? []; var relatedIds = relationRequests.Select(x => x.RelatedWordId).Distinct().ToArray(); var relatedWords = await db.Words.Where(x => relatedIds.Contains(x.Id)).ToListAsync(ct);
        if (relationRequests.Any(x => x.RelatedWordId == entity.Id || !Enum.IsDefined(typeof(VocabularyRelationType), x.Type)) || relatedWords.Count != relatedIds.Length || relatedWords.Any(x => x.LanguageId != request.LanguageId || x.Status != VocabularyContentStatus.Published)) return Result<VocabularyWordDto>.Failure(VocabularyErrors.PublishInvalid);
        foreach (var relation in relationRequests.DistinctBy(x => new { x.RelatedWordId, x.Type })) db.Relations.Add(new VocabularyRelation(Guid.NewGuid(), entity.Id, relation.RelatedWordId, (VocabularyRelationType)relation.Type, clock.UtcNow));
        await db.SaveChangesAsync(ct); return await GetWordAsync(entity.Id, ct);
    }

    public async Task<Result> ArchiveWordAsync(Guid id, CancellationToken ct)
    { var word = await db.Words.FindAsync([id], ct); if (word is null) return Result.Failure(VocabularyErrors.WordNotFound); word.Archive(clock.UtcNow); await db.SaveChangesAsync(ct); return Result.Success(); }

    public async Task<Result<VocabularyPage<VocabularyQuestionDto>>> GetQuestionsAsync(VocabularyListQuery query, CancellationToken ct)
    {
        var (page, size) = VocabularyValidation.Page(query.Page, query.PageSize); var q = db.Questions.AsNoTracking().Include(x => x.Options).AsQueryable();
        if (query.Type is not null && Enum.IsDefined(typeof(VocabularyQuestionType), query.Type.Value)) q = q.Where(x => x.Type == (VocabularyQuestionType)query.Type.Value); if (query.Status is not null && Enum.IsDefined(typeof(VocabularyContentStatus), query.Status.Value)) q = q.Where(x => x.Status == (VocabularyContentStatus)query.Status.Value);
        if (query.LanguageId is not null) q = q.Where(x => db.Words.Any(w => w.Id == x.WordId && w.LanguageId == query.LanguageId)); if (query.TopicId is not null) q = q.Where(x => db.Words.Any(w => w.Id == x.WordId && w.TopicId == query.TopicId));
        var total = await q.CountAsync(ct); var items = await q.OrderByDescending(x => x.UpdatedAtUtc).Skip((page - 1) * size).Take(size).ToListAsync(ct); return Result<VocabularyPage<VocabularyQuestionDto>>.Success(new(items.Select(ToQuestion).ToList(), page, size, total));
    }

    public Task<Result<VocabularyQuestionDto>> CreateQuestionAsync(QuestionUpsertRequest request, CancellationToken ct) => UpsertQuestionAsync(null, request, ct);
    public Task<Result<VocabularyQuestionDto>> UpdateQuestionAsync(Guid id, QuestionUpsertRequest request, CancellationToken ct) => UpsertQuestionAsync(id, request, ct);
    private async Task<Result<VocabularyQuestionDto>> UpsertQuestionAsync(Guid? id, QuestionUpsertRequest request, CancellationToken ct)
    {
        var validation = VocabularyValidation.Validate(request); if (validation.IsFailure) return Result<VocabularyQuestionDto>.ValidationFailure(validation.ValidationErrors!);
        if (!await db.Words.AnyAsync(x => x.Id == request.WordId, ct)) return Result<VocabularyQuestionDto>.Failure(VocabularyErrors.WordNotFound);
        VocabularyQuestion entity;
        if (id is null) { entity = new(Guid.NewGuid(), request.WordId, (VocabularyQuestionType)request.Type, request.PromptTarget, request.PromptTg, request.PromptRu, request.CorrectTokenIndex, clock.UtcNow); db.Questions.Add(entity); }
        else { entity = await db.Questions.Include(x => x.Options).SingleOrDefaultAsync(x => x.Id == id, ct) ?? null!; if (entity is null) return Result<VocabularyQuestionDto>.Failure(VocabularyErrors.QuestionNotFound); }
        var options = request.Options.Select(x => new VocabularyQuestionOption(x.Id ?? Guid.NewGuid(), entity.Id, x.WordId, x.ValueTarget, x.ValueTg, x.ValueRu, x.DisplayOrder, x.IsCorrect, x.CorrectOrder)).ToList();
        entity.ChangeDefinition(request.WordId, (VocabularyQuestionType)request.Type, request.PromptTarget, request.PromptTg, request.PromptRu, request.CorrectTokenIndex, options, clock.UtcNow); await db.SaveChangesAsync(ct); return Result<VocabularyQuestionDto>.Success(ToQuestion(entity));
    }

    public async Task<Result<VocabularyQuestionDto>> PublishQuestionAsync(Guid id, ClaimsPrincipal principal, CancellationToken ct)
    {
        var entity = await db.Questions.Include(x => x.Options).SingleOrDefaultAsync(x => x.Id == id, ct); if (entity is null) return Result<VocabularyQuestionDto>.Failure(VocabularyErrors.QuestionNotFound);
        var wordPublished = await db.Words.AnyAsync(x => x.Id == entity.WordId && x.Status == VocabularyContentStatus.Published, ct); if (!wordPublished || !QuestionIsPublishable(entity)) return Result<VocabularyQuestionDto>.Failure(VocabularyErrors.PublishInvalid);
        entity.Publish(UserId(principal) ?? Guid.Empty, clock.UtcNow); await db.SaveChangesAsync(ct); return Result<VocabularyQuestionDto>.Success(ToQuestion(entity));
    }
    public async Task<Result> ArchiveQuestionAsync(Guid id, CancellationToken ct) { var entity = await db.Questions.FindAsync([id], ct); if (entity is null) return Result.Failure(VocabularyErrors.QuestionNotFound); entity.Archive(clock.UtcNow); await db.SaveChangesAsync(ct); return Result.Success(); }

    public async Task<Result<DraftGenerationResult>> GenerateDraftsAsync(Guid wordId, CancellationToken ct)
    {
        var word = await db.Words.SingleOrDefaultAsync(x => x.Id == wordId, ct); if (word is null) return Result<DraftGenerationResult>.Failure(VocabularyErrors.WordNotFound); if (word.Status != VocabularyContentStatus.Published) return Result<DraftGenerationResult>.Failure(VocabularyErrors.PublishInvalid);
        var pool = await db.Words.AsNoTracking().Where(x => x.Id != word.Id && x.LanguageId == word.LanguageId && x.TopicId == word.TopicId && x.Level == word.Level && x.Status == VocabularyContentStatus.Published).OrderBy(x => x.Id).Take(12).ToListAsync(ct);
        var relations = await db.Relations.AsNoTracking().Where(x => x.WordId == word.Id).ToListAsync(ct); var relationIds = relations.Select(x => x.RelatedWordId).ToArray(); var relationWords = await db.Words.AsNoTracking().Where(x => relationIds.Contains(x.Id)).ToDictionaryAsync(x => x.Id, ct);
        var existing = await db.Questions.Where(x => x.WordId == wordId && x.Status != VocabularyContentStatus.Archived).Select(x => x.Type).ToListAsync(ct); var created = new List<VocabularyQuestion>(); var warnings = new List<DraftGenerationWarning>();
        var now = clock.UtcNow;
        foreach (var type in Enum.GetValues<VocabularyQuestionType>())
        {
            if (existing.Contains(type)) { warnings.Add(new((int)type, "already_exists")); continue; }
            var question = BuildDraft(word, pool, relations, relationWords, type, now); if (question is null) { warnings.Add(new((int)type, "insufficient_content")); continue; }
            created.Add(question); db.Questions.Add(question);
        }
        await db.SaveChangesAsync(ct); return Result<DraftGenerationResult>.Success(new(created.Select(ToQuestion).ToList(), warnings));
    }

    public async Task<Result<VocabularyPage<DailyWordDto>>> GetDailyWordsAsync(VocabularyListQuery query, CancellationToken ct)
    {
        var (page, size) = VocabularyValidation.Page(query.Page, query.PageSize); var q = db.DailyWords.AsNoTracking(); if (query.LanguageId is not null) q = q.Where(x => x.LanguageId == query.LanguageId);
        var total = await q.CountAsync(ct); var rows = await q.OrderByDescending(x => x.LocalDate).Skip((page - 1) * size).Take(size).ToListAsync(ct); var ids = rows.Select(x => x.WordId).ToArray(); var words = await db.Words.AsNoTracking().Where(x => ids.Contains(x.Id)).ToDictionaryAsync(x => x.Id, ct);
        return Result<VocabularyPage<DailyWordDto>>.Success(new(rows.Select(x => new DailyWordDto(x.LanguageId, x.LocalDate, x.IsAutomatic, ToWord(words[x.WordId], [], new Dictionary<Guid, VocabularyWord>()))).ToList(), page, size, total));
    }
    public async Task<Result<DailyWordDto>> UpsertDailyWordAsync(DailyWordUpsertRequest request, CancellationToken ct)
    {
        var languageActive = await db.Languages.AnyAsync(x => x.Id == request.LanguageId && x.IsActive, ct);
        var word = await db.Words.AsNoTracking().SingleOrDefaultAsync(x => x.Id == request.WordId, ct); if (!languageActive || word is null || word.LanguageId != request.LanguageId || word.Status != VocabularyContentStatus.Published || !await db.Topics.AnyAsync(x => x.Id == word.TopicId && x.Status == VocabularyContentStatus.Published, ct)) return Result<DailyWordDto>.Failure(VocabularyErrors.PublishInvalid);
        var entity = await db.DailyWords.FindAsync([request.LanguageId, request.LocalDate], ct); if (entity is null) { entity = new(request.LanguageId, request.LocalDate, request.WordId, false, clock.UtcNow); db.DailyWords.Add(entity); } else entity.Replace(request.WordId, clock.UtcNow);
        await db.SaveChangesAsync(ct); return Result<DailyWordDto>.Success(new(entity.LanguageId, entity.LocalDate, entity.IsAutomatic, ToWord(word, [], new Dictionary<Guid, VocabularyWord>())));
    }

    private static VocabularyQuestion? BuildDraft(VocabularyWord word, IReadOnlyList<VocabularyWord> pool, IReadOnlyList<VocabularyRelation> relations, IReadOnlyDictionary<Guid, VocabularyWord> relationWords, VocabularyQuestionType type, DateTimeOffset now)
    {
        var id = Guid.NewGuid(); var options = new List<VocabularyQuestionOption>(); VocabularyQuestion? q;
        if (type is (VocabularyQuestionType.Translation or VocabularyQuestionType.FillBlank) && pool.Count < 3) return null;
        switch (type)
        {
            case VocabularyQuestionType.Translation:
                q = new(id, word.Id, type, word.TargetText, word.TargetText, word.TargetText, null, now); AddWordOptions(q, word, pool.Take(4), now); break;
            case VocabularyQuestionType.FillBlank:
                var blank = ReplaceFirst(word.ExampleTarget, word.TargetText, "____"); if (blank == word.ExampleTarget) return null;
                q = new(id, word.Id, type, blank, word.ExampleTg, word.ExampleRu, null, now); AddTargetOptions(q, word, pool.Take(4), now); break;
            case VocabularyQuestionType.Synonym:
            case VocabularyQuestionType.Antonym:
                var relationType = type == VocabularyQuestionType.Synonym ? VocabularyRelationType.Synonym : VocabularyRelationType.Antonym;
                var correctRelation = relations.FirstOrDefault(x => x.Type == relationType && relationWords.ContainsKey(x.RelatedWordId)); if (correctRelation is null || pool.Count < 3) return null;
                var correct = relationWords[correctRelation.RelatedWordId]; q = new(id, word.Id, type, word.TargetText, word.TranslationTg, word.TranslationRu, null, now); AddTargetOptions(q, correct, pool.Where(x => x.Id != correct.Id).Take(4), now); break;
            case VocabularyQuestionType.OddWordReplacement:
                if (pool.Count < 4) return null; var tokens = Tokenize(word.ExampleTarget); var index = Array.FindIndex(tokens, x => NormalizeToken(x) == NormalizeToken(word.TargetText)); if (index < 0) return null;
                var wrong = pool[0]; tokens[index] = wrong.TargetText; q = new(id, word.Id, type, string.Join(' ', tokens), word.ExampleTg, word.ExampleRu, index, now); AddTargetOptions(q, word, pool.Skip(1).Take(4), now); break;
            case VocabularyQuestionType.WordOrder:
                var ordered = Tokenize(word.ExampleTarget); if (ordered.Length is < 2 or > 12) return null; q = new(id, word.Id, type, word.ExampleTg, word.ExampleTg, word.ExampleRu, null, now);
                options.AddRange(ordered.Select((x, i) => new VocabularyQuestionOption(Guid.NewGuid(), id, null, x, x, x, i, true, i))); q.Replace(q.PromptTarget, q.PromptTg, q.PromptRu, null, options.OrderBy(x => x.Id), now); break;
            default: return null;
        }
        return q;
    }
    private static void AddWordOptions(VocabularyQuestion q, VocabularyWord correct, IEnumerable<VocabularyWord> distractors, DateTimeOffset now)
    { var words = distractors.Prepend(correct).DistinctBy(x => x.Id).Take(5).ToList(); var opts = words.Select((x, i) => new VocabularyQuestionOption(Guid.NewGuid(), q.Id, x.Id, x.TranslationRu, x.TranslationTg, x.TranslationRu, i, x.Id == correct.Id, null)); q.Replace(q.PromptTarget, q.PromptTg, q.PromptRu, q.CorrectTokenIndex, opts, now); }
    private static void AddTargetOptions(VocabularyQuestion q, VocabularyWord correct, IEnumerable<VocabularyWord> distractors, DateTimeOffset now)
    { var words = distractors.Prepend(correct).DistinctBy(x => x.Id).Take(5).ToList(); var opts = words.Select((x, i) => new VocabularyQuestionOption(Guid.NewGuid(), q.Id, x.Id, x.TargetText, x.TargetText, x.TargetText, i, x.Id == correct.Id, null)); q.Replace(q.PromptTarget, q.PromptTg, q.PromptRu, q.CorrectTokenIndex, opts, now); }
    private static bool QuestionIsPublishable(VocabularyQuestion q) => q.Type == VocabularyQuestionType.WordOrder
        ? q.Options.Count is >= 2 and <= 12 && q.Options.All(x => x.CorrectOrder is not null) && q.Options.Select(x => x.CorrectOrder).Distinct().Count() == q.Options.Count
        : q.Options.Count is >= 4 and <= 5 && q.Options.Count(x => x.IsCorrect) == 1 && (q.Type != VocabularyQuestionType.OddWordReplacement || q.CorrectTokenIndex is not null);
    private static string ReplaceFirst(string input, string value, string replacement) => Regex.Replace(input, Regex.Escape(value), replacement, RegexOptions.IgnoreCase, TimeSpan.FromMilliseconds(100));
    private static string[] Tokenize(string input) => input.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
    private static string NormalizeToken(string value) => value.Trim(' ', '.', ',', '!', '?', ';', ':').ToUpperInvariant();
    private static Guid? UserId(ClaimsPrincipal p) => Guid.TryParse(p.FindFirstValue(ClaimTypes.NameIdentifier) ?? p.FindFirstValue("sub"), out var id) ? id : null;
    private static bool Russian => CultureInfo.CurrentUICulture.Name.StartsWith("ru", StringComparison.OrdinalIgnoreCase);
    private static LearningLanguageDto ToLanguage(LearningLanguage x) => new(x.Id, x.Code, Russian ? x.NameRu : x.NameTg, x.NameTg, x.NameRu, x.DisplayOrder, x.IsActive);
    private static VocabularyTopicDto ToTopic(VocabularyTopic x) => new(x.Id, x.LanguageId, (int)x.Level, Russian ? x.NameRu : x.NameTg, x.NameTg, x.NameRu, Russian ? x.DescriptionRu : x.DescriptionTg, x.DescriptionTg, x.DescriptionRu, (int)x.Status);
    internal static VocabularyWordDto ToWord(VocabularyWord x, IEnumerable<VocabularyRelation> relations, IReadOnlyDictionary<Guid, VocabularyWord> related) => new(x.Id, x.LanguageId, x.TopicId, (int)x.Level, x.TargetText, Russian ? x.TranslationRu : x.TranslationTg, x.TranslationTg, x.TranslationRu, Russian ? x.ExplanationRu : x.ExplanationTg, x.ExplanationTg, x.ExplanationRu, x.ExampleTarget, Russian ? x.ExampleRu : x.ExampleTg, x.ExampleTg, x.ExampleRu, (int)x.Status, relations.Where(r => related.ContainsKey(r.RelatedWordId)).Select(r => new VocabularyRelationDto(r.Id, r.RelatedWordId, related[r.RelatedWordId].TargetText, (int)r.Type)).ToList());
    internal static VocabularyQuestionDto ToQuestion(VocabularyQuestion x) => new(x.Id, x.WordId, (int)x.Type, Russian ? x.PromptRu : x.PromptTg, x.PromptTarget, x.PromptTg, x.PromptRu, x.CorrectTokenIndex, (int)x.Status, x.ReviewedBy, x.ReviewedAtUtc, x.Options.OrderBy(o => o.DisplayOrder).Select(o => new VocabularyQuestionOptionDto(o.Id, o.WordId, Russian ? o.ValueRu : o.ValueTg, o.ValueTarget, o.ValueTg, o.ValueRu, o.DisplayOrder, o.IsCorrect, o.CorrectOrder)).ToList());
}
