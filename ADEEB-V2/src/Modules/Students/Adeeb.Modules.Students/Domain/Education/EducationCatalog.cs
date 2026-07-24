using Adeeb.SharedKernel.Domain;

namespace Adeeb.Modules.Students.Domain.Education;

public enum RegionType
{
    Country = 0,
    Province = 1,
    City = 2,
    District = 3,
    Jamoat = 4,
    Town = 5,
    Village = 6,
    Neighborhood = 7
}

public enum SchoolType
{
    GeneralSchool = 0,
    Lyceum = 1,
    Gymnasium = 2,
    PrivateSchool = 3,
    PresidentialSchool = 4,
    College = 5,
    Other = 6
}

public enum SchoolStatus
{
    Draft = 0,
    Verified = 1,
    Inactive = 2,
    Archived = 3,
    Merged = 4
}

public enum EducationStatus
{
    Incomplete = 0,
    PendingSchoolReview = 1,
    Studying = 2,
    Graduated = 3,
    LeftSchool = 4
}

public enum SchoolSuggestionStatus
{
    Pending = 0,
    ApprovedAsNew = 1,
    LinkedToExisting = 2,
    Rejected = 3
}

public enum EnrollmentSource
{
    StudentProfile = 0,
    AdminCorrection = 1,
    SchoolSuggestionReview = 2,
    AcademicRollover = 3,
    CatalogMerge = 4,
    LegacyBackfill = 5
}

public enum AcademicYearRolloverStatus
{
    Preview = 0,
    Approved = 1,
    Executed = 2,
    Failed = 3,
    Cancelled = 4
}

public enum AcademicYearRolloverItemAction
{
    Promote = 0,
    Graduate = 1,
    Skip = 2,
    Conflict = 3
}

public enum EducationImportStatus
{
    Previewed = 0,
    Committed = 1,
    Cancelled = 2,
    Failed = 3
}

public enum EducationImportKind
{
    Regions = 0,
    Schools = 1
}

public sealed class Region : Entity
{
    public const int NameMaxLength = 160;
    public const int FullPathMaxLength = 1200;

    private Region() { }

    public Region(Guid id, Guid? parentId, RegionType type, string nameTg, string nameRu, string normalizedNameTg,
        string normalizedNameRu, int sortOrder, Guid[] pathIds, int depth, DateTimeOffset now)
    {
        Id = id;
        ParentId = parentId;
        Type = type;
        NameTg = nameTg;
        NameRu = nameRu;
        NormalizedNameTg = normalizedNameTg;
        NormalizedNameRu = normalizedNameRu;
        SortOrder = sortOrder;
        PathIds = pathIds;
        Depth = depth;
        IsActive = true;
        CreatedAtUtc = now;
        UpdatedAtUtc = now;
    }

    public Guid? ParentId { get; private set; }
    public RegionType Type { get; private set; }
    public string NameTg { get; private set; } = string.Empty;
    public string NameRu { get; private set; } = string.Empty;
    public string NormalizedNameTg { get; private set; } = string.Empty;
    public string NormalizedNameRu { get; private set; } = string.Empty;
    public string FullPathTg { get; private set; } = string.Empty;
    public string FullPathRu { get; private set; } = string.Empty;
    public Guid[] PathIds { get; private set; } = [];
    public int Depth { get; private set; }
    public int SortOrder { get; private set; }
    public bool IsActive { get; private set; }
    public DateTimeOffset CreatedAtUtc { get; private set; }
    public DateTimeOffset UpdatedAtUtc { get; private set; }
    public uint Version { get; private set; }

    public void Update(string nameTg, string nameRu, string normalizedNameTg, string normalizedNameRu, int sortOrder, DateTimeOffset now)
    {
        NameTg = nameTg;
        NameRu = nameRu;
        NormalizedNameTg = normalizedNameTg;
        NormalizedNameRu = normalizedNameRu;
        SortOrder = sortOrder;
        UpdatedAtUtc = now;
    }

    public void Move(Guid? parentId, Guid[] pathIds, int depth, DateTimeOffset now)
    {
        ParentId = parentId;
        PathIds = pathIds;
        Depth = depth;
        UpdatedAtUtc = now;
    }

    public void SetPaths(string fullPathTg, string fullPathRu, Guid[] pathIds, int depth, DateTimeOffset now)
    {
        FullPathTg = fullPathTg;
        FullPathRu = fullPathRu;
        PathIds = pathIds;
        Depth = depth;
        UpdatedAtUtc = now;
    }

    public void SetActive(bool isActive, DateTimeOffset now)
    {
        IsActive = isActive;
        UpdatedAtUtc = now;
    }
}

