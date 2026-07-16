using Adeeb.Application.Abstractions.Testing;
using Adeeb.Application.Abstractions.Time;
using Adeeb.Modules.Mmt.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Adeeb.Modules.Mmt.Application;

internal sealed class StudentMmtTestingContextProvider(
    MmtDbContext db,
    IOptions<MmtOptions> options,
    IDateTimeProvider clock) : IStudentMmtTestingContext
{
    public async Task<StudentMmtTestingContext?> GetAsync(Guid userId, CancellationToken ct)
    {
        var year = options.Value.CurrentAdmissionYear ?? clock.UtcNow.Year;
        return await db.StudentProfiles.AsNoTracking()
            .Where(x => x.UserId == userId && x.AdmissionYear == year && x.IsActive)
            .Select(x => new StudentMmtTestingContext(
                x.Id,
                x.MmtClusterId,
                x.MmtCluster.Subjects.Select(subject => subject.SubjectId).ToList(),
                x.Choices.Count,
                x.AdmissionYear))
            .SingleOrDefaultAsync(ct);
    }
}
