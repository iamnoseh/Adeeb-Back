namespace Adeeb.Modules.Mmt.Application;

internal static class MmtOfficialTestPolicy
{
    public static bool TryDurationMinutes(string clusterCode, out int durationMinutes)
    {
        var number = clusterCode.FirstOrDefault(character => character is >= '1' and <= '5');
        durationMinutes = number switch
        {
            '1' or '5' => 220,
            '2' => 200,
            '3' or '4' => 190,
            _ => 0
        };
        return durationMinutes > 0;
    }

    public static (int SingleChoice, int Matching, int ShortAnswer) QuestionMix(string subjectCode)
    {
        var code = new string(subjectCode.Where(char.IsLetterOrDigit).Select(char.ToUpperInvariant).ToArray());
        var extended = code.Contains("MATH", StringComparison.Ordinal)
            || code.Contains("MATEM", StringComparison.Ordinal)
            || code.Contains("PHYS", StringComparison.Ordinal)
            || code.Contains("FIZ", StringComparison.Ordinal)
            || code.Contains("CHEM", StringComparison.Ordinal)
            || code.Contains("KHIM", StringComparison.Ordinal);
        return extended ? (18, 2, 7) : (20, 4, 2);
    }
}
