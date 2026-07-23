namespace Adeeb.Modules.Students.Contracts;

using Microsoft.AspNetCore.Http;

public sealed record PagedResponse<T>(IReadOnlyList<T> Items, int Page, int PageSize, int TotalCount);

public sealed record RegionResponse(
    Guid Id,
    Guid? ParentId,
    int Type,
    string Name,
    string FullPath,
    int Depth,
    int SortOrder,
    bool IsActive,
    uint Version);

public sealed record SchoolResponse(
    Guid Id,
    Guid RegionId,
    string Name,
    string? NameTg,
    string NameRu,
    string? ShortName,
    int? Number,
    int Type,
    string Status,
    string RegionPath,
    string? AddressText,
    uint Version);

public sealed record StudentEducationProfileResponse(
    Guid StudentId,
    RegionResponse? ResidenceRegion,
    SchoolResponse? School,
    Guid? PendingSchoolSuggestionId,
    short? CurrentGrade,
    int? AcademicYearStart,
    int? AcademicYearEnd,
    int? ExpectedGraduationYear,
    string Status,
    string? AddressText,
    uint Version,
    DateTimeOffset UpdatedAtUtc);

public sealed record UpsertStudentEducationProfileRequest(
    Guid ResidenceRegionId,
    Guid SchoolId,
    short CurrentGrade,
    string? AddressText,
    uint? ExpectedVersion);

public sealed record SchoolSearchQuery(Guid RegionId, string? Query, int Page = 1, int PageSize = 10);

public sealed record CreateSchoolSuggestionRequest(
    Guid ResidenceRegionId,
    string SuggestedName,
    int? SuggestedNumber,
    short CurrentGrade,
    string? AddressText,
    uint? ExpectedProfileVersion);

public sealed record SchoolSuggestionResponse(
    Guid Id,
    string SuggestedName,
    int? SuggestedNumber,
    RegionResponse Region,
    string Status,
    Guid? ApprovedSchoolId,
    string? RejectionReason,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset? ReviewedAtUtc,
    uint Version);

public sealed record CreateRegionRequest(
    Guid? ParentId,
    int Type,
    string NameTg,
    string NameRu,
    int SortOrder);

public sealed record UpdateRegionRequest(
    string NameTg,
    string NameRu,
    int SortOrder,
    uint ExpectedVersion);

public sealed record MoveRegionRequest(Guid? ParentId, uint ExpectedVersion);
public sealed record SetRegionStatusRequest(bool IsActive, uint ExpectedVersion);

public sealed record CreateSchoolRequest(
    Guid RegionId,
    string? NameTg,
    string NameRu,
    string? ShortName,
    int? Number,
    int Type,
    string? AddressText);

public sealed record UpdateSchoolRequest(
    Guid RegionId,
    string? NameTg,
    string NameRu,
    string? ShortName,
    int? Number,
    int Type,
    string? AddressText,
    uint ExpectedVersion);

public sealed record SetSchoolStatusRequest(uint ExpectedVersion);
public sealed record MergeSchoolRequest(Guid TargetSchoolId, uint ExpectedSourceVersion, uint ExpectedTargetVersion, string? Reason);

public sealed record AdminSchoolFilter(
    Guid? RegionId = null,
    int? Status = null,
    int? Type = null,
    string? Search = null,
    int Page = 1,
    int PageSize = 10);

public sealed record ReviewSchoolSuggestionRequest(
    Guid? ExistingSchoolId,
    CreateSchoolRequest? NewSchool,
    bool VerifyNewSchool,
    string? RejectionReason,
    uint ExpectedVersion);

public sealed record AdminSchoolSuggestionFilter(
    int? Status = null,
    Guid? RegionId = null,
    string? Search = null,
    int Page = 1,
    int PageSize = 10);

public sealed record AdminCorrectEducationProfileRequest(
    Guid ResidenceRegionId,
    Guid SchoolId,
    short CurrentGrade,
    string Reason,
    uint? ExpectedVersion);

public sealed record AcademicYearRolloverResponse(
    Guid Id,
    int AcademicYearStart,
    int AcademicYearEnd,
    string Status,
    int PromotedCount,
    int GraduatedCount,
    int SkippedCount,
    int ConflictCount,
    DateTimeOffset PreviewCreatedAtUtc,
    DateTimeOffset? ExecutedAtUtc,
    uint Version);

public sealed record CreateAcademicYearRolloverPreviewRequest(int AcademicYearStart, string IdempotencyKey);
public sealed record ExecuteAcademicYearRolloverRequest(uint ExpectedVersion);

public sealed class EducationSchoolImportRequest
{
    public IFormFile? File { get; init; }
    public bool VerifyImportedSchools { get; init; }
}

public sealed record EducationSchoolImportRowResponse(int RowNumber, string? RegionPathRu, string? SchoolNameRu,
    int? SchoolNumber, bool IsValid, bool IsDuplicate, IReadOnlyList<string> Errors);

public sealed record EducationSchoolImportPreviewResponse(int TotalRows, int ValidRows, int InvalidRows, int DuplicateRows,
    IReadOnlyList<EducationSchoolImportRowResponse> Rows);

public sealed record EducationSchoolImportResultResponse(Guid BatchId, int ImportedRegions, int ImportedSchools, int SkippedSchools,
    int InvalidRows);
