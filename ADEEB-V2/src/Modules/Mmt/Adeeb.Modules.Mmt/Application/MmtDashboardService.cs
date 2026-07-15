using Adeeb.Application.Abstractions.Time;
using Adeeb.Modules.Mmt.Contracts;
using Adeeb.Modules.Mmt.Domain;
using Adeeb.Modules.Mmt.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Adeeb.Modules.Mmt.Application;

public sealed class MmtDashboardService(
    MmtDbContext db,
    IDateTimeProvider clock,
    IOptions<MmtOptions> options)
{
    private readonly MmtOptions options = options.Value;

    public async Task<MmtDashboardStatsDto> GetAsync(CancellationToken ct)
    {
        var activeClusters = await db.Clusters.CountAsync(x => x.IsActive, ct);
        var activeUniversities = await db.Universities.CountAsync(x => x.IsActive, ct);
        var activeSpecialties = await db.Specialties.CountAsync(x => x.IsActive, ct);
        var publishedPrograms = await db.AdmissionPrograms.CountAsync(x => x.IsPublished, ct);
        var activePrograms = await db.AdmissionPrograms.CountAsync(x => x.IsActive, ct);
        var missingLatestScore = await db.AdmissionPrograms.CountAsync(
            x => x.IsActive && x.IsPublished && !x.PassingScores.Any(s => s.DistributionRound == DistributionRound.Main), ct);
        var missingAnyScore = await db.AdmissionPrograms.CountAsync(x => !x.PassingScores.Any(), ct);
        var evaluations = await db.ExamEvaluations.CountAsync(ct);
        var profiles = await db.StudentProfiles.CountAsync(ct);

        return new MmtDashboardStatsDto(
            activeClusters,
            activeUniversities,
            activeSpecialties,
            publishedPrograms,
            activePrograms,
            missingLatestScore,
            missingAnyScore,
            options.CurrentAdmissionYear ?? clock.UtcNow.Year,
            evaluations,
            profiles);
    }
}
