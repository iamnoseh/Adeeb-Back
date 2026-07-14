using System.Globalization;
using Adeeb.Application.Abstractions.Localization;
using Adeeb.Application.Abstractions.Time;
using Adeeb.Modules.Mmt.Contracts;
using Adeeb.Modules.Mmt.Domain;
using Adeeb.Modules.Mmt.Infrastructure.Persistence;
using Adeeb.SharedKernel.Results;
using Microsoft.EntityFrameworkCore;

namespace Adeeb.Modules.Mmt.Application;

public sealed class MmtCatalogService(MmtDbContext db, IDateTimeProvider clock)
{
    public async Task<Result<PagedResponse<MmtClusterDto>>> GetClustersAsync(MmtPageQuery query, CancellationToken ct)
    {
        var source = db.Clusters.AsNoTracking();
        if (query.IsActive.HasValue) source = source.Where(x => x.IsActive == query.IsActive);
        if (!string.IsNullOrWhiteSpace(query.Search)) { var s = query.Search.Trim().ToLower(); source = source.Where(x => x.Code.ToLower().Contains(s) || x.Name.ToLower().Contains(s) || x.NameRu.ToLower().Contains(s)); }
        return Result<PagedResponse<MmtClusterDto>>.Success(await PageAsync(source.OrderBy(x => x.Name), query.Page, query.PageSize, x => ToDto(x), ct));
    }
    public async Task<Result<MmtClusterDto>> GetClusterAsync(Guid id, CancellationToken ct) =>
        await db.Clusters.AsNoTracking().SingleOrDefaultAsync(x => x.Id == id, ct) is { } x ? Result<MmtClusterDto>.Success(ToDto(x)) : Result<MmtClusterDto>.Failure(MmtErrors.ClusterNotFound);
    public async Task<Result<MmtClusterDto>> CreateClusterAsync(CreateMmtClusterDto request, CancellationToken ct)
    {
        var validation = MmtValidation.ValidateCluster(request.Name, request.Code, request.Description); if (validation.IsFailure) return Invalid<MmtClusterDto>(validation);
        var code = MmtNormalization.Code(request.Code); if (await db.Clusters.AnyAsync(x => x.Code == code, ct)) return Result<MmtClusterDto>.Failure(MmtErrors.DuplicateCluster);
        var entity = new MmtCluster(Guid.NewGuid(), request.Name, code, request.Description, clock.UtcNow); db.Clusters.Add(entity);
        if (!await SaveAsync(MmtDatabaseConstraints.ClusterCode, ct)) return Result<MmtClusterDto>.Failure(MmtErrors.DuplicateCluster);
        return Result<MmtClusterDto>.Success(ToDto(entity));
    }
    public async Task<Result<MmtClusterDto>> UpdateClusterAsync(Guid id, UpdateMmtClusterDto request, CancellationToken ct)
    {
        var validation = MmtValidation.ValidateCluster(request.Name, request.Code, request.Description); if (validation.IsFailure) return Invalid<MmtClusterDto>(validation);
        var entity = await db.Clusters.SingleOrDefaultAsync(x => x.Id == id, ct); if (entity is null) return Result<MmtClusterDto>.Failure(MmtErrors.ClusterNotFound);
        var code = MmtNormalization.Code(request.Code); if (await db.Clusters.AnyAsync(x => x.Id != id && x.Code == code, ct)) return Result<MmtClusterDto>.Failure(MmtErrors.DuplicateCluster);
        entity.UpdateTranslation(CurrentLanguage, request.Name, request.Description, code, request.IsActive, clock.UtcNow);
        if (!await SaveAsync(MmtDatabaseConstraints.ClusterCode, ct)) return Result<MmtClusterDto>.Failure(MmtErrors.DuplicateCluster);
        return Result<MmtClusterDto>.Success(ToDto(entity));
    }
    public Task<Result> SetClusterStatusAsync(Guid id, bool active, CancellationToken ct) => SetStatusAsync(db.Clusters, id, active, (x, a) => x.SetActive(a, clock.UtcNow), MmtErrors.ClusterNotFound, ct);

