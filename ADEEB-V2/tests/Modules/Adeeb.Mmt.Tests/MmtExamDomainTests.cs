using Adeeb.Modules.Mmt.Domain;

namespace Adeeb.Mmt.Tests;

public sealed class MmtExamDomainTests
{
    private static readonly DateTimeOffset Now = new(2026, 7, 22, 0, 0, 0, TimeSpan.Zero);

    [Theory]
    [InlineData(20, 4, 2)]
    [InlineData(18, 2, 7)]
    public void Official_subtest_blueprints_produce_exactly_forty_raw_points(
        int singleChoice, int matching, int shortAnswer)
    {
        var subtest = new MmtSubtestBlueprint(Guid.NewGuid(), Guid.NewGuid(), "A1", 1,
            Guid.NewGuid(), singleChoice, matching, shortAnswer);

        Assert.Equal(40, subtest.MaxRawScore);
        Assert.Equal(singleChoice + matching + shortAnswer, subtest.QuestionCount);
    }

    [Fact]
    public void Published_exam_version_is_immutable()
    {
        var version = new MmtExamVersion(Guid.NewGuid(), 2026, "MMT 2026", "ММТ 2026",
            true, "https://ntc.tj", "checksum", Now);
        version.Publish(Now.AddMinutes(1));

        Assert.Throws<InvalidOperationException>(() => version.Update("Changed", "Изменено",
            true, null, null, Now.AddMinutes(2)));
    }

    [Theory]
    [InlineData("A0")]
    [InlineData("A5")]
    [InlineData("B1")]
    public void Unsupported_subtest_code_is_rejected(string code)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new MmtSubtestBlueprint(Guid.NewGuid(),
            Guid.NewGuid(), code, 1, Guid.NewGuid(), 20, 4, 2));
    }

    [Fact]
    public void Scale_keeps_four_decimal_precision()
    {
        var entry = new MmtScoreScaleEntry(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "A1",
            null, 23, 41.23456m, 75m);

        Assert.Equal(41.2346m, entry.ScaledScore);
    }

    [Fact]
    public void Cluster_preserves_subject_order_for_official_subtests()
    {
        var subjects = Enumerable.Range(0, 4).Select(_ => Guid.NewGuid()).ToArray();
        var cluster = new MmtCluster(Guid.NewGuid(), "Cluster 1", "C1", null, Now);

        cluster.ReplaceSubjects(subjects);

        Assert.Equal(subjects, cluster.Subjects.OrderBy(x => x.DisplayOrder).Select(x => x.SubjectId));
        Assert.Equal([1, 2, 3, 4], cluster.Subjects.OrderBy(x => x.DisplayOrder).Select(x => x.DisplayOrder));
    }

    [Fact]
    public void Reordering_cluster_subjects_updates_existing_links_without_duplicates()
    {
        var subjects = Enumerable.Range(0, 4).Select(_ => Guid.NewGuid()).ToArray();
        var cluster = new MmtCluster(Guid.NewGuid(), "Cluster 1", "C1", null, Now);
        cluster.ReplaceSubjects(subjects);

        cluster.ReplaceSubjects(subjects.Reverse());

        Assert.Equal(subjects.Reverse(), cluster.Subjects.OrderBy(x => x.DisplayOrder).Select(x => x.SubjectId));
        Assert.Equal(4, cluster.Subjects.Count);
    }
}
