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

        if (!Enum.IsDefined(typeof(QuestionType), request.Type))
        {
            AddError("type", Error.Validation("question.type.invalid", "QuestionBank.InvalidType"));
        }
        var type = (QuestionType)request.Type;

        if (!Enum.IsDefined(typeof(DifficultyLevel), request.Difficulty))
        {
            AddError("difficulty", Error.Validation("question.difficulty.invalid", "QuestionBank.InvalidDifficulty"));
        }

        if (!Enum.IsDefined(typeof(QuestionStatus), request.Status))
        {
            AddError("status", Error.Validation("question.status.invalid", "Validation.InvalidStatus"));
        }
        var status = (QuestionStatus)request.Status;

        ValidateQuestionTranslations(request.Translations, status, AddError);
        
        if (Enum.IsDefined(typeof(QuestionType), request.Type))
        {
            ValidateAnswerOptions(type, request.AnswerOptions, request.Translations?.Select(x => x.Language).ToHashSet() ?? [], AddError);
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

    private static void ValidateQuestionTranslations(IReadOnlyList<QuestionTranslationRequest>? translations, QuestionStatus status, Action<string, Error> addError)
    {
        if (translations is null || translations.Count == 0)
        {
            addError("translations", Error.Validation("question.translations.required", "Validation.Required"));
            return;
        }

        var languages = new HashSet<SupportedLanguage>();
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
    }

    private static void ValidateAnswerOptions(QuestionType type, IReadOnlyList<AnswerOptionRequest>? options, HashSet<int> questionLanguages, Action<string, Error> addError)
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
                    var rights = options.SelectMany(o => o.Translations.Where(t => t.Language == language))
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

    private static void ValidateOptionTranslations(IReadOnlyList<AnswerOptionRequest> options, HashSet<int> questionLanguages, bool requireMatchPair, Action<string, Error> addError)
    {
        for (var i = 0; i < options.Count; i++)
        {
            foreach (var language in questionLanguages)
            {
                var translation = options[i].Translations.SingleOrDefault(x => x.Language == language);
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