public sealed class School : Entity
{
    public const int NameMaxLength = 240;
    public const int ShortNameMaxLength = 120;
    public const int NormalizedNameMaxLength = 300;
    public const int SearchTextMaxLength = 1200;
    public const int AddressTextMaxLength = 400;

    private School() { }

    public School(Guid id, Guid regionId, string? nameTg, string nameRu, string? shortName, int? number,
        SchoolType type, string normalizedName, string searchText, string? addressText, Guid? actorId, DateTimeOffset now)
    {
        Id = id;
        RegionId = regionId;
        NameTg = nameTg;
        NameRu = nameRu;
        ShortName = shortName;
        Number = number;
        Type = type;
        Status = SchoolStatus.Draft;
        NormalizedName = normalizedName;
        SearchText = searchText;
        AddressText = addressText;
        CreatedByUserId = actorId;
        UpdatedByUserId = actorId;
        CreatedAtUtc = now;
        UpdatedAtUtc = now;
    }

    public Guid RegionId { get; private set; }
    public string? NameTg { get; private set; }
    public string NameRu { get; private set; } = string.Empty;
    public string? ShortName { get; private set; }
    public int? Number { get; private set; }
    public SchoolType Type { get; private set; }
    public SchoolStatus Status { get; private set; }
    public string NormalizedName { get; private set; } = string.Empty;
    public string SearchText { get; private set; } = string.Empty;
    public string? AddressText { get; private set; }
    public DateTimeOffset? VerifiedAtUtc { get; private set; }
    public Guid? VerifiedByUserId { get; private set; }
    public DateTimeOffset? ArchivedAtUtc { get; private set; }
    public Guid? ArchivedByUserId { get; private set; }
    public Guid? MergedIntoSchoolId { get; private set; }
    public Guid? CreatedByUserId { get; private set; }
    public Guid? UpdatedByUserId { get; private set; }
    public DateTimeOffset CreatedAtUtc { get; private set; }
    public DateTimeOffset UpdatedAtUtc { get; private set; }
    public uint Version { get; private set; }

    public bool IsSelectableByStudent => Status == SchoolStatus.Verified;

    public void Update(Guid regionId, string? nameTg, string nameRu, string? shortName, int? number, SchoolType type,
        string normalizedName, string searchText, string? addressText, Guid? actorId, DateTimeOffset now)
    {
        RegionId = regionId;
        NameTg = nameTg;
        NameRu = nameRu;
        ShortName = shortName;
        Number = number;
        Type = type;
        NormalizedName = normalizedName;
        SearchText = searchText;
        AddressText = addressText;
        UpdatedByUserId = actorId;
        UpdatedAtUtc = now;
    }

    public void Verify(Guid? actorId, DateTimeOffset now)
    {
        Status = SchoolStatus.Verified;
        VerifiedAtUtc = now;
        VerifiedByUserId = actorId;
        ArchivedAtUtc = null;
        ArchivedByUserId = null;
        UpdatedByUserId = actorId;
        UpdatedAtUtc = now;
    }

    public void SetInactive(Guid? actorId, DateTimeOffset now)
    {
        Status = SchoolStatus.Inactive;
        UpdatedByUserId = actorId;
        UpdatedAtUtc = now;
    }

    public void Archive(Guid? actorId, DateTimeOffset now)
    {
        Status = SchoolStatus.Archived;
        ArchivedAtUtc = now;
        ArchivedByUserId = actorId;
        UpdatedByUserId = actorId;
        UpdatedAtUtc = now;
    }

    public void MergeInto(Guid targetSchoolId, Guid? actorId, DateTimeOffset now)
    {
        Status = SchoolStatus.Merged;
        MergedIntoSchoolId = targetSchoolId;
        ArchivedAtUtc = now;
        ArchivedByUserId = actorId;
        UpdatedByUserId = actorId;
        UpdatedAtUtc = now;
    }
}

public sealed class StudentEducationProfile
{
    private StudentEducationProfile() { }

    public StudentEducationProfile(Guid studentId, Guid? residenceRegionId, Guid? schoolId, Guid? pendingSchoolSuggestionId,
        short? currentGrade, int? academicYearStart, int? academicYearEnd, int? expectedGraduationYear,
        EducationStatus status, string? addressText, DateTimeOffset now)
    {
        StudentId = studentId;
        ResidenceRegionId = residenceRegionId;
        SchoolId = schoolId;
        PendingSchoolSuggestionId = pendingSchoolSuggestionId;
        CurrentGrade = currentGrade;
        AcademicYearStart = academicYearStart;
        AcademicYearEnd = academicYearEnd;
        ExpectedGraduationYear = expectedGraduationYear;
        Status = status;
        AddressText = addressText;
        if (status == EducationStatus.Studying)
        {
            ProfileCompletedAtUtc = now;
        }

        if (status == EducationStatus.Graduated)
        {
            ProfileCompletedAtUtc = now;
            GraduatedAtUtc = now;
        }

        UpdatedAtUtc = now;
    }

