using Adeeb.Modules.AcademicCatalog.Application;
using Adeeb.Modules.AcademicCatalog.Contracts;

namespace Adeeb.AcademicCatalog.Tests;

public sealed class AcademicCatalogValidationTests
{
    [Fact]
    public void Active_subject_requires_tajik_and_russian_translations()
    {
        var request = new SubjectUpsertRequest(
            "math",
            null,
            1,
            1,
            [new TranslationRequest(0, "Math", null)]);

        var result = Validation.ValidateSubject(request);

        Assert.True(result.IsFailure);
        Assert.Contains("translations", result.ValidationErrors!.Keys);
    }

    [Fact]
    public void Draft_subject_allows_single_translation()
    {
        var request = new SubjectUpsertRequest(
            "math",
            null,
            1,
            0,
            [new TranslationRequest(0, "Math", null)]);

        var result = Validation.ValidateSubject(request);

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public void Active_subject_accepts_distinct_tajik_and_russian_translations()
    {
        var request = new SubjectUpsertRequest(
            "biology",
            null,
            0,
            1,
            [
                new TranslationRequest(0, "Биология", null),
                new TranslationRequest(1, "Биология", null)
            ]);

        var result = Validation.ValidateSubject(request);

        Assert.True(result.IsSuccess);
    }
}
