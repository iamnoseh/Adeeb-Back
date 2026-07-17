using Microsoft.AspNetCore.Http;

namespace Adeeb.Modules.Mmt.Contracts;

public sealed record MmtPageQuery(string? Search = null, bool? IsActive = null, int Page = 1, int PageSize = 10);
public sealed record PagedResponse<T>(IReadOnlyList<T> Items, int Page, int PageSize, int TotalCount);
public sealed record StatusRequest(bool IsActive);
public sealed record PublishRequest(bool IsPublished);

public sealed record CreateMmtClusterDto(string Name, string Code, string? Description,
    string? NameTg = null, string? NameRu = null, string? DescriptionTg = null, string? DescriptionRu = null,
    IReadOnlyList<Guid>? SubjectIds = null);
public sealed record UpdateMmtClusterDto(string Name, string Code, string? Description, bool IsActive,
    string? NameTg = null, string? NameRu = null, string? DescriptionTg = null, string? DescriptionRu = null,
    IReadOnlyList<Guid>? SubjectIds = null);
public sealed record MmtClusterSubjectDto(Guid Id, string Code, string Name);
public sealed record MmtClusterDto(Guid Id, string Name, string Code, string? Description, bool IsActive,
    DateTimeOffset CreatedAtUtc, DateTimeOffset UpdatedAtUtc, string NameTg = "", string NameRu = "",
    string? DescriptionTg = null, string? DescriptionRu = null, IReadOnlyList<MmtClusterSubjectDto>? Subjects = null);
public sealed record CreateUniversityDto(string FullName, string? ShortName, string City, int Type, string? LogoUrl,
    string? FullNameTg = null, string? FullNameRu = null, string? ShortNameTg = null, string? ShortNameRu = null,
    string? CityTg = null, string? CityRu = null);
public sealed record UpdateUniversityDto(string FullName, string? ShortName, string City, int Type, string? LogoUrl, bool IsActive,
    string? FullNameTg = null, string? FullNameRu = null, string? ShortNameTg = null, string? ShortNameRu = null,
    string? CityTg = null, string? CityRu = null);
public sealed record UniversityDto(Guid Id, string FullName, string? ShortName, string City, int Type, string? LogoUrl,
    bool IsActive, DateTimeOffset CreatedAtUtc, DateTimeOffset UpdatedAtUtc, string FullNameTg = "", string FullNameRu = "",
    string? ShortNameTg = null, string? ShortNameRu = null, string CityTg = "", string CityRu = "", bool NeedsTranslation = false);
public sealed record CreateSpecialtyDto(string Code, string Name, string? Description,
    string? NameTg = null, string? NameRu = null, string? DescriptionTg = null, string? DescriptionRu = null);
public sealed record UpdateSpecialtyDto(string Code, string Name, string? Description, bool IsActive,
    string? NameTg = null, string? NameRu = null, string? DescriptionTg = null, string? DescriptionRu = null);
public sealed record SpecialtyDto(Guid Id, string Code, string Name, string? Description, bool IsActive,
    DateTimeOffset CreatedAtUtc, DateTimeOffset UpdatedAtUtc, string NameTg = "", string NameRu = "",
    string? DescriptionTg = null, string? DescriptionRu = null, bool NeedsTranslation = false);

public sealed record CreateAdmissionProgramDto(Guid UniversityId, Guid SpecialtyId, Guid MmtClusterId, int AdmissionType,
    int StudyForm, int StudyLanguage, int AdmissionYear, int? SeatsCount, bool IsPublished = false,
    string? StudyLocationTg = null, string? StudyLocationRu = null, decimal? TuitionFeeTjs = null);
public sealed record UpdateAdmissionProgramDto(Guid UniversityId, Guid SpecialtyId, Guid MmtClusterId, int AdmissionType,
    int StudyForm, int StudyLanguage, int AdmissionYear, int? SeatsCount, bool IsPublished, bool IsActive,
    string? StudyLocationTg = null, string? StudyLocationRu = null, decimal? TuitionFeeTjs = null);
public sealed record AdmissionProgramFilter(Guid? ClusterId = null, Guid? UniversityId = null, Guid? SpecialtyId = null,
    int? AdmissionType = null, int? StudyForm = null, int? StudyLanguage = null, int? AdmissionYear = null,
    bool? IsPublished = null, bool? IsActive = null, string? Search = null, int Page = 1, int PageSize = 10);