    public Guid StudentId { get; private set; }
    public Guid? ResidenceRegionId { get; private set; }
    public Guid? SchoolId { get; private set; }
    public Guid? PendingSchoolSuggestionId { get; private set; }
    public short? CurrentGrade { get; private set; }
    public int? AcademicYearStart { get; private set; }
    public int? AcademicYearEnd { get; private set; }
    public int? ExpectedGraduationYear { get; private set; }
    public EducationStatus Status { get; private set; }
    public string? AddressText { get; private set; }
    public DateTimeOffset? ProfileCompletedAtUtc { get; private set; }
    public DateTimeOffset? GraduatedAtUtc { get; private set; }
    public DateTimeOffset UpdatedAtUtc { get; private set; }
    public uint Version { get; private set; }

    public void SetStudying(Guid residenceRegionId, Guid schoolId, short grade, int academicYearStart, int academicYearEnd,
        int expectedGraduationYear, string? addressText, DateTimeOffset now)
    {
        ResidenceRegionId = residenceRegionId;
        SchoolId = schoolId;
        PendingSchoolSuggestionId = null;
        CurrentGrade = grade;
        AcademicYearStart = academicYearStart;
        AcademicYearEnd = academicYearEnd;
        ExpectedGraduationYear = expectedGraduationYear;
        Status = EducationStatus.Studying;
        AddressText = addressText;
        ProfileCompletedAtUtc ??= now;
        UpdatedAtUtc = now;
    }

    public void SetPendingSchool(Guid residenceRegionId, Guid suggestionId, short grade, int academicYearStart, int academicYearEnd,
        int expectedGraduationYear, string? addressText, DateTimeOffset now)
    {
        ResidenceRegionId = residenceRegionId;
        SchoolId = null;
        PendingSchoolSuggestionId = suggestionId;
        CurrentGrade = grade;
        AcademicYearStart = academicYearStart;
        AcademicYearEnd = academicYearEnd;
        ExpectedGraduationYear = expectedGraduationYear;
        Status = EducationStatus.PendingSchoolReview;
        AddressText = addressText;
        UpdatedAtUtc = now;
    }

    public void Graduate(DateTimeOffset now)
    {
        Status = EducationStatus.Graduated;
        GraduatedAtUtc ??= now;
        PendingSchoolSuggestionId = null;
        UpdatedAtUtc = now;
    }

    public void Promote(short grade, int academicYearStart, int academicYearEnd, int expectedGraduationYear, DateTimeOffset now)
    {
        CurrentGrade = grade;
        AcademicYearStart = academicYearStart;
        AcademicYearEnd = academicYearEnd;
        ExpectedGraduationYear = expectedGraduationYear;
        Status = EducationStatus.Studying;
        UpdatedAtUtc = now;
    }

    public void ReassignSchoolAfterSuggestion(Guid schoolId, DateTimeOffset now)
    {
        SchoolId = schoolId;
        PendingSchoolSuggestionId = null;
        Status = EducationStatus.Studying;
        ProfileCompletedAtUtc ??= now;
        UpdatedAtUtc = now;
    }

    public void RejectPendingSchool(DateTimeOffset now)
    {
        SchoolId = null;
        PendingSchoolSuggestionId = null;
        Status = EducationStatus.Incomplete;
        UpdatedAtUtc = now;
    }

    public void ReplaceSchoolAfterCatalogMerge(Guid schoolId, DateTimeOffset now)
    {
        SchoolId = schoolId;
        UpdatedAtUtc = now;
    }
}

public sealed class StudentSchoolEnrollment : Entity
{
    public const int ReasonMaxLength = 280;

    private StudentSchoolEnrollment() { }

    public StudentSchoolEnrollment(Guid id, Guid studentId, Guid schoolId, Guid regionId, short grade, int academicYearStart,
        int academicYearEnd, EnrollmentSource source, string? changeReason, DateTimeOffset now)
    {
        Id = id;
        StudentId = studentId;
        SchoolId = schoolId;
        RegionId = regionId;
        Grade = grade;
        AcademicYearStart = academicYearStart;
        AcademicYearEnd = academicYearEnd;
        IsCurrent = true;
        StartedAtUtc = now;
        Source = source;
        ChangeReason = changeReason;
        CreatedAtUtc = now;
    }