    public async Task<Result<PagedResponse<UniversityDto>>> GetUniversitiesAsync(MmtPageQuery query, CancellationToken ct)
    {
        var source = db.Universities.AsNoTracking();
        if (query.IsActive.HasValue) source = source.Where(x => x.IsActive == query.IsActive);
        if (!string.IsNullOrWhiteSpace(query.Search)) { var s = query.Search.Trim().ToLower(); source = source.Where(x => x.FullName.ToLower().Contains(s) || x.FullNameRu.ToLower().Contains(s) || (x.ShortName != null && x.ShortName.ToLower().Contains(s)) || (x.ShortNameRu != null && x.ShortNameRu.ToLower().Contains(s)) || x.City.ToLower().Contains(s) || x.CityRu.ToLower().Contains(s)); }
        return Result<PagedResponse<UniversityDto>>.Success(await PageAsync(source.OrderBy(x => x.FullName), query.Page, query.PageSize, ToDto, ct));
    }
    public async Task<Result<UniversityDto>> GetUniversityAsync(Guid id, CancellationToken ct) =>
        await db.Universities.AsNoTracking().SingleOrDefaultAsync(x => x.Id == id, ct) is { } x ? Result<UniversityDto>.Success(ToDto(x)) : Result<UniversityDto>.Failure(MmtErrors.UniversityNotFound);
    public async Task<Result<UniversityDto>> CreateUniversityAsync(CreateUniversityDto r, CancellationToken ct)
    {
        var validation = MmtValidation.ValidateUniversity(r.FullName, r.ShortName, r.City, r.Type, r.LogoUrl); if (validation.IsFailure) return Invalid<UniversityDto>(validation);
        var key = MmtNormalization.NameKey(r.FullName); if (await db.Universities.AnyAsync(x => x.NormalizedFullName == key, ct)) return Result<UniversityDto>.Failure(MmtErrors.DuplicateUniversity);
        var entity = new University(Guid.NewGuid(), r.FullName, r.ShortName, r.City, (UniversityType)r.Type, r.LogoUrl, clock.UtcNow); db.Universities.Add(entity);
        if (!await SaveAsync(MmtDatabaseConstraints.UniversityName, ct)) return Result<UniversityDto>.Failure(MmtErrors.DuplicateUniversity);
        return Result<UniversityDto>.Success(ToDto(entity));
    }
    public async Task<Result<UniversityDto>> UpdateUniversityAsync(Guid id, UpdateUniversityDto r, CancellationToken ct)
    {
        var validation = MmtValidation.ValidateUniversity(r.FullName, r.ShortName, r.City, r.Type, r.LogoUrl); if (validation.IsFailure) return Invalid<UniversityDto>(validation);
        var entity = await db.Universities.SingleOrDefaultAsync(x => x.Id == id, ct); if (entity is null) return Result<UniversityDto>.Failure(MmtErrors.UniversityNotFound);
        var key = MmtNormalization.NameKey(r.FullName); if (CurrentLanguage != SupportedLanguage.Russian && await db.Universities.AnyAsync(x => x.Id != id && x.NormalizedFullName == key, ct)) return Result<UniversityDto>.Failure(MmtErrors.DuplicateUniversity);
        entity.UpdateTranslation(CurrentLanguage, r.FullName, r.ShortName, r.City, (UniversityType)r.Type, r.LogoUrl, r.IsActive, clock.UtcNow);
        if (!await SaveAsync(MmtDatabaseConstraints.UniversityName, ct)) return Result<UniversityDto>.Failure(MmtErrors.DuplicateUniversity);
        return Result<UniversityDto>.Success(ToDto(entity));
    }
    public Task<Result> SetUniversityStatusAsync(Guid id, bool active, CancellationToken ct) => SetStatusAsync(db.Universities, id, active, (x, a) => x.SetActive(a, clock.UtcNow), MmtErrors.UniversityNotFound, ct);

