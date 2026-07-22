using Adeeb.SharedKernel.Domain;

namespace Adeeb.Modules.Mmt.Domain;

public enum MmtExamVersionStatus { Draft = 0, Published = 1, Archived = 2 }

public sealed class MmtExamVersion : Entity
{
    private readonly List<MmtClusterExamBlueprint> _blueprints = [];
    private readonly List<MmtSpecialtyRange> _specialtyRanges = [];
    private readonly List<MmtScoreScaleEntry> _scaleEntries = [];
    private readonly List<MmtPassThreshold> _thresholds = [];
    private MmtExamVersion() { }

    public MmtExamVersion(Guid id, int admissionYear, string nameTg, string nameRu, bool isOfficial,
        string? sourceUrl, string? sourceChecksum, DateTimeOffset now)
    {
        Id = id;
        AdmissionYear = admissionYear;
        CreatedAtUtc = now;
        Update(nameTg, nameRu, isOfficial, sourceUrl, sourceChecksum, now);
    }

    public int AdmissionYear { get; private set; }
    public string NameTg { get; private set; } = string.Empty;
    public string NameRu { get; private set; } = string.Empty;
    public bool IsOfficial { get; private set; }
    public string? SourceUrl { get; private set; }
    public string? SourceChecksum { get; private set; }
    public MmtExamVersionStatus Status { get; private set; }
    public DateTimeOffset CreatedAtUtc { get; private set; }
    public DateTimeOffset UpdatedAtUtc { get; private set; }
    public DateTimeOffset? PublishedAtUtc { get; private set; }
    public uint Version { get; private set; }
    public IReadOnlyCollection<MmtClusterExamBlueprint> Blueprints => _blueprints;
    public IReadOnlyCollection<MmtSpecialtyRange> SpecialtyRanges => _specialtyRanges;
    public IReadOnlyCollection<MmtScoreScaleEntry> ScaleEntries => _scaleEntries;
    public IReadOnlyCollection<MmtPassThreshold> Thresholds => _thresholds;

    public void Update(string nameTg, string nameRu, bool isOfficial, string? sourceUrl,
        string? sourceChecksum, DateTimeOffset now)
    {
        EnsureDraft();
        NameTg = Required(nameTg, nameof(nameTg), 160);
        NameRu = Required(nameRu, nameof(nameRu), 160);
        IsOfficial = isOfficial;
        SourceUrl = Optional(sourceUrl, 500);
        SourceChecksum = Optional(sourceChecksum, 128);
        UpdatedAtUtc = now;
    }

    public void Publish(DateTimeOffset now)
    {
        EnsureDraft();
        Status = MmtExamVersionStatus.Published;
        PublishedAtUtc = now;
        UpdatedAtUtc = now;
    }

    public void Archive(DateTimeOffset now)
    {
        if (Status == MmtExamVersionStatus.Archived) return;
        Status = MmtExamVersionStatus.Archived;
        UpdatedAtUtc = now;
    }

    public string NameFor(bool russian) => russian ? NameRu : NameTg;
    public void EnsureDraft()
    {
        if (Status != MmtExamVersionStatus.Draft)
            throw new InvalidOperationException("Published MMT exam versions are immutable.");
    }

    private static string Required(string value, string name, int max)
    {
        var normalized = value?.Trim() ?? string.Empty;
        if (normalized.Length is 0 || normalized.Length > max) throw new ArgumentOutOfRangeException(name);
        return normalized;
    }
    private static string? Optional(string? value, int max)
    {
        var normalized = string.IsNullOrWhiteSpace(value) ? null : value.Trim();
        if (normalized?.Length > max) throw new ArgumentOutOfRangeException(nameof(value));
        return normalized;
    }
}

public sealed class MmtClusterExamBlueprint : Entity
{
    private readonly List<MmtSubtestBlueprint> _subtests = [];
    private MmtClusterExamBlueprint() { }
    public MmtClusterExamBlueprint(Guid id, Guid examVersionId, Guid clusterId, int durationMinutes)
    { Id = id; ExamVersionId = examVersionId; MmtClusterId = clusterId; SetDuration(durationMinutes); }
    public Guid ExamVersionId { get; private set; }
    public Guid MmtClusterId { get; private set; }
    public int DurationMinutes { get; private set; }
    public MmtExamVersion ExamVersion { get; private set; } = null!;
    public MmtCluster Cluster { get; private set; } = null!;
    public IReadOnlyCollection<MmtSubtestBlueprint> Subtests => _subtests;
    public int QuestionCount => _subtests.Sum(x => x.QuestionCount);
    public void SetDuration(int value)
    {
        if (value is < 30 or > 360) throw new ArgumentOutOfRangeException(nameof(value));
        DurationMinutes = value;
    }
}