    public Guid StudentId { get; private set; }
    public Guid SchoolId { get; private set; }
    public Guid RegionId { get; private set; }
    public short Grade { get; private set; }
    public int AcademicYearStart { get; private set; }
    public int AcademicYearEnd { get; private set; }
    public bool IsCurrent { get; private set; }
    public DateTimeOffset StartedAtUtc { get; private set; }
    public DateTimeOffset? EndedAtUtc { get; private set; }
    public string? ChangeReason { get; private set; }
    public EnrollmentSource Source { get; private set; }
    public DateTimeOffset CreatedAtUtc { get; private set; }

    public void End(string? reason, DateTimeOffset now)
    {
        IsCurrent = false;
        EndedAtUtc = now;
        ChangeReason = reason ?? ChangeReason;
    }
}

public sealed class SchoolSuggestion : Entity
{
    public const int NameMaxLength = School.NameMaxLength;
    public const int AddressMaxLength = School.AddressTextMaxLength;
    public const int RejectionReasonMaxLength = 400;

    private SchoolSuggestion() { }

    public SchoolSuggestion(Guid id, Guid submittedByStudentId, string suggestedName, int? suggestedNumber, Guid regionId,
        string normalizedName, string? addressText, DateTimeOffset now)
    {
        Id = id;
        SubmittedByStudentId = submittedByStudentId;
        SuggestedName = suggestedName;
        SuggestedNumber = suggestedNumber;
        RegionId = regionId;
        NormalizedName = normalizedName;
        AddressText = addressText;
        Status = SchoolSuggestionStatus.Pending;
        CreatedAtUtc = now;
    }

    public Guid SubmittedByStudentId { get; private set; }
    public string SuggestedName { get; private set; } = string.Empty;
    public int? SuggestedNumber { get; private set; }
    public Guid RegionId { get; private set; }
    public string NormalizedName { get; private set; } = string.Empty;
    public string? AddressText { get; private set; }
    public SchoolSuggestionStatus Status { get; private set; }
    public Guid? ApprovedSchoolId { get; private set; }
    public string? RejectionReason { get; private set; }
    public DateTimeOffset CreatedAtUtc { get; private set; }
    public DateTimeOffset? ReviewedAtUtc { get; private set; }
    public Guid? ReviewedByAdminId { get; private set; }
    public uint Version { get; private set; }

    public void Approve(Guid schoolId, bool createdAsNew, Guid? actorId, DateTimeOffset now)
    {
        ApprovedSchoolId = schoolId;
        Status = createdAsNew ? SchoolSuggestionStatus.ApprovedAsNew : SchoolSuggestionStatus.LinkedToExisting;
        ReviewedByAdminId = actorId;
        ReviewedAtUtc = now;
        RejectionReason = null;
    }

    public void Reject(string reason, Guid? actorId, DateTimeOffset now)
    {
        Status = SchoolSuggestionStatus.Rejected;
        RejectionReason = reason;
        ReviewedByAdminId = actorId;
        ReviewedAtUtc = now;
    }

    public void RelinkApprovedSchool(Guid schoolId)
    {
        if (ApprovedSchoolId == schoolId)
        {
            return;
        }

        ApprovedSchoolId = schoolId;
    }
}

public sealed class AcademicYearRollover : Entity
{
    private AcademicYearRollover() { }

    public AcademicYearRollover(Guid id, int academicYearStart, int academicYearEnd, string idempotencyKey, DateTimeOffset now)
    {
        Id = id;
        AcademicYearStart = academicYearStart;
        AcademicYearEnd = academicYearEnd;
        IdempotencyKey = idempotencyKey;
        Status = AcademicYearRolloverStatus.Preview;
        PreviewCreatedAtUtc = now;
    }

    public int AcademicYearStart { get; private set; }
    public int AcademicYearEnd { get; private set; }
    public AcademicYearRolloverStatus Status { get; private set; }
    public DateTimeOffset PreviewCreatedAtUtc { get; private set; }
    public DateTimeOffset? ApprovedAtUtc { get; private set; }
    public DateTimeOffset? ExecutedAtUtc { get; private set; }
    public Guid? ExecutedByUserId { get; private set; }
    public int PromotedCount { get; private set; }
    public int GraduatedCount { get; private set; }
    public int SkippedCount { get; private set; }
    public int ConflictCount { get; private set; }
    public string? Notes { get; private set; }
    public string IdempotencyKey { get; private set; } = string.Empty;
    public uint Version { get; private set; }

    public void Approve(DateTimeOffset now) { Status = AcademicYearRolloverStatus.Approved; ApprovedAtUtc = now; }

