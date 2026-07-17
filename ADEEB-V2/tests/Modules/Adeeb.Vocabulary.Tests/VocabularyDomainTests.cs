using Adeeb.Modules.Vocabulary.Domain;

namespace Adeeb.Vocabulary.Tests;

public sealed class VocabularyDomainTests
{
    [Fact]
    public void Word_normalization_is_form_c_trimmed_whitespace_collapsed_and_case_insensitive()
    {
        Assert.Equal("CAFÉ AU LAIT", VocabularyWord.Normalize("  cafe\u0301   au lait "));
    }

    [Fact]
    public void Spaced_repetition_uses_expected_intervals_and_wrong_answer_resets_mastery()
    {
        var progress = new StudentWordProgress(Guid.NewGuid(), Guid.NewGuid());
        var date = new DateOnly(2026, 7, 17); var now = DateTimeOffset.UtcNow;
        int[] intervals = [1, 3, 7, 14, 30];
        foreach (var interval in intervals)
        {
            progress.Apply(true, date, now);
            Assert.Equal(date.AddDays(interval), progress.NextReviewDate);
        }
        progress.Apply(false, date, now);
        Assert.Equal(0, progress.MasteryLevel); Assert.Equal(date.AddDays(1), progress.NextReviewDate); Assert.Equal(1, progress.WrongCount);
    }
}
