using System.Globalization;
using Adeeb.Application.Abstractions.Localization;

namespace Adeeb.Infrastructure.Localization;

public sealed class StaticMessageLocalizer : IMessageLocalizer
{
    private static readonly Dictionary<string, Dictionary<string, string>> Messages = new(StringComparer.OrdinalIgnoreCase)
    {
        ["tg-TJ"] = new(StringComparer.OrdinalIgnoreCase)
        {
            ["Auth.InvalidCredentials"] = "Маълумоти воридшавӣ нодуруст аст",
            ["Auth.AccountBlocked"] = "Ҳисоб фаъол нест",
            ["Auth.SessionExpired"] = "Сессия ба анҷом расид",
            ["Auth.InvalidRefreshToken"] = "Сессияи воридшавӣ нодуруст аст",
            ["Auth.EmailAlreadyExists"] = "Ин почтаи электронӣ аллакай истифода мешавад",
            ["Auth.PhoneAlreadyExists"] = "Ин рақами телефон аллакай истифода мешавад",
            ["Auth.Forbidden"] = "Дастрасӣ манъ аст",
            ["Validation.Failed"] = "Маълумоти воридшуда нодуруст аст",
            ["Validation.Required"] = "Ин майдон ҳатмист",
            ["Validation.InvalidEmail"] = "Суроғаи почтаи электронӣ нодуруст аст",
            ["Validation.UnsupportedLanguage"] = "Забони интихобшуда дастгирӣ намешавад",
            ["Validation.PasswordPolicy"] = "Парол бояд ҳадди ақал 8 аломат, ҳарфи калон, ҳарфи хурд ва рақам дошта бошад",
            ["Common.NotFound"] = "Ёфт нашуд",
            ["Common.Forbidden"] = "Дастрасӣ манъ аст",
            ["Common.UnexpectedError"] = "Хатои ғайричашмдошт рух дод",
            ["RateLimit.TooManyRequests"] = "Дархостҳо аз ҳад зиёданд"
        },
        ["ru-RU"] = new(StringComparer.OrdinalIgnoreCase)
        {
            ["Auth.InvalidCredentials"] = "Неверные данные для входа",
            ["Auth.AccountBlocked"] = "Учетная запись недоступна",
            ["Auth.SessionExpired"] = "Сессия истекла",
            ["Auth.InvalidRefreshToken"] = "Недействительная сессия входа",
            ["Auth.EmailAlreadyExists"] = "Этот адрес электронной почты уже используется",
            ["Auth.PhoneAlreadyExists"] = "Этот номер телефона уже используется",
            ["Auth.Forbidden"] = "Доступ запрещен",
            ["Validation.Failed"] = "Введенные данные недействительны",
            ["Validation.Required"] = "Это поле обязательно",
            ["Validation.InvalidEmail"] = "Адрес электронной почты недействителен",
            ["Validation.UnsupportedLanguage"] = "Выбранный язык не поддерживается",
            ["Validation.PasswordPolicy"] = "Пароль должен содержать минимум 8 символов, заглавную букву, строчную букву и цифру",
            ["Common.NotFound"] = "Не найдено",
            ["Common.Forbidden"] = "Доступ запрещен",
            ["Common.UnexpectedError"] = "Произошла непредвиденная ошибка",
            ["RateLimit.TooManyRequests"] = "Слишком много запросов"
        },
        ["en-US"] = new(StringComparer.OrdinalIgnoreCase)
        {
            ["Auth.InvalidCredentials"] = "Invalid login credentials",
            ["Auth.AccountBlocked"] = "Account is not available",
            ["Auth.SessionExpired"] = "Session has expired",
            ["Auth.InvalidRefreshToken"] = "Invalid login session",
            ["Auth.EmailAlreadyExists"] = "This email address is already in use",
            ["Auth.PhoneAlreadyExists"] = "This phone number is already in use",
            ["Auth.Forbidden"] = "Access is forbidden",
            ["Validation.Failed"] = "The submitted data is invalid",
            ["Validation.Required"] = "This field is required",
            ["Validation.InvalidEmail"] = "Email address is invalid",
            ["Validation.UnsupportedLanguage"] = "Selected language is not supported",
            ["Validation.PasswordPolicy"] = "Password must be at least 8 characters and include uppercase, lowercase, and a digit",
            ["Common.NotFound"] = "Not found",
            ["Common.Forbidden"] = "Access is forbidden",
            ["Common.UnexpectedError"] = "An unexpected error occurred",
            ["RateLimit.TooManyRequests"] = "Too many requests"
        }
    };

    public string this[string key]
    {
        get
        {
            var culture = CultureInfo.CurrentUICulture.Name;
            if (Messages.TryGetValue(culture, out var localized) && localized.TryGetValue(key, out var message))
            {
                return message;
            }

            return Messages["tg-TJ"].TryGetValue(key, out var fallback) ? fallback : key;
        }
    }
}
