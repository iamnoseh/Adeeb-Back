using Adeeb.Application.Abstractions.Localization;
using Adeeb.Application.Abstractions.Time;
using Adeeb.Modules.AcademicCatalog.Contracts;
using Adeeb.Modules.AcademicCatalog.Domain;
using Adeeb.Modules.AcademicCatalog.Infrastructure.Persistence;
using Adeeb.SharedKernel.Results;
using Microsoft.EntityFrameworkCore;

namespace Adeeb.Modules.AcademicCatalog.Application;

public sealed class AcademicCatalogService(AcademicCatalogDbContext db, IDateTimeProvider clock) : IAcademicCatalogLookup
{
    public Task<bool> SubjectExistsAsync(Guid subjectId, CancellationToken ct) =>
        db.Subjects.AnyAsync(x => x.Id == subjectId && x.Status != AcademicItemStatus.Archived, ct);

    public Task<bool> TopicBelongsToSubjectAsync(Guid topicId, Guid subjectId, CancellationToken ct) =>
        db.Topics.AnyAsync(x => x.Id == topicId && x.SubjectId == subjectId && x.Status != AcademicItemStatus.Archived, ct);

    public async Task<Result<PagedResponse<SubjectResponse>>> GetSubjectsAsync(AcademicListQuery query, SupportedLanguage language, bool admin, CancellationToken ct)
    {
        var page = Math.Max(1, query.Page);
        var pageSize = Math.Clamp(query.PageSize, 1, 100);
        var subjects = db.Subjects.Include(x => x.Translations).AsNoTracking();
        if (!admin)
        {
            subjects = subjects.Where(x => x.Status == AcademicItemStatus.Active);
        }

        if (Validation.TryParseStatus(query.Status, out var status) && query.Status.HasValue)
        {
            subjects = subjects.Where(x => x.Status == status);
        }

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var search = query.Search.Trim().ToLower();
            subjects = subjects.Where(x => x.Code.ToLower().Contains(search) || x.Translations.Any(t => t.Name.ToLower().Contains(search)));
        }

        subjects = query.Sort?.ToLowerInvariant() switch
        {
            "code" => subjects.OrderBy(x => x.Code),
            "name" => subjects.OrderBy(x => x.Translations.Where(t => t.Language == language).Select(t => t.Name).FirstOrDefault()),
            _ => subjects.OrderBy(x => x.DisplayOrder).ThenBy(x => x.Code)
        };

