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
        var errors = new Dictionary<string, List<Error>>(StringComparer.OrdinalIgnoreCase);
        void AddError(string key, Error error)
        {
            if (!errors.TryGetValue(key, out var list))
            {
                errors[key] = list = [];
            }
            list.Add(error);
        }

        if (request.SubjectId == Guid.Empty)
        {
            AddError("subjectId", Error.Validation("question.subject_id.required", "Validation.Required"));
        }

        QuestionType? type = null;
        if (Enum.IsDefined(typeof(QuestionType), request.Type))
        {
            type = (QuestionType)request.Type;
        }
        else
        {
            AddError("type", Error.Validation("question.type.invalid", "QuestionBank.InvalidType"));
        }

        if (!Enum.IsDefined(typeof(DifficultyLevel), request.Difficulty))
        {
            AddError("difficulty", Error.Validation("question.difficulty.invalid", "QuestionBank.InvalidDifficulty"));
        }

        QuestionStatus? status = null;
        if (Enum.IsDefined(typeof(QuestionStatus), request.Status))
        {
            status = (QuestionStatus)request.Status;
        }
        else
        {
            AddError("status", Error.Validation("question.status.invalid", "Validation.InvalidStatus"));
        }

        var validLanguages = ValidateQuestionTranslations(request.Translations, status, AddError);

        if (type.HasValue)
        {
            ValidateAnswerOptions(type.Value, request.AnswerOptions, validLanguages, AddError);
        }

        return errors.Count == 0 ? Result.Success() : Result.ValidationFailure(errors.ToDictionary(k => k.Key, v => (IReadOnlyList<Error>)v.Value));
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

    private static HashSet<SupportedLanguage> ValidateQuestionTranslations(IReadOnlyList<QuestionTranslationRequest>? translations, QuestionStatus? status, Action<string, Error> addError)
    {
        var languages = new HashSet<SupportedLanguage>();
        if (translations is null || translations.Count == 0)
        {
            addError("translations", Error.Validation("question.translations.required", "Validation.Required"));
            return languages;
        }

        for (var i = 0; i < translations.Count; i++)
        {
            var item = translations[i];
            if (!TryParseLanguage(item.Language, out var language))
            {
                addError($"translations[{i}].language", Error.Validation("question.language.unsupported", "Validation.UnsupportedLanguage"));
                continue;
            }

            if (!languages.Add(language))
            {
                addError($"translations[{i}].language", Error.Validation("question.language.duplicate", "Validation.DuplicateLanguage"));
            }

            if (string.IsNullOrWhiteSpace(item.Content))
            {
                addError($"translations[{i}].content", Error.Validation("question.content.required", "Validation.Required"));
            }
        }

        if (status == QuestionStatus.Active && (!languages.Contains(SupportedLanguage.Tajik) || !languages.Contains(SupportedLanguage.Russian)))
        {
            addError("translations", Error.Validation("question.active_translations.required", "QuestionBank.ActiveTranslationsRequired"));
        }

        return languages;
    }

    private static void ValidateAnswerOptions(QuestionType type, IReadOnlyList<AnswerOptionRequest>? options, HashSet<SupportedLanguage> questionLanguages, Action<string, Error> addError)
    {
        options ??= [];
        switch (type)
        {
            case QuestionType.SingleChoice:
                if (options.Count != 4)
                {
                    addError("answerOptions", Error.Validation("question.single_choice.option_count", "QuestionBank.SingleChoiceOptionCount"));
                }
                if (options.Count(x => x.IsCorrect) != 1)
                {
                    addError("answerOptions", Error.Validation("question.single_choice.correct_count", "QuestionBank.SingleChoiceCorrectCount"));
                }
                ValidateOptionTranslations(options, questionLanguages, requireMatchPair: false, addError);
                break;
            case QuestionType.Matching:
                if (options.Count != 4)
                {
                    addError("answerOptions", Error.Validation("question.matching.pair_count", "QuestionBank.MatchingPairCount"));
                }
                ValidateOptionTranslations(options, questionLanguages, requireMatchPair: true, addError);
                foreach (var language in questionLanguages)
                {
                    var languageValue = (int)language;
                    var rights = options.SelectMany(o => o.Translations.Where(t => t.Language == languageValue))
                                        .Select(t => t.MatchPairText)
                                        .Where(text => !string.IsNullOrWhiteSpace(text))
                                        .Select(Normalize)
                                        .ToList();
                    if (rights.Count != rights.Distinct(StringComparer.OrdinalIgnoreCase).Count())
                    {
                        addError($"answerOptions.{language}.matchPairText", Error.Validation("question.matching.right_duplicate", "QuestionBank.MatchingRightDuplicate"));
                    }
                }
                break;
            case QuestionType.ClosedAnswer:
                if (options.Count != 1 || !options[0].IsCorrect)
                {
                    addError("answerOptions", Error.Validation("question.closed_answer.option_count", "QuestionBank.ClosedAnswerCanonicalCount"));
                }
                ValidateOptionTranslations(options, questionLanguages, requireMatchPair: false, addError);
                break;
        }
    }

    private static void ValidateOptionTranslations(IReadOnlyList<AnswerOptionRequest> options, HashSet<SupportedLanguage> questionLanguages, bool requireMatchPair, Action<string, Error> addError)
    {
        for (var i = 0; i < options.Count; i++)
        {
            foreach (var language in questionLanguages)
            {
                var languageValue = (int)language;
                var translation = options[i].Translations.SingleOrDefault(x => x.Language == languageValue);
                if (translation is null)
                {
                    addError($"answerOptions[{i}].translations", Error.Validation("question.answer_translation.missing", "QuestionBank.AnswerTranslationMissing"));
                    continue;
                }

                if (string.IsNullOrWhiteSpace(translation.Text))
                {
                    addError($"answerOptions[{i}].translations.{language}.text", Error.Validation("question.answer_text.required", "Validation.Required"));
                }

                if (requireMatchPair && string.IsNullOrWhiteSpace(translation.MatchPairText))
                {
                    addError($"answerOptions[{i}].translations.{language}.matchPairText", Error.Validation("question.match_pair.required", "QuestionBank.MatchPairRequired"));
                }
            }
        }
    }

    private static string Normalize(string? value) => value?.Trim().ToLowerInvariant() ?? string.Empty;
}
