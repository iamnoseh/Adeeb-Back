using Adeeb.SharedKernel.Domain;

namespace Adeeb.Modules.Mmt.Domain;

public sealed class StudentMmtProfile : Entity
{
    private readonly List<StudentAdmissionChoice> _choices = [];
    private StudentMmtProfile() { }

    public StudentMmtProfile(Guid id, Guid userId, Guid clusterId, int admissionYear, Guid? goalProgramId, DateTimeOffset now)
    {
        Id = id;
        UserId = userId;
        CreatedAtUtc = now;
        IsActive = true;
        Update(clusterId, admissionYear, goalProgramId, now);
    }

    public Guid UserId { get; private set; }
    public Guid MmtClusterId { get; private set; }
    public int AdmissionYear { get; private set; }
    public Guid? GoalAdmissionProgramId { get; private set; }
    public bool IsActive { get; private set; }
    public DateTimeOffset CreatedAtUtc { get; private set; }
    public DateTimeOffset UpdatedAtUtc { get; private set; }
    public MmtCluster MmtCluster { get; private set; } = null!;
    public AdmissionProgram? GoalAdmissionProgram { get; private set; }
    public IReadOnlyCollection<StudentAdmissionChoice> Choices => _choices;

    public void Update(Guid clusterId, int admissionYear, Guid? goalProgramId, DateTimeOffset now)
    {
        MmtClusterId = clusterId;
        AdmissionYear = admissionYear;
        GoalAdmissionProgramId = goalProgramId;
        IsActive = true;
        UpdatedAtUtc = now;
    }

    public void Deactivate(DateTimeOffset now)
    {
        IsActive = false;
        UpdatedAtUtc = now;
    }
}

public sealed class StudentAdmissionChoice : Entity
{
    private StudentAdmissionChoice() { }

    public StudentAdmissionChoice(Guid id, Guid profileId, Guid programId, int priorityOrder, DateTimeOffset now)
    {
        Id = id;
        StudentMmtProfileId = profileId;
        AdmissionProgramId = programId;
        PriorityOrder = priorityOrder;
        CreatedAtUtc = now;
        UpdatedAtUtc = now;
    }

    public Guid StudentMmtProfileId { get; private set; }
    public Guid AdmissionProgramId { get; private set; }
    public int PriorityOrder { get; private set; }
    public DateTimeOffset CreatedAtUtc { get; private set; }
    public DateTimeOffset UpdatedAtUtc { get; private set; }
    public StudentMmtProfile StudentMmtProfile { get; private set; } = null!;
    public AdmissionProgram AdmissionProgram { get; private set; } = null!;
}

public sealed class MmtExamEvaluation : Entity
{
    private readonly List<MmtAdmissionChoiceSnapshot> _choiceSnapshots = [];
    private MmtExamEvaluation() { }

    public MmtExamEvaluation(Guid id, Guid userId, Guid profileId, Guid? examSessionId, decimal totalScore,
        int admissionYear, Guid clusterId, DateTimeOffset evaluatedAtUtc, int? acceptedPriority,
        Guid? acceptedProgramId, decimal? missingScoreForGoal, decimal? readinessPercentage,
        string motivationalMessageKey, DateTimeOffset createdAtUtc)
    {
        Id = id;
        UserId = userId;
        StudentMmtProfileId = profileId;
        ExamSessionId = examSessionId;
        TotalScore = totalScore;
        AdmissionYear = admissionYear;
        ClusterId = clusterId;
        EvaluatedAtUtc = evaluatedAtUtc;
        AcceptedChoicePriority = acceptedPriority;
        AcceptedAdmissionProgramId = acceptedProgramId;
        MissingScoreForGoal = missingScoreForGoal;
        ReadinessPercentage = readinessPercentage;
        MotivationalMessageKey = motivationalMessageKey;
        CreatedAtUtc = createdAtUtc;
    }

    public Guid UserId { get; private set; }
    public Guid StudentMmtProfileId { get; private set; }
    public Guid? ExamSessionId { get; private set; }
    public decimal TotalScore { get; private set; }
    public int AdmissionYear { get; private set; }
    public Guid ClusterId { get; private set; }
    public DateTimeOffset EvaluatedAtUtc { get; private set; }
    public int? AcceptedChoicePriority { get; private set; }
    public Guid? AcceptedAdmissionProgramId { get; private set; }
    public decimal? MissingScoreForGoal { get; private set; }
    public decimal? ReadinessPercentage { get; private set; }
    public string MotivationalMessageKey { get; private set; } = string.Empty;
    public DateTimeOffset CreatedAtUtc { get; private set; }
    public StudentMmtProfile StudentMmtProfile { get; private set; } = null!;
    public AdmissionProgram? AcceptedAdmissionProgram { get; private set; }
    public MmtCluster Cluster { get; private set; } = null!;
    public IReadOnlyCollection<MmtAdmissionChoiceSnapshot> ChoiceSnapshots => _choiceSnapshots;
}

public sealed class MmtAdmissionChoiceSnapshot : Entity
{
    private MmtAdmissionChoiceSnapshot() { }

    public MmtAdmissionChoiceSnapshot(Guid id, Guid evaluationId, int priorityOrder, Guid programId,
        string universityName, string specialtyCode, string specialtyName, string clusterCode,
        AdmissionType admissionType, StudyForm studyForm, StudyLanguage studyLanguage, int admissionYear,
        decimal? passingScoreUsed, decimal? conservativeThresholdUsed, decimal studentScore,
        bool isAccepted, decimal? missingScore)
    {
        Id = id;
        MmtExamEvaluationId = evaluationId;
        PriorityOrder = priorityOrder;
        AdmissionProgramId = programId;
        UniversityNameSnapshot = universityName;
        SpecialtyCodeSnapshot = specialtyCode;
        SpecialtyNameSnapshot = specialtyName;
        ClusterCodeSnapshot = clusterCode;
        AdmissionType = admissionType;
        StudyForm = studyForm;
        StudyLanguage = studyLanguage;
        AdmissionYear = admissionYear;
        PassingScoreUsed = passingScoreUsed;
        ConservativeThresholdUsed = conservativeThresholdUsed;
        StudentScore = studentScore;
        IsAccepted = isAccepted;
        MissingScore = missingScore;
    }

    public Guid MmtExamEvaluationId { get; private set; }
    public int PriorityOrder { get; private set; }
    public Guid AdmissionProgramId { get; private set; }
    public string UniversityNameSnapshot { get; private set; } = string.Empty;
    public string SpecialtyCodeSnapshot { get; private set; } = string.Empty;
    public string SpecialtyNameSnapshot { get; private set; } = string.Empty;
    public string ClusterCodeSnapshot { get; private set; } = string.Empty;
    public AdmissionType AdmissionType { get; private set; }
    public StudyForm StudyForm { get; private set; }
    public StudyLanguage StudyLanguage { get; private set; }
    public int AdmissionYear { get; private set; }
    public decimal? PassingScoreUsed { get; private set; }
    public decimal? ConservativeThresholdUsed { get; private set; }
    public decimal StudentScore { get; private set; }
    public bool IsAccepted { get; private set; }
    public decimal? MissingScore { get; private set; }
    public MmtExamEvaluation MmtExamEvaluation { get; private set; } = null!;
    public AdmissionProgram AdmissionProgram { get; private set; } = null!;
}