public sealed record StudentSpecialtyLookupQuery(Guid ClusterId, string? Search = null, int Page = 1, int PageSize = 10);
public sealed record StudentUniversityLookupQuery(Guid ClusterId, Guid? SpecialtyId = null, string? Search = null, int Page = 1, int PageSize = 10);
public sealed record AdmissionProgramListItemDto(Guid Id, Guid UniversityId, string UniversityName, Guid SpecialtyId,
    string SpecialtyCode, string SpecialtyName, Guid MmtClusterId, string ClusterCode, string ClusterName,
    int AdmissionType, int StudyForm, int StudyLanguage, int AdmissionYear, int? SeatsCount, bool IsPublished,
    bool IsActive, decimal? LatestPassingScore, string StudyLocation = "", string StudyLocationTg = "",
    string StudyLocationRu = "", decimal? TuitionFeeTjs = null, bool NeedsTranslation = false);
public sealed record AdmissionProgramDto(Guid Id, UniversityDto University, SpecialtyDto Specialty, MmtClusterDto Cluster,
    int AdmissionType, int StudyForm, int StudyLanguage, int AdmissionYear, int? SeatsCount, bool IsPublished,
    bool IsActive, decimal? LatestPassingScore, decimal? AveragePassingScoreLast3Years, decimal? ConservativeThreshold,
    DateTimeOffset CreatedAtUtc, DateTimeOffset UpdatedAtUtc, string StudyLocation = "", string StudyLocationTg = "",
    string StudyLocationRu = "", decimal? TuitionFeeTjs = null, bool NeedsTranslation = false);

public sealed record CreatePassingScoreHistoryDto(int Year, decimal PassingScore, int? SeatsCount, string? Source, string? Note,
    int DistributionRound = 0);
public sealed record UpdatePassingScoreHistoryDto(int Year, decimal PassingScore, int? SeatsCount, string? Source, string? Note,
    int DistributionRound = 0);
public sealed record PassingScoreHistoryDto(Guid Id, Guid AdmissionProgramId, int Year, decimal PassingScore,
    int? SeatsCount, string? Source, string? Note, DateTimeOffset CreatedAtUtc, DateTimeOffset UpdatedAtUtc,
    int DistributionRound = 0);
public sealed record PassingScoreAnalyticsDto(decimal? LatestPassingScore, decimal? AverageLast3Years, decimal? ConservativeThreshold);

public sealed class MmtImportPreviewRequestDto
{
    public IFormFile? File { get; init; }
    public bool CreateMissingReferences { get; init; }
    public int ExistingScoreMode { get; init; }
    public int? AdmissionYear { get; init; }
}
public sealed class MmtImportConfirmRequestDto
{
    public IFormFile? File { get; init; }
    public bool CreateMissingReferences { get; init; }
    public int ExistingScoreMode { get; init; }
    public bool PublishAdmissionPrograms { get; init; }
    public int? AdmissionYear { get; init; }
}
public sealed record MmtImportNormalizedRowDto(int Year, string ClusterCode, string ClusterName, string UniversityFullName,
    string? UniversityShortName, string UniversityCity, int UniversityType, string SpecialtyCode, string SpecialtyName,
    int AdmissionType, int StudyForm, int StudyLanguage, int? SeatsCount, decimal PassingScore, string? Source, string? Note,
    int DistributionRound = 0);
public sealed record MmtImportRowPreviewDto(int RowNumber, MmtImportNormalizedRowDto? Values, bool IsValid,
    bool IsDuplicate, IReadOnlyList<string> ValidationErrors);
public sealed record MmtImportPreviewResultDto(int TotalRows, int ValidRowsCount, int InvalidRowsCount,
    int DuplicateRowsCount, IReadOnlyList<MmtImportRowPreviewDto> Rows);
public sealed record MmtImportResultDto(int ProcessedRows, int ImportedPrograms, int InsertedScores, int UpdatedScores,
    int SkippedScores, int InvalidRows, IReadOnlyList<MmtImportRowPreviewDto> Rows);

public sealed class MmtCatalogImportRequestDto
{
    public IFormFile? File { get; init; }
    public Guid MmtClusterId { get; init; }
    public int AdmissionYear { get; init; }
    public int DefaultUniversityType { get; init; }
    public string? UniversityTypeOverridesJson { get; init; }
}
public sealed record MmtCatalogUniversityTypeOverrideDto(string UniversityNameRu, int UniversityType);
public sealed record MmtCatalogImportNormalizedRowDto(string SourceId, string SpecialtyCode, string SpecialtyNameRu,
    string UniversityNameRu, string StudyLocationRu, int StudyForm, int AdmissionType, int StudyLanguage,
    int SeatsCount, decimal? TuitionFeeTjs);
