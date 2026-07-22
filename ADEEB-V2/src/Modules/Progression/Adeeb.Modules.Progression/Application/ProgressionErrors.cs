using Adeeb.SharedKernel.Errors;

namespace Adeeb.Modules.Progression.Application;

public static class ProgressionErrors
{
    public static readonly Error NotFound = Error.NotFound("progression.not_found", "Progression.NotFound");
    public static readonly Error InvalidLeague = Error.Validation("progression.league_invalid", "Progression.LeagueInvalid");
    public static readonly Error InvalidThresholds = Error.Validation("progression.thresholds_invalid", "Progression.ThresholdsInvalid");
    public static readonly Error StructuralLocked = Error.Conflict("progression.structural_locked", "Progression.StructuralLocked");
    public static readonly Error SeasonActive = Error.Conflict("progression.season_active", "Progression.SeasonActive");
    public static readonly Error SeasonUnavailable = Error.Conflict("progression.season_unavailable", "Progression.SeasonUnavailable");
    public static readonly Error StudentUnavailable = Error.Forbidden("progression.student_unavailable", "Progression.StudentUnavailable");
    public static readonly Error AvatarInvalid = Error.Validation("progression.avatar_invalid", "Progression.AvatarInvalid");
}