public sealed class MmtSubtestBlueprint : Entity
{
    private MmtSubtestBlueprint() { }
    public MmtSubtestBlueprint(Guid id, Guid clusterBlueprintId, string code, int displayOrder, Guid subjectId,
        int singleChoiceCount, int matchingCount, int shortAnswerCount)
    {
        Id = id; MmtClusterExamBlueprintId = clusterBlueprintId; Code = NormalizeCode(code);
        DisplayOrder = displayOrder; SubjectId = subjectId; SingleChoiceCount = singleChoiceCount;
        MatchingCount = matchingCount; ShortAnswerCount = shortAnswerCount;
        if (displayOrder is < 1 or > 4 || QuestionCount is < 1 or > 40) throw new ArgumentOutOfRangeException(nameof(displayOrder));
    }
    public Guid MmtClusterExamBlueprintId { get; private set; }
    public string Code { get; private set; } = string.Empty;
    public int DisplayOrder { get; private set; }
    public Guid SubjectId { get; private set; }
    public int SingleChoiceCount { get; private set; }
    public int MatchingCount { get; private set; }
    public int ShortAnswerCount { get; private set; }
    public int QuestionCount => SingleChoiceCount + MatchingCount + ShortAnswerCount;
    public int MaxRawScore => SingleChoiceCount + MatchingCount * 4 + ShortAnswerCount * 2;
    public MmtClusterExamBlueprint ClusterBlueprint { get; private set; } = null!;
    internal static string NormalizeCode(string value)
    {
        var code = value.Trim().ToUpperInvariant();
        if (code is not ("A1" or "A2" or "A3" or "A4")) throw new ArgumentOutOfRangeException(nameof(value));
        return code;
    }
}

public sealed class MmtSpecialtyRange : Entity
{
    private readonly List<MmtSpecialtyRangeSpecialty> _specialties = [];
    private MmtSpecialtyRange() { }
    public MmtSpecialtyRange(Guid id, Guid examVersionId, Guid clusterId, string code,
        decimal a2MaxScore, decimal a3MaxScore, decimal a4MaxScore)
    {
        Id = id; ExamVersionId = examVersionId; MmtClusterId = clusterId;
        Code = code.Trim().ToUpperInvariant(); A2MaxScore = Positive(a2MaxScore);
        A3MaxScore = Positive(a3MaxScore); A4MaxScore = Positive(a4MaxScore);
    }
    public Guid ExamVersionId { get; private set; }
    public Guid MmtClusterId { get; private set; }
    public string Code { get; private set; } = string.Empty;
    public decimal A2MaxScore { get; private set; }
    public decimal A3MaxScore { get; private set; }
    public decimal A4MaxScore { get; private set; }
    public MmtExamVersion ExamVersion { get; private set; } = null!;
    public MmtCluster Cluster { get; private set; } = null!;
    public IReadOnlyCollection<MmtSpecialtyRangeSpecialty> Specialties => _specialties;
    private static decimal Positive(decimal value) => value is > 0 and <= 500 ? value : throw new ArgumentOutOfRangeException(nameof(value));
}

public sealed class MmtSpecialtyRangeSpecialty
{
    private MmtSpecialtyRangeSpecialty() { }
    public MmtSpecialtyRangeSpecialty(Guid rangeId, Guid specialtyId) { MmtSpecialtyRangeId = rangeId; SpecialtyId = specialtyId; }
    public Guid MmtSpecialtyRangeId { get; private set; }
    public Guid SpecialtyId { get; private set; }
    public MmtSpecialtyRange Range { get; private set; } = null!;
    public Specialty Specialty { get; private set; } = null!;
}

public sealed class MmtScoreScaleEntry : Entity
{
    private MmtScoreScaleEntry() { }
    public MmtScoreScaleEntry(Guid id, Guid examVersionId, Guid clusterId, string subtestCode,
        Guid? specialtyRangeId, int rawScore, decimal scaledScore, decimal maxScaledScore)
    {
        Id = id; ExamVersionId = examVersionId; MmtClusterId = clusterId;
        SubtestCode = MmtSubtestBlueprint.NormalizeCode(subtestCode); SpecialtyRangeId = specialtyRangeId;
        RawScore = rawScore is >= 0 and <= 40 ? rawScore : throw new ArgumentOutOfRangeException(nameof(rawScore));
        ScaledScore = scaledScore is >= 0 and <= 500 ? decimal.Round(scaledScore, 4) : throw new ArgumentOutOfRangeException(nameof(scaledScore));
        MaxScaledScore = maxScaledScore is > 0 and <= 500 ? decimal.Round(maxScaledScore, 4) : throw new ArgumentOutOfRangeException(nameof(maxScaledScore));
    }
    public Guid ExamVersionId { get; private set; }
    public Guid MmtClusterId { get; private set; }
    public string SubtestCode { get; private set; } = string.Empty;
    public Guid? SpecialtyRangeId { get; private set; }
    public int RawScore { get; private set; }
    public decimal ScaledScore { get; private set; }
    public decimal MaxScaledScore { get; private set; }
    public MmtExamVersion ExamVersion { get; private set; } = null!;
    public MmtSpecialtyRange? SpecialtyRange { get; private set; }
}

public sealed class MmtPassThreshold : Entity
{
    private MmtPassThreshold() { }
    public MmtPassThreshold(Guid id, Guid examVersionId, Guid clusterId, string subtestCode, int minimumRawScore)
    {
        Id = id; ExamVersionId = examVersionId; MmtClusterId = clusterId;
        SubtestCode = MmtSubtestBlueprint.NormalizeCode(subtestCode);
        MinimumRawScore = minimumRawScore is >= 0 and <= 40 ? minimumRawScore : throw new ArgumentOutOfRangeException(nameof(minimumRawScore));
    }
    public Guid ExamVersionId { get; private set; }
    public Guid MmtClusterId { get; private set; }
    public string SubtestCode { get; private set; } = string.Empty;
    public int MinimumRawScore { get; private set; }
    public MmtExamVersion ExamVersion { get; private set; } = null!;
}