    public void SetPreviewCounts(int promoted, int graduated, int skipped, int conflicts)
    {
        PromotedCount = promoted;
        GraduatedCount = graduated;
        SkippedCount = skipped;
        ConflictCount = conflicts;
    }

    public void Complete(Guid? actorId, int promoted, int graduated, int skipped, int conflicts, DateTimeOffset now)
    {
        Status = AcademicYearRolloverStatus.Executed;
        ExecutedByUserId = actorId;
        ExecutedAtUtc = now;
        PromotedCount = promoted;
        GraduatedCount = graduated;
        SkippedCount = skipped;
        ConflictCount = conflicts;
    }
}

public sealed class AcademicYearRolloverItem : Entity
{
    private AcademicYearRolloverItem() { }

    public AcademicYearRolloverItem(Guid id, Guid rolloverId, Guid studentId, uint profileVersion, short? sourceGrade,
        AcademicYearRolloverItemAction action, string? reason, DateTimeOffset now)
    {
        Id = id;
        RolloverId = rolloverId;
        StudentId = studentId;
        ProfileVersion = profileVersion;
        SourceGrade = sourceGrade;
        Action = action;
        Reason = reason;
        CreatedAtUtc = now;
    }

    public Guid RolloverId { get; private set; }
    public Guid StudentId { get; private set; }
    public uint ProfileVersion { get; private set; }
    public short? SourceGrade { get; private set; }
    public AcademicYearRolloverItemAction Action { get; private set; }
    public string? Reason { get; private set; }
    public DateTimeOffset CreatedAtUtc { get; private set; }
}

public sealed class StudentEducationAuditLog : Entity
{
    private StudentEducationAuditLog() { }

    public StudentEducationAuditLog(Guid id, Guid? actorUserId, string action, string resourceType, string resourceId,
        Guid? studentId, string? oldValuesJson, string? newValuesJson, string? correlationId, DateTimeOffset now)
    {
        Id = id;
        ActorUserId = actorUserId;
        Action = action;
        ResourceType = resourceType;
        ResourceId = resourceId;
        StudentId = studentId;
        OldValuesJson = oldValuesJson;
        NewValuesJson = newValuesJson;
        CorrelationId = correlationId;
        CreatedAtUtc = now;
    }

    public Guid? ActorUserId { get; private set; }
    public string Action { get; private set; } = string.Empty;
    public string ResourceType { get; private set; } = string.Empty;
    public string ResourceId { get; private set; } = string.Empty;
    public Guid? StudentId { get; private set; }
    public string? OldValuesJson { get; private set; }
    public string? NewValuesJson { get; private set; }
    public string? CorrelationId { get; private set; }
    public DateTimeOffset CreatedAtUtc { get; private set; }
}

public sealed class EducationImportBatch : Entity
{
    public const int FileNameMaxLength = 260;

    private EducationImportBatch() { }

    public EducationImportBatch(Guid id, EducationImportKind kind, string fileName, Guid? requestedByUserId, int totalRows,
        int validRows, int invalidRows, DateTimeOffset now)
    {
        Id = id;
        Kind = kind;
        FileName = fileName;
        RequestedByUserId = requestedByUserId;
        TotalRows = totalRows;
        ValidRows = validRows;
        InvalidRows = invalidRows;
        Status = EducationImportStatus.Previewed;
        CreatedAtUtc = now;
    }

    public EducationImportKind Kind { get; private set; }
    public string FileName { get; private set; } = string.Empty;
    public Guid? RequestedByUserId { get; private set; }
    public EducationImportStatus Status { get; private set; }
    public int TotalRows { get; private set; }
    public int ValidRows { get; private set; }
    public int InvalidRows { get; private set; }
    public int CreatedRegions { get; private set; }
    public int CreatedSchools { get; private set; }
    public int SkippedSchools { get; private set; }
    public string? SummaryJson { get; private set; }
    public DateTimeOffset CreatedAtUtc { get; private set; }
    public DateTimeOffset? CompletedAtUtc { get; private set; }

    public void Complete(int createdRegions, int createdSchools, int skippedSchools, string summaryJson, DateTimeOffset now)
    {
        Status = EducationImportStatus.Committed;
        CreatedRegions = createdRegions;
        CreatedSchools = createdSchools;
        SkippedSchools = skippedSchools;
        SummaryJson = summaryJson;
        CompletedAtUtc = now;
    }

    public void Fail(string summaryJson, DateTimeOffset now)
    {
        Status = EducationImportStatus.Failed;
        SummaryJson = summaryJson;
        CompletedAtUtc = now;
    }
}