public sealed record MmtCatalogImportRowPreviewDto(int RowNumber, MmtCatalogImportNormalizedRowDto? Values,
    bool IsValid, bool IsExisting, bool NeedsTranslation, IReadOnlyList<string> ValidationErrors,
    IReadOnlyList<string> Warnings);
public sealed record MmtCatalogImportUniversityDto(string UniversityNameRu, int UniversityType, bool Exists);
public sealed record MmtCatalogImportPreviewResultDto(int TotalRows, int ValidRowsCount, int InvalidRowsCount,
    int NewRowsCount, int SkippedRowsCount, int NeedsTranslationCount,
    IReadOnlyList<MmtCatalogImportUniversityDto> Universities, IReadOnlyList<MmtCatalogImportRowPreviewDto> Rows);
public sealed record MmtCatalogImportResultDto(int ProcessedRows, int ImportedPrograms, int SkippedPrograms,
    int CreatedUniversities, int CreatedSpecialties, int InvalidRows);

public sealed record UpsertStudentMmtProfileDto(Guid MmtClusterId, int? AdmissionYear, Guid? GoalAdmissionProgramId);
public sealed record StudentMmtProfileDto(Guid Id, Guid UserId, MmtClusterDto Cluster, int AdmissionYear,
    Guid? GoalAdmissionProgramId, bool IsActive, int ChoicesCount, DateTimeOffset CreatedAtUtc, DateTimeOffset UpdatedAtUtc);
public sealed record AdmissionChoiceInputDto(Guid AdmissionProgramId, int PriorityOrder);
public sealed record UpsertAdmissionChoicesDto(IReadOnlyList<AdmissionChoiceInputDto> Choices);
public sealed record StudentAdmissionChoiceDto(Guid Id, int PriorityOrder, AdmissionProgramListItemDto AdmissionProgram,
    DateTimeOffset CreatedAtUtc, DateTimeOffset UpdatedAtUtc,
    IReadOnlyList<PassingScoreHistoryDto>? RecentPassingScores = null);
public sealed record SimulateMmtEvaluationDto(decimal TotalScore);
public sealed record MmtAdmissionChoiceSnapshotDto(Guid Id, int PriorityOrder, Guid AdmissionProgramId,
    string UniversityName, string SpecialtyCode, string SpecialtyName, string ClusterCode, int AdmissionType,
    int StudyForm, int StudyLanguage, int AdmissionYear, decimal? PassingScoreUsed,
    decimal? ConservativeThresholdUsed, decimal StudentScore, bool IsAccepted, decimal? MissingScore);
public sealed record MmtEvaluationListItemDto(Guid Id, decimal TotalScore, int AdmissionYear, Guid ClusterId,
    DateTimeOffset EvaluatedAtUtc, int? AcceptedChoicePriority, Guid? AcceptedAdmissionProgramId,
    decimal? MissingScoreForGoal, decimal? ReadinessPercentage, string MotivationalMessageKey);
public sealed record MmtEvaluationDto(Guid Id, Guid UserId, Guid StudentMmtProfileId, Guid? ExamSessionId,
    decimal TotalScore, int AdmissionYear, Guid ClusterId, DateTimeOffset EvaluatedAtUtc,
    int? AcceptedChoicePriority, Guid? AcceptedAdmissionProgramId, decimal? MissingScoreForGoal,
    decimal? ReadinessPercentage, string MotivationalMessageKey, DateTimeOffset CreatedAtUtc,
    IReadOnlyList<MmtAdmissionChoiceSnapshotDto> Choices);
public sealed record StudentMmtProfileFilter(Guid? UserId = null, int? AdmissionYear = null, bool? IsActive = null,
    int Page = 1, int PageSize = 10);
public sealed record MmtEvaluationFilter(Guid? UserId = null, Guid? StudentMmtProfileId = null,
    int? AdmissionYear = null, int Page = 1, int PageSize = 10);

public sealed record MmtDashboardStatsDto(
    int ActiveClustersCount,
    int ActiveUniversitiesCount,
    int ActiveSpecialtiesCount,
    int PublishedProgramsCount,
    int ActiveProgramsCount,
    int ProgramsMissingLatestScoreCount,
    int ProgramsMissingAnyScoreCount,
    int CurrentAdmissionYear,
    int EvaluationsCount,
    int StudentProfilesCount);
