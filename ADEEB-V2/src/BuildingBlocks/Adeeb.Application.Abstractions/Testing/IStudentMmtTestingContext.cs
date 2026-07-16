namespace Adeeb.Application.Abstractions.Testing;

public sealed record StudentMmtTestingContext(
    Guid ProfileId,
    Guid ClusterId,
    IReadOnlyList<Guid> SubjectIds,
    int AdmissionChoicesCount,
    int AdmissionYear);

public interface IStudentMmtTestingContext
{
    Task<StudentMmtTestingContext?> GetAsync(Guid userId, CancellationToken ct);
}
