using Adeeb.Application.Abstractions.Localization;
using Adeeb.Modules.AcademicCatalog.Contracts;
using Adeeb.Modules.AcademicCatalog.Domain;
using Adeeb.SharedKernel.Errors;
using Adeeb.SharedKernel.Results;

namespace Adeeb.Modules.AcademicCatalog.Application;

internal static class Validation
{
    public static Result ValidateSubject(SubjectUpsertRequest request) =>
        ValidateCommon(request.Code, request.Status, request.Translations, allowIcon: string.IsNullOrWhiteSpace(request.IconUrl) || request.IconUrl.Length <= 512);

    public static Result ValidateTopic(TopicUpsertRequest request)
    {
        var result = ValidateCommon(request.Code, request.Status, request.Translations, allowIcon: true);
        if (result.IsFailure)
        {
            return result;
        }

        return request.SubjectId == Guid.Empty
            ? Result.ValidationFailure(new Dictionary<string, IReadOnlyList<Error>>
            {
                ["subjectId"] = [Error.Validation("academic.subject_id.required", "Validation.Required")]
            })
            : Result.Success();
    }

    public static bool TryParseStatus(int? status, out AcademicItemStatus parsed)
    {
        parsed = AcademicItemStatus.Draft;
        if (status is null)
        {
            return true;
        }

        if (!Enum.IsDefined(typeof(AcademicItemStatus), status.Value))
        {
            return false;
        }

        parsed = (AcademicItemStatus)status.Value;
        return true;
    }

    public static bool TryParseLanguage(int language, out SupportedLanguage parsed)
    {
        parsed = SupportedLanguage.Tajik;
        if (!Enum.IsDefined(typeof(SupportedLanguage), language))
        {
            return false;
        }

        parsed = (SupportedLanguage)language;
        return true;
    }

    private static Result ValidateCommon(string? code, int status, IReadOnlyList<TranslationRequest>? translations, bool allowIcon)
    {
        var errors = new Dictionary<string, IReadOnlyList<Error>>(StringComparer.OrdinalIgnoreCase);
        if (string.IsNullOrWhiteSpace(code) || code.Trim().Length > 80)
        {
            errors["code"] = [Error.Validation("academic.code.required", "Validation.Required")];
        }

        if (!TryParseStatus(status, out var parsedStatus))
        {
            errors["status"] = [Error.Validation("academic.status.invalid", "Validation.InvalidStatus")];
        }

        if (!allowIcon)
        {
            errors["iconUrl"] = [Error.Validation("academic.icon_url.invalid", "Validation.InvalidUrl")];
        }

        if (translations is null || translations.Count == 0)
        {
            errors["translations"] = [Error.Validation("academic.translations.required", "Validation.Required")];
        }
        else
        {
            var languages = new HashSet<SupportedLanguage>();
            for (var i = 0; i < translations.Count; i++)
            {
                var translation = translations[i];
                if (!TryParseLanguage(translation.Language, out var language))
                {
                    errors[$"translations[{i}].language"] = [Error.Validation("academic.language.unsupported", "Validation.UnsupportedLanguage")];
                    continue;
                }

                if (!languages.Add(language))
                {
                    errors[$"translations[{i}].language"] = [Error.Validation("academic.language.duplicate", "Validation.DuplicateLanguage")];
                }

                if (string.IsNullOrWhiteSpace(translation.Name) || translation.Name.Trim().Length > 160)
                {
                    errors[$"translations[{i}].name"] = [Error.Validation("academic.name.required", "Validation.Required")];
                }
            }

            if (parsedStatus == AcademicItemStatus.Active &&
                (!languages.Contains(SupportedLanguage.Tajik) || !languages.Contains(SupportedLanguage.Russian)))
            {
                errors["translations"] = [Error.Validation("academic.active_translations.required", "Academic.ActiveTranslationsRequired")];
            }
        }

        return errors.Count == 0 ? Result.Success() : Result.ValidationFailure(errors);
    }
}