        var total = await subjects.CountAsync(ct);
        var items = await subjects.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(ct);
        return Result<PagedResponse<SubjectResponse>>.Success(new(items.Select(x => ToSubjectResponse(x, language)).ToList(), page, pageSize, total));
    }

    public async Task<Result<SubjectResponse>> GetSubjectAsync(Guid id, SupportedLanguage language, bool admin, CancellationToken ct)
    {
        var subject = await db.Subjects.Include(x => x.Translations).SingleOrDefaultAsync(x => x.Id == id, ct);
        if (subject is null || (!admin && subject.Status != AcademicItemStatus.Active))
        {
            return Result<SubjectResponse>.Failure(AcademicCatalogErrors.SubjectNotFound);
        }

        return Result<SubjectResponse>.Success(ToSubjectResponse(subject, language));
    }

    public async Task<Result<SubjectResponse>> CreateSubjectAsync(SubjectUpsertRequest request, SupportedLanguage language, CancellationToken ct)
    {
        var validation = Validation.ValidateSubject(request);
        if (validation.IsFailure)
        {
            return Result<SubjectResponse>.ValidationFailure(validation.ValidationErrors!);
        }

        var code = Subject.NormalizeCode(request.Code);
        if (await db.Subjects.AnyAsync(x => x.Code == code, ct))
        {
            return Result<SubjectResponse>.Failure(AcademicCatalogErrors.DuplicateSubjectCode);
        }

        var now = clock.UtcNow;
        var subject = new Subject(Guid.NewGuid(), request.Code, request.IconUrl, request.DisplayOrder, now);
        Validation.TryParseStatus(request.Status, out var status);
        subject.Update(request.Code, request.IconUrl, request.DisplayOrder, status, now);
        subject.ReplaceTranslations(ToSubjectTranslations(subject.Id, request.Translations));
        db.Subjects.Add(subject);
        await db.SaveChangesAsync(ct);
        return Result<SubjectResponse>.Success(ToSubjectResponse(subject, language));
    }

    public async Task<Result<SubjectResponse>> UpdateSubjectAsync(Guid id, SubjectUpsertRequest request, SupportedLanguage language, CancellationToken ct)
    {
        var validation = Validation.ValidateSubject(request);
        if (validation.IsFailure)
        {
            return Result<SubjectResponse>.ValidationFailure(validation.ValidationErrors!);
        }

        var subject = await db.Subjects.Include(x => x.Translations).SingleOrDefaultAsync(x => x.Id == id, ct);
        if (subject is null)
        {
            return Result<SubjectResponse>.Failure(AcademicCatalogErrors.SubjectNotFound);
        }

        var code = Subject.NormalizeCode(request.Code);
        if (await db.Subjects.AnyAsync(x => x.Id != id && x.Code == code, ct))
        {
            return Result<SubjectResponse>.Failure(AcademicCatalogErrors.DuplicateSubjectCode);
        }

        Validation.TryParseStatus(request.Status, out var status);
        subject.Update(request.Code, request.IconUrl ?? subject.IconUrl, request.DisplayOrder, status, clock.UtcNow);
        subject.ReplaceTranslations(ToSubjectTranslations(id, request.Translations));
        await db.SaveChangesAsync(ct);
        return Result<SubjectResponse>.Success(ToSubjectResponse(subject, language));
    }

    public async Task<Result> ArchiveSubjectAsync(Guid id, CancellationToken ct)
    {
        var subject = await db.Subjects.SingleOrDefaultAsync(x => x.Id == id, ct);
        if (subject is null)
        {
            return Result.Failure(AcademicCatalogErrors.SubjectNotFound);
        }

        subject.Archive(clock.UtcNow, "admin_archive");
        await db.SaveChangesAsync(ct);
        return Result.Success();
    }

    public async Task<Result> DeleteSubjectAsync(Guid id, CancellationToken ct)
    {
        var subject = await db.Subjects.Include(x => x.Topics).SingleOrDefaultAsync(x => x.Id == id, ct);
        if (subject is null)
        {
            return Result.Failure(AcademicCatalogErrors.SubjectNotFound);
        }

        if (subject.Status != AcademicItemStatus.Draft || subject.Topics.Count > 0)
        {
            subject.Archive(clock.UtcNow, "delete_requested_but_in_use");
        }
        else
        {
            db.Subjects.Remove(subject);
        }

        await db.SaveChangesAsync(ct);
        return Result.Success();
    }

    public async Task<Result<PagedResponse<TopicResponse>>> GetTopicsAsync(Guid? subjectId, AcademicListQuery query, SupportedLanguage language, bool admin, CancellationToken ct)
    {
        var page = Math.Max(1, query.Page);
        var pageSize = Math.Clamp(query.PageSize, 1, 100);
        var topics = db.Topics.Include(x => x.Translations).AsNoTracking();
        if (subjectId.HasValue)
        {
            topics = topics.Where(x => x.SubjectId == subjectId.Value);
        }

        if (!admin)
        {
            topics = topics.Where(x => x.Status == AcademicItemStatus.Active);
        }

        if (Validation.TryParseStatus(query.Status, out var status) && query.Status.HasValue)
        {
            topics = topics.Where(x => x.Status == status);
        }

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var search = query.Search.Trim().ToLower();
            topics = topics.Where(x => x.Code.ToLower().Contains(search) || x.Translations.Any(t => t.Name.ToLower().Contains(search)));
        }

        topics = topics.OrderBy(x => x.DisplayOrder).ThenBy(x => x.Code);
        var total = await topics.CountAsync(ct);
        var items = await topics.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(ct);
        return Result<PagedResponse<TopicResponse>>.Success(new(items.Select(x => ToTopicResponse(x, language)).ToList(), page, pageSize, total));
    }

    public async Task<Result<TopicResponse>> CreateTopicAsync(TopicUpsertRequest request, SupportedLanguage language, CancellationToken ct)
    {
        var validation = Validation.ValidateTopic(request);
        if (validation.IsFailure)
        {
            return Result<TopicResponse>.ValidationFailure(validation.ValidationErrors!);
        }

        if (!await db.Subjects.AnyAsync(x => x.Id == request.SubjectId, ct))
        {
            return Result<TopicResponse>.Failure(AcademicCatalogErrors.SubjectNotFound);
        }

        var code = Subject.NormalizeCode(request.Code);
        if (await db.Topics.AnyAsync(x => x.SubjectId == request.SubjectId && x.Code == code, ct))
        {
            return Result<TopicResponse>.Failure(AcademicCatalogErrors.DuplicateTopicCode);
        }

        var now = clock.UtcNow;
        Validation.TryParseStatus(request.Status, out var status);
        var topic = new Topic(Guid.NewGuid(), request.SubjectId, request.Code, request.DisplayOrder, now);
        topic.Update(request.Code, request.DisplayOrder, status, now);
        topic.ReplaceTranslations(ToTopicTranslations(topic.Id, request.Translations));
        db.Topics.Add(topic);
        await db.SaveChangesAsync(ct);
        return Result<TopicResponse>.Success(ToTopicResponse(topic, language));
    }

    public async Task<Result<TopicResponse>> UpdateTopicAsync(Guid id, TopicUpsertRequest request, SupportedLanguage language, CancellationToken ct)
    {
        var validation = Validation.ValidateTopic(request);
        if (validation.IsFailure)
        {
            return Result<TopicResponse>.ValidationFailure(validation.ValidationErrors!);
        }

        var topic = await db.Topics.Include(x => x.Translations).SingleOrDefaultAsync(x => x.Id == id, ct);
        if (topic is null)
        {
            return Result<TopicResponse>.Failure(AcademicCatalogErrors.TopicNotFound);
        }

        if (!await db.Subjects.AnyAsync(x => x.Id == request.SubjectId, ct))
        {
            return Result<TopicResponse>.Failure(AcademicCatalogErrors.SubjectNotFound);
        }

        var code = Subject.NormalizeCode(request.Code);
        if (await db.Topics.AnyAsync(x => x.Id != id && x.SubjectId == request.SubjectId && x.Code == code, ct))
        {
            return Result<TopicResponse>.Failure(AcademicCatalogErrors.DuplicateTopicCode);
        }

        Validation.TryParseStatus(request.Status, out var status);
        topic.Update(request.Code, request.DisplayOrder, status, clock.UtcNow);
        topic.ReplaceTranslations(ToTopicTranslations(id, request.Translations));
        await db.SaveChangesAsync(ct);
        return Result<TopicResponse>.Success(ToTopicResponse(topic, language));
    }

    public async Task<Result> ArchiveTopicAsync(Guid id, CancellationToken ct)
    {
        var topic = await db.Topics.SingleOrDefaultAsync(x => x.Id == id, ct);
        if (topic is null)
        {
            return Result.Failure(AcademicCatalogErrors.TopicNotFound);
        }

        topic.Archive(clock.UtcNow, "admin_archive");
        await db.SaveChangesAsync(ct);
        return Result.Success();
    }

    public async Task<Result> DeleteTopicAsync(Guid id, CancellationToken ct)
    {
        var topic = await db.Topics.SingleOrDefaultAsync(x => x.Id == id, ct);
        if (topic is null)
        {
            return Result.Failure(AcademicCatalogErrors.TopicNotFound);
        }

        if (topic.Status != AcademicItemStatus.Draft)
        {
            topic.Archive(clock.UtcNow, "delete_requested_but_in_use");
        }
        else
        {
            db.Topics.Remove(topic);
        }

        await db.SaveChangesAsync(ct);
        return Result.Success();
    }

    private static IReadOnlyList<SubjectTranslation> ToSubjectTranslations(Guid subjectId, IEnumerable<TranslationRequest> translations) =>
        translations.Select(x =>
        {
            Validation.TryParseLanguage(x.Language, out var language);
            return new SubjectTranslation(subjectId, language, x.Name, x.Description);
        }).ToList();

    private static IReadOnlyList<TopicTranslation> ToTopicTranslations(Guid topicId, IEnumerable<TranslationRequest> translations) =>
        translations.Select(x =>
        {
            Validation.TryParseLanguage(x.Language, out var language);
            return new TopicTranslation(topicId, language, x.Name, x.Description);
        }).ToList();

    private static SubjectResponse ToSubjectResponse(Subject subject, SupportedLanguage language) =>
        new(subject.Id, subject.Code, subject.NameFor(language), subject.IconUrl, subject.DisplayOrder, (int)subject.Status, subject.Translations.Select(ToTranslationResponse).ToList());

    private static TopicResponse ToTopicResponse(Topic topic, SupportedLanguage language) =>
        new(topic.Id, topic.SubjectId, topic.Code, topic.NameFor(language), topic.DisplayOrder, (int)topic.Status, topic.Translations.Select(ToTranslationResponse).ToList());

    private static TranslationResponse ToTranslationResponse(SubjectTranslation translation) =>
        new((int)translation.Language, translation.Name, translation.Description);

    private static TranslationResponse ToTranslationResponse(TopicTranslation translation) =>
        new((int)translation.Language, translation.Name, translation.Description);
}