    public async Task<Result<PagedResponse<SpecialtyDto>>> GetSpecialtiesAsync(MmtPageQuery query, CancellationToken ct)
    {
        var source = db.Specialties.AsNoTracking();
        if (query.IsActive.HasValue) source = source.Where(x => x.IsActive == query.IsActive);
        if (!string.IsNullOrWhiteSpace(query.Search)) { var s = query.Search.Trim().ToLower(); source = source.Where(x => x.Code.ToLower().Contains(s) || x.Name.ToLower().Contains(s) || x.NameRu.ToLower().Contains(s)); }
        return Result<PagedResponse<SpecialtyDto>>.Success(await PageAsync(source.OrderBy(x => x.Code), query.Page, query.PageSize, ToDto, ct));
    }
    public async Task<Result<SpecialtyDto>> GetSpecialtyAsync(Guid id, CancellationToken ct) =>
        await db.Specialties.AsNoTracking().SingleOrDefaultAsync(x => x.Id == id, ct) is { } x ? Result<SpecialtyDto>.Success(ToDto(x)) : Result<SpecialtyDto>.Failure(MmtErrors.SpecialtyNotFound);
    public async Task<Result<SpecialtyDto>> CreateSpecialtyAsync(CreateSpecialtyDto r, CancellationToken ct)
    {
        var validation = MmtValidation.ValidateSpecialty(r.Code, r.Name, r.Description); if (validation.IsFailure) return Invalid<SpecialtyDto>(validation);
        var code = MmtNormalization.Code(r.Code); if (await db.Specialties.AnyAsync(x => x.Code == code, ct)) return Result<SpecialtyDto>.Failure(MmtErrors.DuplicateSpecialty);
        var entity = new Specialty(Guid.NewGuid(), code, r.Name, r.Description, clock.UtcNow); db.Specialties.Add(entity);
        if (!await SaveAsync(MmtDatabaseConstraints.SpecialtyCode, ct)) return Result<SpecialtyDto>.Failure(MmtErrors.DuplicateSpecialty);
        return Result<SpecialtyDto>.Success(ToDto(entity));
    }
    public async Task<Result<SpecialtyDto>> UpdateSpecialtyAsync(Guid id, UpdateSpecialtyDto r, CancellationToken ct)
    {
        var validation = MmtValidation.ValidateSpecialty(r.Code, r.Name, r.Description); if (validation.IsFailure) return Invalid<SpecialtyDto>(validation);
        var entity = await db.Specialties.SingleOrDefaultAsync(x => x.Id == id, ct); if (entity is null) return Result<SpecialtyDto>.Failure(MmtErrors.SpecialtyNotFound);
        var code = MmtNormalization.Code(r.Code); if (await db.Specialties.AnyAsync(x => x.Id != id && x.Code == code, ct)) return Result<SpecialtyDto>.Failure(MmtErrors.DuplicateSpecialty);
        entity.UpdateTranslation(CurrentLanguage, code, r.Name, r.Description, r.IsActive, clock.UtcNow);
        if (!await SaveAsync(MmtDatabaseConstraints.SpecialtyCode, ct)) return Result<SpecialtyDto>.Failure(MmtErrors.DuplicateSpecialty);
        return Result<SpecialtyDto>.Success(ToDto(entity));
    }
    public Task<Result> SetSpecialtyStatusAsync(Guid id, bool active, CancellationToken ct) => SetStatusAsync(db.Specialties, id, active, (x, a) => x.SetActive(a, clock.UtcNow), MmtErrors.SpecialtyNotFound, ct);

    private async Task<bool> SaveAsync(string uniqueConstraint, CancellationToken ct)
    { try { await db.SaveChangesAsync(ct); return true; } catch (DbUpdateException ex) when (MmtDatabaseConstraints.IsUniqueViolation(ex, uniqueConstraint)) { db.ChangeTracker.Clear(); return false; } }
    private async Task<Result> SetStatusAsync<T>(DbSet<T> set, Guid id, bool active, Action<T, bool> update, Adeeb.SharedKernel.Errors.Error notFound, CancellationToken ct) where T : class
    { var entity = await set.FindAsync([id], ct); if (entity is null) return Result.Failure(notFound); update(entity, active); await db.SaveChangesAsync(ct); return Result.Success(); }
    private static Result<T> Invalid<T>(Result validation) => Result<T>.ValidationFailure(validation.ValidationErrors!);
    private static async Task<PagedResponse<TDto>> PageAsync<TEntity, TDto>(IQueryable<TEntity> query, int page, int pageSize, Func<TEntity, TDto> map, CancellationToken ct)
    { page = Math.Max(1, page); pageSize = Math.Clamp(pageSize, 1, 100); var total = await query.CountAsync(ct); var items = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(ct); return new(items.Select(map).ToList(), page, pageSize, total); }
    internal static MmtClusterDto ToDto(MmtCluster x) => new(x.Id, x.NameFor(CurrentLanguage), x.Code, x.DescriptionFor(CurrentLanguage), x.IsActive, x.CreatedAtUtc, x.UpdatedAtUtc);
    internal static UniversityDto ToDto(University x) => new(x.Id, x.FullNameFor(CurrentLanguage), x.ShortNameFor(CurrentLanguage), x.CityFor(CurrentLanguage), (int)x.Type, x.LogoUrl, x.IsActive, x.CreatedAtUtc, x.UpdatedAtUtc);
    internal static SpecialtyDto ToDto(Specialty x) => new(x.Id, x.Code, x.NameFor(CurrentLanguage), x.DescriptionFor(CurrentLanguage), x.IsActive, x.CreatedAtUtc, x.UpdatedAtUtc);
    internal static SupportedLanguage CurrentLanguage => SupportedLanguageExtensions.TryParseCulture(CultureInfo.CurrentUICulture.Name, out var language) ? language : SupportedLanguage.Tajik;
}
