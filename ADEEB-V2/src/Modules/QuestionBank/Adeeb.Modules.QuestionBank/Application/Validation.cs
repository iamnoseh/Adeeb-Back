using Adeeb.Application.Abstractions.Localization;
using Adeeb.Modules.QuestionBank.Contracts;
using Adeeb.Modules.QuestionBank.Domain;
using Adeeb.SharedKernel.Errors;
using Adeeb.SharedKernel.Results;

namespace Adeeb.Modules.QuestionBank.Application;

internal static class Validation
{
    public static Result ValidateQuestion(QuestionUpsertRequest request)
    {
        var errors = new Dictionary<string, IReadOnlyList<Error>>(StringComparer.OrdinalIgnoreCase);
        if (request.SubjectId == Guid.Empty)
        {
            errors["subjectId"] = [Error.Validation("question.subject_id.required", "Validation.Required")];
        }

        if (!Enum.IsDefined(typeof(QuestionType), request.Type))
        {
            errors["type"] = [Error.Validation("question.type.invalid", "QuestionBank.InvalidType")];
        }
        var type = (QuestionType)request.Type;

        if (!Enum.IsDefined(typeof(DifficultyLevel), request.Difficulty))
        {
            errors["difficulty"] = [Error.Validation("question.difficulty.invalid", "QuestionBank.InvalidDifficulty")];
        }

        if (!Enum.IsDefined(typeof(QuestionStatus), request.Status))
        {
            errors["status"] = [Error.Validation("question.status.invalid", "Validation.InvalidStatus")];
        }
        var status = (QuestionStatus)request.Status;

        ValidateQuestionTranslations(request.Translations, status, errors);
        if (errors.Count == 0)
        {
            ValidateAnswerOptions(type, request.AnswerOptions, request.Translations.Select(x => x.Language).ToHashSet(), errors);
        }

        return errors.Count == 0 ? Result.Success() : Result.ValidationFailure(errors);
    }

    public static bool TryParseLanguage(int value, out SupportedLanguage language)
    {
        language = SupportedLanguage.Tajik;
        if (!Enum.IsDefined(typeof(SupportedLanguage), value))
        {
            return false;
        }

        language = (SupportedLanguage)value;
        return true;
    }

    private static void ValidateQuestionTranslations(IReadOnlyList<QuestionTranslationRequest>? translations, QuestionStatus status, Dictionary<string, IReadOnlyList<Error>> errors)
    {
        if (translations is null || translations.Count == 0)
        {
            errors["translations"] = [Error.Validation("question.translations.required", "Validation.Required")];
            return;
        }

        var languages = new HashSet<SupportedLanguage>();
        for (var i = 0; i < translations.Count; i++)
        {
            var item = translations[i];
            if (!TryParseLanguage(item.Language, out var language))
            {
                errors[$"translations[{i}].language"] = [Error.Validation("question.language.unsupported", "Validation.UnsupportedLanguage")];
                continue;
            }

            if (!languages.Add(language))
            {
                errors[$"translations[{i}].language"] = [Error.Validation("question.language.duplicate", "Validation.DuplicateLanguage")];
            }

            if (string.IsNullOrWhiteSpace(item.Content))
            {
                errors[$"translations[{i}].content"] = [Error.Validation("question.content.required", "Validation.Required")];
            }
        }

        if (status == QuestionStatus.Active && (!languages.Contains(SupportedLanguage.Tajik) || !languages.Contains(SupportedLanguage.Russian)))
        {
            errors["translations"] = [Error.Validation("question.active_translations.required", "QuestionBank.ActiveTranslationsRequired")];
        }
    }

    private static void ValidateAnswerOptions(QuestionType type, IReadOnlyList<AnswerOptionRequest>? options, HashSet<int> questionLanguages, Dictionary<string, IReadOnlyList<Error>> errors)
    {
        options ??= [];
        switch (type)
        {
            case QuestionType.SingleChoice:
                if (options.Count != 4)
                {
                    errors["answerOptions"] = [Error.Validation("question.single_choice.option_count", "QuestionBank.SingleChoiceOptionCount")];
                }
                if (options.Count(x => x.IsCorrect) != 1)
                {
                    errors["answerOptions"] = [Error.Validation("question.single_choice.correct_count", "QuestionBank.SingleChoiceCorrectCount")];
                }
                ValidateOptionTranslations(options, questionLanguages, requireMatchPair: false, errors);
                break;
            case QuestionType.Matching:
                if (options.Count != 4)
                {
                    errors["answerOptions"] = [Error.Validation("question.matching.pair_count", "QuestionBank.MatchingPairCount")];
                }
                ValidateOptionTranslations(options, questionLanguages, requireMatchPair: true, errors);
                foreach (var language in questionLanguages)
                {
                    var rights = options.SelectMany(o => o.Translations.Where(t => t.Language == language)).Select(t => Normalize(t.MatchPairText)).ToList();
                    if (rights.Count != rights.Distinct(StringComparer.OrdinalIgnoreCase).Count())
                    {
                        errors[$"answerOptions.{language}.matchPairText"] = [Error.Validation("question.matching.right_duplicate", "QuestionBank.MatchingRightDuplicate")];
                    }
                }
                break;
            case QuestionType.ClosedAnswer:
                if (options.Count != 1 || !options[0].IsCorrect)
                {
                    errors["answerOptions"] = [Error.Validation("question.closed_answer.option_count", "QuestionBank.ClosedAnswerCanonicalCount")];
                }
                ValidateOptionTranslations(options, questionLanguages, requireMatchPair: false, errors);
                break;
        }
    }

    private static void ValidateOptionTranslations(IReadOnlyList<AnswerOptionRequest> options, HashSet<int> questionLanguages, bool requireMatchPair, Dictionary<string, IReadOnlyList<Error>> errors)
    {
        for (var i = 0; i < options.Count; i++)
        {
            foreach (var language in questionLanguages)
            {
                var translation = options[i].Translations.SingleOrDefault(x => x.Language == language);
                if (translation is null)
                {
                    errors[$"answerOptions[{i}].translations"] = [Error.Validation("question.answer_translation.missing", "QuestionBank.AnswerTranslationMissing")];
                    continue;
                }

                if (string.IsNullOrWhiteSpace(translation.Text))
                {
                    errors[$"answerOptions[{i}].translations.{language}.text"] = [Error.Validation("question.answer_text.required", "Validation.Required")];
                }

                if (requireMatchPair && string.IsNullOrWhiteSpace(translation.MatchPairText))
                {
                    errors[$"answerOptions[{i}].translations.{language}.matchPairText"] = [Error.Validation("question.match_pair.required", "QuestionBank.MatchPairRequired")];
                }
            }
        }
    }

    private static string Normalize(string? value) => value?.Trim().ToLowerInvariant() ?? string.Empty;
}
