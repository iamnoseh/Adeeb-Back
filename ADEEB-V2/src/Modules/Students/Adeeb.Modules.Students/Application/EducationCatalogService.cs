using System.Security.Claims;
using System.Text.Json;
using Adeeb.Application.Abstractions.Time;
using Adeeb.Modules.Students.Contracts;
using Adeeb.Modules.Students.Domain.Education;
using Adeeb.Modules.Students.Domain.Students;
using Adeeb.Modules.Students.Infrastructure.Persistence;
using Adeeb.SharedKernel.Errors;
using Adeeb.SharedKernel.Results;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Logging;

namespace Adeeb.Modules.Students.Application;

public sealed class EducationCatalogService(
    StudentsDbContext db,
    IDateTimeProvider clock,
    ILogger<EducationCatalogService> logger)
{
    public async Task<Result<IReadOnlyList<RegionResponse>>> GetRegionsAsync(Guid? parentId, bool russian, CancellationToken ct)
    {
        if (parentId.HasValue && !await db.Regions.AsNoTracking().AnyAsync(x => x.Id == parentId && x.IsActive, ct))
        {
            return Result<IReadOnlyList<RegionResponse>>.Failure(EducationErrors.RegionNotFound);
        }

        var regions = await db.Regions.AsNoTracking()
            .Where(x => x.IsActive && x.ParentId == parentId)
            .OrderBy(x => x.SortOrder).ThenBy(x => x.NameRu)
            .ToListAsync(ct);
        return Result<IReadOnlyList<RegionResponse>>.Success(regions.Select(x => ToRegionResponse(x, russian)).ToArray());
    }

    public async Task<Result<RegionResponse>> CreateRegionAsync(CreateRegionRequest request, ClaimsPrincipal actor, bool russian, CancellationToken ct)
    {
        if (!TryRegionType(request.Type, out var type) || !ValidName(request.NameTg, Region.NameMaxLength) || !ValidName(request.NameRu, Region.NameMaxLength))
        {
            return Result<RegionResponse>.ValidationFailure(Invalid("region", "student.region.invalid", "Student.Region.Invalid"));
        }

        var parent = await ValidateParentAsync(request.ParentId, type, ct);
        if (parent.IsFailure) return Result<RegionResponse>.Failure(parent.Error!);
        var normalizedTg = EducationNormalization.Key(request.NameTg);
        var normalizedRu = EducationNormalization.Key(request.NameRu);
        if (await RegionNameExistsAsync(request.ParentId, type, normalizedRu, null, ct))
        {
            return Result<RegionResponse>.Failure(EducationErrors.SchoolDuplicate);
        }

        var now = clock.UtcNow;
        var id = Guid.NewGuid();
        var path = parent.Value is null ? new[] { id } : [.. parent.Value.PathIds, id];
        var region = new Region(id, request.ParentId, type, request.NameTg.Trim(), request.NameRu.Trim(), normalizedTg, normalizedRu,
            request.SortOrder, path, path.Length - 1, now);
        region.SetPaths(parent.Value is null ? region.NameTg : $"{parent.Value.FullPathTg} / {region.NameTg}",
            parent.Value is null ? region.NameRu : $"{parent.Value.FullPathRu} / {region.NameRu}", path, path.Length - 1, now);
        db.Regions.Add(region);
        WriteAudit(ActorId(actor), "student.region.created", "region", region.Id, null, null, new { region.ParentId, region.Type });
        await db.SaveChangesAsync(ct);
        return Result<RegionResponse>.Success(ToRegionResponse(region, russian));
    }

    public async Task<Result<RegionResponse>> UpdateRegionAsync(Guid id, UpdateRegionRequest request, ClaimsPrincipal actor, bool russian, CancellationToken ct)
    {
        var region = await db.Regions.SingleOrDefaultAsync(x => x.Id == id, ct);
        if (region is null) return Result<RegionResponse>.Failure(EducationErrors.RegionNotFound);
        if (region.Version != request.ExpectedVersion) return Result<RegionResponse>.Failure(EducationErrors.ProfileConflict);
        if (!ValidName(request.NameTg, Region.NameMaxLength) || !ValidName(request.NameRu, Region.NameMaxLength))
            return Result<RegionResponse>.ValidationFailure(Invalid("region", "student.region.invalid", "Student.Region.Invalid"));
        var normalizedTg = EducationNormalization.Key(request.NameTg);
        var normalizedRu = EducationNormalization.Key(request.NameRu);
        if (await RegionNameExistsAsync(region.ParentId, region.Type, normalizedRu, id, ct)) return Result<RegionResponse>.Failure(EducationErrors.SchoolDuplicate);

        var now = clock.UtcNow;
        region.Update(request.NameTg.Trim(), request.NameRu.Trim(), normalizedTg, normalizedRu, request.SortOrder, now);
        await RebuildRegionSubtreeAsync(region, now, ct);
        WriteAudit(ActorId(actor), "student.region.updated", "region", region.Id, null, null, new { region.ParentId, region.Type });
        await db.SaveChangesAsync(ct);
        return Result<RegionResponse>.Success(ToRegionResponse(region, russian));
    }

    public async Task<Result<RegionResponse>> MoveRegionAsync(Guid id, MoveRegionRequest request, ClaimsPrincipal actor, bool russian, CancellationToken ct)
    {
        var region = await db.Regions.SingleOrDefaultAsync(x => x.Id == id, ct);
        if (region is null) return Result<RegionResponse>.Failure(EducationErrors.RegionNotFound);
        if (region.Version != request.ExpectedVersion) return Result<RegionResponse>.Failure(EducationErrors.ProfileConflict);
        if (request.ParentId == id) return Result<RegionResponse>.Failure(EducationErrors.RegionHierarchyInvalid);
        var parent = await ValidateParentAsync(request.ParentId, region.Type, ct);
        if (parent.IsFailure) return Result<RegionResponse>.Failure(parent.Error!);
        if (parent.Value is not null && parent.Value.PathIds.Contains(id)) return Result<RegionResponse>.Failure(EducationErrors.RegionHierarchyInvalid);

        var now = clock.UtcNow;
        var path = parent.Value is null ? new[] { id } : [.. parent.Value.PathIds, id];
        region.Move(request.ParentId, path, path.Length - 1, now);
        await RebuildRegionSubtreeAsync(region, now, ct);
        WriteAudit(ActorId(actor), "student.region.moved", "region", region.Id, null, null, new { region.ParentId });
        await db.SaveChangesAsync(ct);
        return Result<RegionResponse>.Success(ToRegionResponse(region, russian));
    }

    public async Task<Result> SetRegionStatusAsync(Guid id, SetRegionStatusRequest request, ClaimsPrincipal actor, CancellationToken ct)
    {
        var region = await db.Regions.SingleOrDefaultAsync(x => x.Id == id, ct);
        if (region is null) return Result.Failure(EducationErrors.RegionNotFound);
        if (region.Version != request.ExpectedVersion) return Result.Failure(EducationErrors.ProfileConflict);
        if (!request.IsActive)
        {
            var hasActiveChildren = await db.Regions.AnyAsync(x => x.ParentId == id && x.IsActive, ct);
            var hasLiveSchools = await db.Schools.AnyAsync(x => x.RegionId == id &&
                (x.Status == SchoolStatus.Draft || x.Status == SchoolStatus.Verified || x.Status == SchoolStatus.Inactive), ct);
            if (hasActiveChildren || hasLiveSchools) return Result.Failure(EducationErrors.RegionInUse);
        }
        region.SetActive(request.IsActive, clock.UtcNow);
        WriteAudit(ActorId(actor), "student.region.status_changed", "region", id, null, null, new { request.IsActive });
        await db.SaveChangesAsync(ct);
        return Result.Success();
    }

    public async Task<Result<PagedResponse<SchoolResponse>>> SearchStudentSchoolsAsync(SchoolSearchQuery request, bool russian, CancellationToken ct) =>
        await SearchSchoolsAsync(request.RegionId, request.Query, SchoolStatus.Verified, null, request.Page, request.PageSize, russian, ct);

    public async Task<Result<PagedResponse<SchoolResponse>>> GetAdminSchoolsAsync(AdminSchoolFilter request, bool russian, CancellationToken ct)
    {
        if (request.Status.HasValue && !TrySchoolStatus(request.Status.Value, out _))
            return Result<PagedResponse<SchoolResponse>>.ValidationFailure(Invalid("status", "student.school.status.invalid", "Student.School.InvalidStatus"));
        if (request.Type.HasValue && !TrySchoolType(request.Type.Value, out _))
            return Result<PagedResponse<SchoolResponse>>.ValidationFailure(Invalid("type", "student.school.type.invalid", "Student.School.InvalidType"));
        return await SearchSchoolsAsync(request.RegionId, request.Search, null, request, request.Page, request.PageSize, russian, ct);
    }

    public async Task<Result<SchoolResponse>> CreateSchoolAsync(CreateSchoolRequest request, ClaimsPrincipal actor, bool russian, CancellationToken ct)
    {
        if (!TrySchoolType(request.Type, out var type) || !ValidName(request.NameRu, School.NameMaxLength) ||
            (request.NameTg is not null && !ValidOptional(request.NameTg, School.NameMaxLength)) ||
            (request.Number is <= 0) || !ValidOptional(request.AddressText, School.AddressTextMaxLength))
            return Result<SchoolResponse>.ValidationFailure(Invalid("school", "student.school.invalid", "Student.School.Invalid"));
        var region = await db.Regions.SingleOrDefaultAsync(x => x.Id == request.RegionId, ct);
        if (region is null) return Result<SchoolResponse>.Failure(EducationErrors.RegionNotFound);
        if (!region.IsActive) return Result<SchoolResponse>.Failure(EducationErrors.RegionInactive);
        var normalized = EducationNormalization.Key(request.NameRu);
        if (await SchoolExistsAsync(request.RegionId, request.Number, type, normalized, null, ct)) return Result<SchoolResponse>.Failure(EducationErrors.SchoolDuplicate);
        var now = clock.UtcNow;
        var school = new School(Guid.NewGuid(), request.RegionId, Trim(request.NameTg), request.NameRu.Trim(), Trim(request.ShortName),
            request.Number, type, normalized, EducationNormalization.SearchText(request.NameTg, request.NameRu, request.ShortName, request.Number),
            Trim(request.AddressText), ActorId(actor), now);
        db.Schools.Add(school);
        WriteAudit(ActorId(actor), "student.school.created", "school", school.Id, null, null, new { school.RegionId, school.Type, school.Number });
        await db.SaveChangesAsync(ct);
        return Result<SchoolResponse>.Success(ToSchoolResponse(school, region, russian));
    }

    public async Task<Result<SchoolResponse>> UpdateSchoolAsync(Guid id, UpdateSchoolRequest request, ClaimsPrincipal actor, bool russian, CancellationToken ct)
    {
        var school = await db.Schools.SingleOrDefaultAsync(x => x.Id == id, ct);
        if (school is null) return Result<SchoolResponse>.Failure(EducationErrors.SchoolNotFound);
        if (school.Version != request.ExpectedVersion) return Result<SchoolResponse>.Failure(EducationErrors.ProfileConflict);
        if (!TrySchoolType(request.Type, out var type) || !ValidName(request.NameRu, School.NameMaxLength) || request.Number is <= 0 ||
            (request.NameTg is not null && !ValidOptional(request.NameTg, School.NameMaxLength)) || !ValidOptional(request.AddressText, School.AddressTextMaxLength))
            return Result<SchoolResponse>.ValidationFailure(Invalid("school", "student.school.invalid", "Student.School.Invalid"));
        var region = await db.Regions.SingleOrDefaultAsync(x => x.Id == request.RegionId, ct);
        if (region is null) return Result<SchoolResponse>.Failure(EducationErrors.RegionNotFound);
        if (!region.IsActive) return Result<SchoolResponse>.Failure(EducationErrors.RegionInactive);
        var normalized = EducationNormalization.Key(request.NameRu);
        if (await SchoolExistsAsync(request.RegionId, request.Number, type, normalized, id, ct)) return Result<SchoolResponse>.Failure(EducationErrors.SchoolDuplicate);
        school.Update(request.RegionId, Trim(request.NameTg), request.NameRu.Trim(), Trim(request.ShortName), request.Number, type,
            normalized, EducationNormalization.SearchText(request.NameTg, request.NameRu, request.ShortName, request.Number), Trim(request.AddressText), ActorId(actor), clock.UtcNow);
        WriteAudit(ActorId(actor), "student.school.updated", "school", id, null, null, new { school.RegionId, school.Type, school.Number });
        await db.SaveChangesAsync(ct);
        return Result<SchoolResponse>.Success(ToSchoolResponse(school, region, russian));
    }

    public async Task<Result<SchoolResponse>> VerifySchoolAsync(Guid id, SetSchoolStatusRequest request, ClaimsPrincipal actor, bool russian, CancellationToken ct)
    {
        var school = await db.Schools.SingleOrDefaultAsync(x => x.Id == id, ct);
        if (school is null) return Result<SchoolResponse>.Failure(EducationErrors.SchoolNotFound);
        if (school.Version != request.ExpectedVersion) return Result<SchoolResponse>.Failure(EducationErrors.ProfileConflict);
        var region = await db.Regions.SingleOrDefaultAsync(x => x.Id == school.RegionId, ct);
        if (region is null || !region.IsActive) return Result<SchoolResponse>.Failure(EducationErrors.RegionInactive);
        school.Verify(ActorId(actor), clock.UtcNow);
        WriteAudit(ActorId(actor), "student.school.verified", "school", id, null, null, null);
        await db.SaveChangesAsync(ct);
        return Result<SchoolResponse>.Success(ToSchoolResponse(school, region, russian));
    }

    public async Task<Result> ArchiveSchoolAsync(Guid id, SetSchoolStatusRequest request, ClaimsPrincipal actor, CancellationToken ct) =>
        await ChangeSchoolStatusAsync(id, request, actor, SchoolStatus.Archived, ct);

    public async Task<Result> DeactivateSchoolAsync(Guid id, SetSchoolStatusRequest request, ClaimsPrincipal actor, CancellationToken ct) =>
        await ChangeSchoolStatusAsync(id, request, actor, SchoolStatus.Inactive, ct);

    public async Task<Result> MergeSchoolsAsync(Guid sourceId, MergeSchoolRequest request, ClaimsPrincipal actor, CancellationToken ct)
    {
        if (sourceId == request.TargetSchoolId) return Result.Failure(EducationErrors.SchoolMergeInvalid);
        IDbContextTransaction? transaction = db.Database.IsRelational() ? await db.Database.BeginTransactionAsync(ct) : null;
        await using (transaction)
        {
            var schools = await db.Schools.Where(x => x.Id == sourceId || x.Id == request.TargetSchoolId).ToDictionaryAsync(x => x.Id, ct);
            if (!schools.TryGetValue(sourceId, out var source) || !schools.TryGetValue(request.TargetSchoolId, out var target)) return Result.Failure(EducationErrors.SchoolNotFound);
            if (source.Version != request.ExpectedSourceVersion || target.Version != request.ExpectedTargetVersion) return Result.Failure(EducationErrors.ProfileConflict);
            if (source.Status is SchoolStatus.Archived or SchoolStatus.Merged || target.Status != SchoolStatus.Verified || target.MergedIntoSchoolId.HasValue)
                return Result.Failure(EducationErrors.SchoolMergeInvalid);
            var now = clock.UtcNow;
            source.MergeInto(target.Id, ActorId(actor), now);
            var profiles = await db.EducationProfiles.Where(x => x.SchoolId == sourceId).ToListAsync(ct);
            foreach (var profile in profiles) profile.ReplaceSchoolAfterCatalogMerge(target.Id, now);
            var suggestions = await db.SchoolSuggestions.Where(x => x.ApprovedSchoolId == sourceId).ToListAsync(ct);
            foreach (var suggestion in suggestions) suggestion.RelinkApprovedSchool(target.Id);
            WriteAudit(ActorId(actor), "student.school.merged", "school", sourceId, null, null, new { targetSchoolId = target.Id, request.Reason });
            await db.SaveChangesAsync(ct);
            if (transaction is not null) await transaction.CommitAsync(ct);
        }
        logger.LogInformation("student.school.merged source_school_id={SourceSchoolId} target_school_id={TargetSchoolId}", sourceId, request.TargetSchoolId);
        return Result.Success();
    }

    private async Task<Result<PagedResponse<SchoolResponse>>> SearchSchoolsAsync(Guid? regionId, string? search, SchoolStatus? forcedStatus,
        AdminSchoolFilter? adminFilter, int page, int pageSize, bool russian, CancellationToken ct)
    {
        if (page < 1 || pageSize < 1) return Result<PagedResponse<SchoolResponse>>.ValidationFailure(Invalid("page", "student.pagination.invalid", "Common.InvalidPagination"));
        pageSize = Math.Min(pageSize, 50);
        Region? selectedRegion = null;
        if (regionId.HasValue)
        {
            selectedRegion = await db.Regions.AsNoTracking().SingleOrDefaultAsync(x => x.Id == regionId, ct);
            if (selectedRegion is null) return Result<PagedResponse<SchoolResponse>>.Failure(EducationErrors.RegionNotFound);
            if (forcedStatus.HasValue && !selectedRegion.IsActive) return Result<PagedResponse<SchoolResponse>>.Failure(EducationErrors.RegionInactive);
        }
        var terms = EducationNormalization.ParseSearch(search);
        var useTrigram = db.Database.IsNpgsql() && terms.NormalizedQuery.Length >= 3;
        var query = db.Schools.AsNoTracking().Join(db.Regions.AsNoTracking(), school => school.RegionId, region => region.Id,
            (school, region) => new { school, region });
        if (selectedRegion is not null) query = query.Where(x => x.region.PathIds.Contains(selectedRegion.Id));
        if (forcedStatus.HasValue) query = query.Where(x => x.school.Status == forcedStatus.Value);
        if (adminFilter?.Status is int status) query = query.Where(x => (int)x.school.Status == status);
        if (adminFilter?.Type is int type) query = query.Where(x => (int)x.school.Type == type);
        if (!string.IsNullOrEmpty(terms.NormalizedQuery))
        {
            if (useTrigram)
            {
                query = query.Where(x =>
                    (terms.Number.HasValue && x.school.Number == terms.Number) ||
                    x.school.SearchText.Contains(terms.NormalizedQuery) ||
                    EF.Functions.TrigramsSimilarity(x.school.SearchText, terms.NormalizedQuery) >= 0.22f);
            }
            else
            {
                query = query.Where(x =>
                    (terms.Number.HasValue && x.school.Number == terms.Number) ||
                    x.school.SearchText.Contains(terms.NormalizedQuery));
            }
        }
        if (terms.TypeHint == "lyceum") query = query.Where(x => x.school.Type == SchoolType.Lyceum);
        if (terms.TypeHint == "gymnasium") query = query.Where(x => x.school.Type == SchoolType.Gymnasium);
        var total = await query.CountAsync(ct);
        var ordered = query
            .OrderByDescending(x => terms.Number.HasValue && x.school.Number == terms.Number)
            .ThenByDescending(x => !string.IsNullOrEmpty(terms.NormalizedQuery) && x.school.NormalizedName == terms.NormalizedQuery);
        var items = useTrigram
            ? await ordered.ThenByDescending(x => EF.Functions.TrigramsSimilarity(x.school.SearchText, terms.NormalizedQuery))
                .ThenBy(x => x.school.Number ?? int.MaxValue).ThenBy(x => x.school.NormalizedName).ThenBy(x => x.school.Id)
                .Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(ct)
            : await ordered.ThenBy(x => x.school.Number ?? int.MaxValue).ThenBy(x => x.school.NormalizedName).ThenBy(x => x.school.Id)
                .Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(ct);
        return Result<PagedResponse<SchoolResponse>>.Success(new(items.Select(x => ToSchoolResponse(x.school, x.region, russian)).ToArray(), page, pageSize, total));
    }

    private async Task<Result<Region?>> ValidateParentAsync(Guid? parentId, RegionType childType, CancellationToken ct)
    {
        if (parentId is null) return childType == RegionType.Country
            ? Result<Region?>.Success(null)
            : Result<Region?>.Failure(EducationErrors.RegionHierarchyInvalid);
        var parent = await db.Regions.SingleOrDefaultAsync(x => x.Id == parentId, ct);
        if (parent is null) return Result<Region?>.Failure(EducationErrors.RegionNotFound);
        if (!parent.IsActive) return Result<Region?>.Failure(EducationErrors.RegionInactive);
        return CanContain(parent.Type, childType)
            ? Result<Region?>.Success(parent)
            : Result<Region?>.Failure(EducationErrors.RegionHierarchyInvalid);
    }

    private async Task RebuildRegionSubtreeAsync(Region root, DateTimeOffset now, CancellationToken ct)
    {
        var all = await db.Regions.OrderBy(x => x.Depth).ToListAsync(ct);
        var byId = all.ToDictionary(x => x.Id);
        foreach (var region in all.Where(x => x.Id == root.Id || x.PathIds.Contains(root.Id)).OrderBy(x => x.Depth))
        {
            Region? parent = region.ParentId.HasValue ? byId.GetValueOrDefault(region.ParentId.Value) : null;
            var path = parent is null ? new[] { region.Id } : [.. parent.PathIds, region.Id];
            var tg = parent is null ? region.NameTg : $"{parent.FullPathTg} / {region.NameTg}";
            var ru = parent is null ? region.NameRu : $"{parent.FullPathRu} / {region.NameRu}";
            region.SetPaths(tg, ru, path, path.Length - 1, now);
        }
    }

    private async Task<bool> RegionNameExistsAsync(Guid? parentId, RegionType type, string normalizedRu, Guid? exceptId, CancellationToken ct) =>
        await db.Regions.AnyAsync(x => x.ParentId == parentId && x.Type == type && x.NormalizedNameRu == normalizedRu && x.IsActive && (!exceptId.HasValue || x.Id != exceptId), ct);

    private async Task<bool> SchoolExistsAsync(Guid regionId, int? number, SchoolType type, string normalizedName, Guid? exceptId, CancellationToken ct) =>
        await db.Schools.AnyAsync(x => x.RegionId == regionId && x.Type == type &&
            (x.Status == SchoolStatus.Draft || x.Status == SchoolStatus.Verified || x.Status == SchoolStatus.Inactive) &&
            (!exceptId.HasValue || x.Id != exceptId) && (number.HasValue ? x.Number == number : x.Number == null && x.NormalizedName == normalizedName), ct);

    private async Task<Result> ChangeSchoolStatusAsync(Guid id, SetSchoolStatusRequest request, ClaimsPrincipal actor, SchoolStatus newStatus, CancellationToken ct)
    {
        var school = await db.Schools.SingleOrDefaultAsync(x => x.Id == id, ct);
        if (school is null) return Result.Failure(EducationErrors.SchoolNotFound);
        if (school.Version != request.ExpectedVersion) return Result.Failure(EducationErrors.ProfileConflict);
        if (newStatus == SchoolStatus.Archived) school.Archive(ActorId(actor), clock.UtcNow);
        else school.SetInactive(ActorId(actor), clock.UtcNow);
        WriteAudit(ActorId(actor), newStatus == SchoolStatus.Archived ? "student.school.archived" : "student.school.deactivated", "school", id, null, null, null);
        await db.SaveChangesAsync(ct);
        return Result.Success();
    }

    private void WriteAudit(Guid? actorId, string action, string resourceType, Guid resourceId, Guid? studentId, object? oldValues, object? newValues)
    {
        db.EducationAuditLogs.Add(new StudentEducationAuditLog(Guid.NewGuid(), actorId, action, resourceType, resourceId.ToString(), studentId,
            Serialize(oldValues), Serialize(newValues), null, clock.UtcNow));
    }

    private static string? Serialize(object? value) => value is null ? null : JsonSerializer.Serialize(value);
    private static Guid? ActorId(ClaimsPrincipal principal) => Guid.TryParse(principal.FindFirstValue(ClaimTypes.NameIdentifier) ?? principal.FindFirstValue("sub"), out var id) ? id : null;
    private static string? Trim(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    private static bool ValidName(string? value, int max) => !string.IsNullOrWhiteSpace(value) && value.Trim().Length <= max;
    private static bool ValidOptional(string? value, int max) => string.IsNullOrWhiteSpace(value) || value.Trim().Length <= max;
    private static bool TryRegionType(int value, out RegionType type) { type = (RegionType)value; return Enum.IsDefined(type); }
    private static bool TrySchoolType(int value, out SchoolType type) { type = (SchoolType)value; return Enum.IsDefined(type); }
    private static bool TrySchoolStatus(int value, out SchoolStatus status) { status = (SchoolStatus)value; return Enum.IsDefined(status); }
    private static bool CanContain(RegionType parent, RegionType child) => parent switch
    {
        RegionType.Country => child is RegionType.Province or RegionType.City,
        RegionType.Province => child is RegionType.District or RegionType.City or RegionType.Town,
        RegionType.City => child is RegionType.District or RegionType.Town or RegionType.Neighborhood,
        RegionType.District => child is RegionType.Jamoat or RegionType.Town or RegionType.Village or RegionType.Neighborhood,
        RegionType.Jamoat => child is RegionType.Village or RegionType.Neighborhood,
        RegionType.Town => child == RegionType.Neighborhood,
        _ => false
    };
    private static RegionResponse ToRegionResponse(Region region, bool russian) => new(region.Id, region.ParentId, (int)region.Type,
        russian ? region.NameRu : region.NameTg, russian ? region.FullPathRu : region.FullPathTg, region.Depth, region.SortOrder, region.IsActive, region.Version);
    private static SchoolResponse ToSchoolResponse(School school, Region region, bool russian) => new(school.Id, school.RegionId,
        russian || string.IsNullOrWhiteSpace(school.NameTg) ? school.NameRu : school.NameTg!, school.NameTg, school.NameRu, school.ShortName,
        school.Number, (int)school.Type, school.Status.ToString(), russian ? region.FullPathRu : region.FullPathTg, school.AddressText, school.Version);
    private static Dictionary<string, IReadOnlyList<Error>> Invalid(string field, string code, string message) => new() { [field] = [Error.Validation(code, message)] };
}
