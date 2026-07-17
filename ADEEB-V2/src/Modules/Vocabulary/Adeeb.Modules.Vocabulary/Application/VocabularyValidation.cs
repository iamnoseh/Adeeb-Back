using Adeeb.Modules.Vocabulary.Contracts;
using Adeeb.Modules.Vocabulary.Domain;
using Adeeb.SharedKernel.Errors;
using Adeeb.SharedKernel.Results;

namespace Adeeb.Modules.Vocabulary.Application;

internal static class VocabularyValidation
{
    public static (int Page, int Size) Page(int page, int size) => (Math.Max(1, page), Math.Clamp(size, 1, 50));
    public static Result Validate(LanguageUpsertRequest r)
    {
        var e = Errors(); Required(e, "code", r.Code, 16); Required(e, "nameTg", r.NameTg, 120); Required(e, "nameRu", r.NameRu, 120);
        return Finish(e);
    }
    public static Result Validate(TopicUpsertRequest r)
    {
        var e = Errors(); Enum<VocabularyLevel>(e, "level", r.Level); Enum<VocabularyContentStatus>(e, "status", r.Status); Required(e, "nameTg", r.NameTg, 160); Required(e, "nameRu", r.NameRu, 160);
        return Finish(e);
    }
    public static Result Validate(WordUpsertRequest r)
    {
        var e = Errors(); Enum<VocabularyLevel>(e, "level", r.Level); Enum<VocabularyContentStatus>(e, "status", r.Status);
        Required(e, "targetText", r.TargetText, 240); Required(e, "translationTg", r.TranslationTg, 500); Required(e, "translationRu", r.TranslationRu, 500);
        Required(e, "exampleTarget", r.ExampleTarget, 1000); Required(e, "exampleTg", r.ExampleTg, 1000); Required(e, "exampleRu", r.ExampleRu, 1000);
        return Finish(e);
    }
    public static Result Validate(QuestionUpsertRequest r)
    {
        var e = Errors(); Enum<VocabularyQuestionType>(e, "type", r.Type); Required(e, "promptTarget", r.PromptTarget, 2000); Required(e, "promptTg", r.PromptTg, 2000); Required(e, "promptRu", r.PromptRu, 2000);
        if (r.Options.Count is < 2 or > 12) Add(e, "options");
        if (r.Options.Select(x => x.DisplayOrder).Distinct().Count() != r.Options.Count) Add(e, "options");
        if (r.Options.Any(x => string.IsNullOrWhiteSpace(x.ValueTarget) || string.IsNullOrWhiteSpace(x.ValueTg) || string.IsNullOrWhiteSpace(x.ValueRu)
            || x.ValueTarget.Trim().Length > 1000 || x.ValueTg.Trim().Length > 1000 || x.ValueRu.Trim().Length > 1000)) Add(e, "options");
        return Finish(e);
    }
    private static Dictionary<string, IReadOnlyList<Error>> Errors() => new(StringComparer.OrdinalIgnoreCase);
    private static void Required(Dictionary<string, IReadOnlyList<Error>> e, string field, string? value, int max) { if (string.IsNullOrWhiteSpace(value) || value.Trim().Length > max) Add(e, field); }
    private static void Enum<T>(Dictionary<string, IReadOnlyList<Error>> e, string field, int value) where T : struct, Enum { if (!System.Enum.IsDefined(typeof(T), value)) Add(e, field); }
    private static void Add(Dictionary<string, IReadOnlyList<Error>> e, string field) => e[field] = [Error.Validation($"vocabulary.{field}.invalid", "Vocabulary.Invalid")];
    private static Result Finish(Dictionary<string, IReadOnlyList<Error>> e) => e.Count == 0 ? Result.Success() : Result.ValidationFailure(e);
}
