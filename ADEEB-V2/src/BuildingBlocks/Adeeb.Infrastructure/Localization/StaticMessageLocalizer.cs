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
            ["Student.Activity.InvalidTimeZone"] = "Минтақаи вақт нодуруст аст",
            ["Student.Activity.InvalidPeriod"] = "Сол ё моҳи интихобшуда нодуруст аст",
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
            ["Student.Activity.InvalidTimeZone"] = "Указан некорректный часовой пояс",
            ["Student.Activity.InvalidPeriod"] = "Указан некорректный год или месяц",
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
            ["Student.Activity.InvalidTimeZone"] = "The time zone is invalid",
            ["Student.Activity.InvalidPeriod"] = "The selected year or month is invalid",
            ["RateLimit.TooManyRequests"] = "Too many requests"
        }
    };

    private static readonly Dictionary<string, Dictionary<string, string>> ContentMessages = new(StringComparer.OrdinalIgnoreCase)
    {
        ["tg-TJ"] = new(StringComparer.OrdinalIgnoreCase)
        {
            ["Validation.InvalidStatus"] = "Статус нодуруст аст",
            ["Validation.InvalidUrl"] = "URL нодуруст аст",
            ["Validation.DuplicateLanguage"] = "Забон такрор шудааст",
            ["Academic.SubjectNotFound"] = "Фан ёфт нашуд",
            ["Academic.TopicNotFound"] = "Мавзӯъ ёфт нашуд",
            ["Academic.SubjectCodeExists"] = "Рамзи фан аллакай вуҷуд дорад",
            ["Academic.TopicCodeExists"] = "Рамзи мавзӯъ барои ин фан аллакай вуҷуд дорад",
            ["Academic.ActiveTranslationsRequired"] = "Барои фаъол кардан тарҷумаҳои тоҷикӣ ва русӣ ҳатмӣ мебошанд",
            ["QuestionBank.QuestionNotFound"] = "Савол ёфт нашуд",
            ["QuestionBank.InvalidType"] = "Навъи савол нодуруст аст",
            ["QuestionBank.InvalidDifficulty"] = "Дараҷаи душворӣ нодуруст аст",
            ["QuestionBank.ActiveTranslationsRequired"] = "Барои саволи фаъол тарҷумаҳои тоҷикӣ ва русӣ ҳатмӣ мебошанд",
            ["QuestionBank.SingleChoiceOptionCount"] = "Саволи якҷавоба бояд маҳз 4 вариант дошта бошад",
            ["QuestionBank.SingleChoiceCorrectCount"] = "Саволи якҷавоба бояд танҳо 1 ҷавоби дуруст дошта бошад",
            ["QuestionBank.MatchingPairCount"] = "Саволи мувофиқат бояд маҳз 4 ҷуфт дошта бошад",
            ["QuestionBank.MatchingRightDuplicate"] = "Қиматҳои тарафи рости мувофиқат набояд такрор шаванд",
            ["QuestionBank.MatchPairRequired"] = "Матни ҷуфти мувофиқат ҳатмӣ аст",
            ["QuestionBank.AnswerTranslationMissing"] = "Тарҷумаи варианти ҷавоб ҳатмӣ аст",
            ["QuestionBank.ClosedAnswerCanonicalCount"] = "Саволи ҷавоби пӯшида бояд як ҷавоби дурусти асосӣ дошта бошад",
            ["QuestionBank.InvalidFormJson"] = "JSON-и form нодуруст аст",
            ["QuestionBank.InvalidImageType"] = "Навъи расми савол дастгирӣ намешавад",
            ["QuestionBank.ImageTooLarge"] = "Расми савол аз ҳад калон аст",
            ["Student.NotFound"] = "Профили донишҷӯ ёфт нашуд",
            ["Student.AlreadyExists"] = "Профили донишҷӯ аллакай вуҷуд дорад",
            ["Student.ProvisioningUnavailable"] = "Эҷоди профили донишҷӯ муваққатан дастнорас аст",
            ["Student.ProvisioningRequired"] = "Эҷоди профили донишҷӯ лозим аст",
            ["Student.Suspended"] = "Профили донишҷӯ боздошта шудааст",
            ["Student.Closed"] = "Профили донишҷӯ баста шудааст",
            ["Student.InvalidStatusTransition"] = "Гузариши ҳолати донишҷӯ нодуруст аст",
            ["Student.InvalidStatus"] = "Ҳолати донишҷӯ нодуруст аст",
            ["Student.Profile.Invalid"] = "Маълумоти профили донишҷӯ нодуруст аст",
            ["Student.Profile.InvalidDateOfBirth"] = "Санаи таваллуд нодуруст аст",
            ["Student.Profile.InvalidGrade"] = "Синф нодуруст аст",
            ["Student.Profile.InvalidGender"] = "Ҷинс нодуруст аст",
            ["Student.Profile.DateOfBirthLocked"] = "Санаи таваллуд баъди сабт дигар тағйир дода намешавад",
            ["Student.Avatar.Invalid"] = "Расми профил нодуруст аст ё аз 10 MB калон аст",
            ["Student.Region.NotFound"] = "Минтақа ёфт нашуд",
            ["Student.Region.Inactive"] = "Минтақа ғайрифаъол аст",
            ["Student.Region.HierarchyInvalid"] = "Сохтори минтақа нодуруст аст",
            ["Student.Region.InUse"] = "Минтақа дорои зерминтақа ё мактаби фаъол аст",
            ["Student.Region.Duplicate"] = "Минтақа бо чунин ном аллакай вуҷуд дорад",
            ["Student.Region.Invalid"] = "Маълумоти минтақа нодуруст аст",
            ["Student.School.NotFound"] = "Мактаб ёфт нашуд",
            ["Student.School.NotSelectable"] = "Ин мактаб барои интихоб дастрас нест",
            ["Student.School.Duplicate"] = "Чунин мактаб ё минтақа аллакай вуҷуд дорад",
            ["Student.School.MergeInvalid"] = "Якҷоякунии мактабҳо иҷозат нест",
            ["Student.School.Invalid"] = "Маълумоти мактаб нодуруст аст",
            ["Student.School.InvalidStatus"] = "Ҳолати мактаб нодуруст аст",
            ["Student.School.InvalidType"] = "Навъи мактаб нодуруст аст",
            ["Student.Education.Invalid"] = "Маълумоти таҳсил нодуруст аст",
            ["Student.Education.ConcurrencyConflict"] = "Маълумот тағйир ёфт; саҳифаро нав кунед",
            ["Student.SchoolSuggestion.NotFound"] = "Пешниҳоди мактаб ёфт нашуд",
            ["Student.SchoolSuggestion.Invalid"] = "Маълумоти пешниҳоди мактаб нодуруст аст",
            ["Student.SchoolSuggestion.ReviewInvalid"] = "Баррасии пешниҳоди мактаб нодуруст аст",
            ["Student.EducationImport.Invalid"] = "Файли import-и мактабҳо нодуруст аст",
            ["Student.EducationImport.Conflict"] = "Файли import бо маълумоти ҷорӣ мухолифат дорад; preview-ро аз нав гиред",
            ["Student.Rollover.NotFound"] = "Гузариши соли таҳсил ёфт нашуд",
            ["Student.Rollover.Invalid"] = "Гузариши соли таҳсил иҷозат нест",
            ["Common.InvalidPagination"] = "Параметрҳои саҳифабандӣ нодуруст аст",
            ["Commerce.StudentRequired"] = "Профили фаъоли донишҷӯ барои commerce лозим аст",
            ["Commerce.StudentNotFound"] = "Профили фаъоли донишҷӯ ёфт нашуд",
            ["Commerce.EntitlementNotFound"] = "Ҳуқуқи commerce ёфт нашуд",
            ["Commerce.IdempotencyKey.Invalid"] = "Калиди idempotency нодуруст аст",
            ["Commerce.IdempotencyKey.InUse"] = "Калиди idempotency аллакай истифода шудааст",
            ["Commerce.ExpiresAt.Invalid"] = "Мӯҳлати анҷом бояд баъд аз оғоз бошад",
            ["Commerce.RevokeReason.Invalid"] = "Сабаби бекоркунӣ нодуруст аст",
            ["Commerce.TariffNotFound"] = "Тариф ёфт нашуд",
            ["Commerce.ReceiptNotFound"] = "Чеки пардохт ёфт нашуд",
            ["Commerce.ReceiptAlreadyReviewed"] = "Чеки пардохт аллакай санҷида шудааст",
            ["Commerce.ReceiptConcurrencyConflict"] = "Чеки пардохт ҳамзамон аз тарафи дигар санҷида шуд",
            ["Commerce.EntitlementAlreadyCreated"] = "Дастрасӣ барои ин чек аллакай сохта шудааст",
            ["Commerce.ReviewerRequired"] = "Маълумоти санҷанда лозим аст",
            ["Commerce.Tariff.Name.Invalid"] = "Номи тариф нодуруст аст",
            ["Commerce.Tariff.Price.Invalid"] = "Нархи тариф нодуруст аст",
            ["Commerce.Tariff.Currency.Invalid"] = "Асъори тариф нодуруст аст",
            ["Commerce.Tariff.Currency.Unsupported"] = "Асъори тариф дастгирӣ намешавад",
            ["Commerce.Tariff.Duration.Invalid"] = "Давомнокии тариф нодуруст аст",
            ["Commerce.Tariff.QrImage.Required"] = "Расми QR-код лозим аст",
            ["Commerce.Tariff.Status.Invalid"] = "Ҳолати тариф нодуруст аст",
            ["Commerce.Receipt.Image.Required"] = "Расми чек лозим аст",
            ["Commerce.ReviewNote.Invalid"] = "Эзоҳи санҷиш нодуруст аст",
            ["Commerce.QrImage.InvalidType"] = "Навъи расми QR-код нодуруст аст",
            ["Commerce.Receipt.Image.InvalidType"] = "Навъи расми чек нодуруст аст",
            ["Commerce.Image.TooLarge"] = "Андозаи расм аз ҳад зиёд аст",
            ["Commerce.Receipt.Image.Corrupted"] = "Расми чек вайрон аст",
            ["Commerce.Image.Dimensions.Invalid"] = "Андозаҳои расм аз ҳад зиёданд",
            ["Commerce.Receipt.ImageNotFound"] = "Расми чек ёфт нашуд",
            ["Commerce.Receipt.Status.Invalid"] = "Ҳолати чек нодуруст аст",
            ["Pagination.Limit.Invalid"] = "Ҳадди саҳифа нодуруст аст",
            ["Pagination.Cursor.Invalid"] = "Курсори саҳифа нодуруст аст",
            ["DateRange.Invalid"] = "Фосилаи сана нодуруст аст",
            ["Idempotency.PayloadMismatch"] = "Калиди такрорӣ бо маълумоти дигар истифода шуд"
        },
        ["ru-RU"] = new(StringComparer.OrdinalIgnoreCase)
        {
            ["Validation.InvalidStatus"] = "Статус недействителен",
            ["Validation.InvalidUrl"] = "URL недействителен",
            ["Validation.DuplicateLanguage"] = "Язык уже добавлен",
            ["Academic.SubjectNotFound"] = "Предмет не найден",
            ["Academic.TopicNotFound"] = "Тема не найдена",
            ["Academic.SubjectCodeExists"] = "Код предмета уже существует",
            ["Academic.TopicCodeExists"] = "Код темы для этого предмета уже существует",
            ["Academic.ActiveTranslationsRequired"] = "Для активации требуются переводы на таджикский и русский языки",
            ["QuestionBank.QuestionNotFound"] = "Вопрос не найден",
            ["QuestionBank.InvalidType"] = "Недопустимый тип вопроса",
            ["QuestionBank.InvalidDifficulty"] = "Недопустимый уровень сложности вопроса",
            ["QuestionBank.ActiveTranslationsRequired"] = "Для активных вопросов требуются переводы на таджикский и русский языки",
            ["QuestionBank.SingleChoiceOptionCount"] = "Вопрос с одним вариантом ответа должен содержать ровно 4 варианта",
            ["QuestionBank.SingleChoiceCorrectCount"] = "Вопрос с одним вариантом ответа должен иметь только 1 правильный ответ",
            ["QuestionBank.MatchingPairCount"] = "Вопрос на соответствие должен содержать ровно 4 пары",
            ["QuestionBank.MatchingRightDuplicate"] = "Значения правой стороны соответствия не должны повторяться",
            ["QuestionBank.MatchPairRequired"] = "Текст пары соответствия обязателен",
            ["QuestionBank.AnswerTranslationMissing"] = "Перевод варианта ответа обязателен",
            ["QuestionBank.ClosedAnswerCanonicalCount"] = "Вопрос с закрытым ответом должен иметь один основной правильный ответ",
            ["QuestionBank.InvalidFormJson"] = "JSON формы недействителен",
            ["QuestionBank.InvalidImageType"] = "Тип изображения вопроса не поддерживается",
            ["QuestionBank.ImageTooLarge"] = "Изображение вопроса слишком большое",
            ["Student.NotFound"] = "Профиль ученика не найден",
            ["Student.AlreadyExists"] = "Профиль ученика уже существует",
            ["Student.ProvisioningUnavailable"] = "Создание профиля ученика временно недоступно",
            ["Student.ProvisioningRequired"] = "Требуется создание профиля ученика",
            ["Student.Suspended"] = "Профиль ученика приостановлен",
            ["Student.Closed"] = "Профиль ученика закрыт",
            ["Student.InvalidStatusTransition"] = "Переход статуса ученика недопустим",
            ["Student.InvalidStatus"] = "Статус ученика недействителен",
            ["Student.Profile.Invalid"] = "Значение профиля ученика недействительно",
            ["Student.Profile.InvalidDateOfBirth"] = "Дата рождения недействительна",
            ["Student.Profile.InvalidGrade"] = "Класс недействителен",
            ["Student.Profile.InvalidGender"] = "Пол указан неверно",
            ["Student.Profile.DateOfBirthLocked"] = "Дату рождения нельзя изменить после сохранения",
            ["Student.Avatar.Invalid"] = "Фото профиля некорректно или превышает 10 МБ",
            ["Student.Region.NotFound"] = "Регион не найден",
            ["Student.Region.Inactive"] = "Регион неактивен",
            ["Student.Region.HierarchyInvalid"] = "Структура регионов недопустима",
            ["Student.Region.InUse"] = "В регионе есть активные подразделения или школы",
            ["Student.Region.Duplicate"] = "Регион с таким названием уже существует",
            ["Student.Region.Invalid"] = "Данные региона недействительны",
            ["Student.School.NotFound"] = "Школа не найдена",
            ["Student.School.NotSelectable"] = "Эта школа недоступна для выбора",
            ["Student.School.Duplicate"] = "Такая школа или регион уже существует",
            ["Student.School.MergeInvalid"] = "Объединение школ недопустимо",
            ["Student.School.Invalid"] = "Данные школы недействительны",
            ["Student.School.InvalidStatus"] = "Статус школы недействителен",
            ["Student.School.InvalidType"] = "Тип школы недействителен",
            ["Student.Education.Invalid"] = "Данные обучения недействительны",
            ["Student.Education.ConcurrencyConflict"] = "Данные изменились; обновите страницу",
            ["Student.SchoolSuggestion.NotFound"] = "Предложенная школа не найдена",
            ["Student.SchoolSuggestion.Invalid"] = "Данные предложения школы недействительны",
            ["Student.SchoolSuggestion.ReviewInvalid"] = "Проверка предложения школы недействительна",
            ["Student.EducationImport.Invalid"] = "Файл импорта школ недействителен",
            ["Student.EducationImport.Conflict"] = "Импорт конфликтует с текущими данными; выполните предпросмотр повторно",
            ["Student.Rollover.NotFound"] = "Переход учебного года не найден",
            ["Student.Rollover.Invalid"] = "Переход учебного года недопустим",
            ["Common.InvalidPagination"] = "Параметры пагинации недействительны",
            ["Commerce.StudentRequired"] = "Для commerce требуется активный профиль ученика",
            ["Commerce.StudentNotFound"] = "Активный профиль ученика не найден",
            ["Commerce.EntitlementNotFound"] = "Commerce entitlement не найден",
            ["Commerce.IdempotencyKey.Invalid"] = "Ключ идемпотентности недействителен",
            ["Commerce.IdempotencyKey.InUse"] = "Ключ идемпотентности уже используется",
            ["Commerce.ExpiresAt.Invalid"] = "Дата окончания должна быть после даты начала",
            ["Commerce.RevokeReason.Invalid"] = "Причина отзыва недействительна",
            ["Commerce.TariffNotFound"] = "Тариф не найден",
            ["Commerce.ReceiptNotFound"] = "Чек платежа не найден",
            ["Commerce.ReceiptAlreadyReviewed"] = "Чек платежа уже проверен",
            ["Commerce.ReceiptConcurrencyConflict"] = "Чек одновременно проверен другим пользователем",
            ["Commerce.EntitlementAlreadyCreated"] = "Доступ для этого чека уже создан",
            ["Commerce.ReviewerRequired"] = "Требуются данные проверяющего",
            ["Commerce.Tariff.Name.Invalid"] = "Название тарифа недействительно",
            ["Commerce.Tariff.Price.Invalid"] = "Цена тарифа недействительна",
            ["Commerce.Tariff.Currency.Invalid"] = "Валюта тарифа недействительна",
            ["Commerce.Tariff.Currency.Unsupported"] = "Валюта тарифа не поддерживается",
            ["Commerce.Tariff.Duration.Invalid"] = "Длительность тарифа недействительна",
            ["Commerce.Tariff.QrImage.Required"] = "Требуется изображение QR-кода",
            ["Commerce.Tariff.Status.Invalid"] = "Статус тарифа недействителен",
            ["Commerce.Receipt.Image.Required"] = "Требуется изображение чека",
            ["Commerce.ReviewNote.Invalid"] = "Комментарий проверки недействителен",
            ["Commerce.QrImage.InvalidType"] = "Тип изображения QR-кода недействителен",
            ["Commerce.Receipt.Image.InvalidType"] = "Тип изображения чека недействителен",
            ["Commerce.Image.TooLarge"] = "Размер изображения слишком большой",
            ["Commerce.Receipt.Image.Corrupted"] = "Изображение чека повреждено",
            ["Commerce.Image.Dimensions.Invalid"] = "Размеры изображения превышают допустимые",
            ["Commerce.Receipt.ImageNotFound"] = "Изображение чека не найдено",
            ["Commerce.Receipt.Status.Invalid"] = "Статус чека недействителен",
            ["Pagination.Limit.Invalid"] = "Лимит страницы недействителен",
            ["Pagination.Cursor.Invalid"] = "Курсор страницы недействителен",
            ["DateRange.Invalid"] = "Диапазон дат недействителен",
            ["Idempotency.PayloadMismatch"] = "Ключ идемпотентности использован с другими данными"
        },
        ["en-US"] = new(StringComparer.OrdinalIgnoreCase)
        {
            ["Validation.InvalidStatus"] = "Status is invalid",
            ["Validation.InvalidUrl"] = "URL is invalid",
            ["Validation.DuplicateLanguage"] = "Language already exists",
            ["Academic.SubjectNotFound"] = "Subject was not found",
            ["Academic.TopicNotFound"] = "Topic was not found",
            ["Academic.SubjectCodeExists"] = "Subject code already exists",
            ["Academic.TopicCodeExists"] = "Topic code already exists for this subject",
            ["Academic.ActiveTranslationsRequired"] = "Active items require Tajik and Russian translations",
            ["QuestionBank.QuestionNotFound"] = "Question was not found",
            ["QuestionBank.InvalidType"] = "Question type is invalid",
            ["QuestionBank.InvalidDifficulty"] = "Question difficulty level is invalid",
            ["QuestionBank.ActiveTranslationsRequired"] = "Active questions require Tajik and Russian translations",
            ["QuestionBank.SingleChoiceOptionCount"] = "Single choice questions must contain exactly 4 options",
            ["QuestionBank.SingleChoiceCorrectCount"] = "Single choice questions must have exactly 1 correct answer",
            ["QuestionBank.MatchingPairCount"] = "Matching questions must contain exactly 4 pairs",
            ["QuestionBank.MatchingRightDuplicate"] = "Matching right-side values must be unique",
            ["QuestionBank.MatchPairRequired"] = "Matching pair text is required",
            ["QuestionBank.AnswerTranslationMissing"] = "Answer option translation is required",
            ["QuestionBank.ClosedAnswerCanonicalCount"] = "Closed answer questions must have one canonical correct answer",
            ["QuestionBank.InvalidFormJson"] = "Form JSON is invalid",
            ["QuestionBank.InvalidImageType"] = "Question image type is not supported",
            ["QuestionBank.ImageTooLarge"] = "Question image is too large",
            ["Student.NotFound"] = "Student persona was not found",
            ["Student.AlreadyExists"] = "Student persona already exists",
            ["Student.ProvisioningUnavailable"] = "Student provisioning is temporarily unavailable",
            ["Student.ProvisioningRequired"] = "Student provisioning is required",
            ["Student.Suspended"] = "Student persona is suspended",
            ["Student.Closed"] = "Student persona is closed",
            ["Student.InvalidStatusTransition"] = "Student status transition is invalid",
            ["Student.InvalidStatus"] = "Student status is invalid",
            ["Student.Profile.Invalid"] = "Student profile value is invalid",
            ["Student.Profile.InvalidDateOfBirth"] = "Date of birth is invalid",
            ["Student.Profile.InvalidGrade"] = "Grade is invalid",
            ["Student.Profile.InvalidGender"] = "Gender is invalid",
            ["Student.Profile.DateOfBirthLocked"] = "Date of birth cannot be changed after it is saved",
            ["Student.Avatar.Invalid"] = "Profile photo is invalid or larger than 10 MB",
            ["Student.Region.NotFound"] = "Region was not found",
            ["Student.Region.Inactive"] = "Region is inactive",
            ["Student.Region.HierarchyInvalid"] = "Region hierarchy is invalid",
            ["Student.Region.InUse"] = "Region has active children or schools",
            ["Student.Region.Duplicate"] = "A region with this name already exists",
            ["Student.Region.Invalid"] = "Region data is invalid",
            ["Student.School.NotFound"] = "School was not found",
            ["Student.School.NotSelectable"] = "School is not available for selection",
            ["Student.School.Duplicate"] = "School or region already exists",
            ["Student.School.MergeInvalid"] = "School merge is invalid",
            ["Student.School.Invalid"] = "School data is invalid",
            ["Student.School.InvalidStatus"] = "School status is invalid",
            ["Student.School.InvalidType"] = "School type is invalid",
            ["Student.Education.Invalid"] = "Education data is invalid",
            ["Student.Education.ConcurrencyConflict"] = "Data changed; refresh the page",
            ["Student.SchoolSuggestion.NotFound"] = "School suggestion was not found",
            ["Student.SchoolSuggestion.Invalid"] = "School suggestion is invalid",
            ["Student.SchoolSuggestion.ReviewInvalid"] = "School suggestion review is invalid",
            ["Student.EducationImport.Invalid"] = "School import file is invalid",
            ["Student.EducationImport.Conflict"] = "Import conflicts with current data; preview it again",
            ["Student.Rollover.NotFound"] = "Academic-year rollover was not found",
            ["Student.Rollover.Invalid"] = "Academic-year rollover is invalid",
            ["Common.InvalidPagination"] = "Pagination parameters are invalid",
            ["Commerce.StudentRequired"] = "An active student persona is required for commerce",
            ["Commerce.StudentNotFound"] = "Active student persona was not found",
            ["Commerce.EntitlementNotFound"] = "Commerce entitlement was not found",
            ["Commerce.IdempotencyKey.Invalid"] = "Idempotency key is invalid",
            ["Commerce.IdempotencyKey.InUse"] = "Idempotency key is already in use",
            ["Commerce.ExpiresAt.Invalid"] = "Expiration must be after start",
            ["Commerce.RevokeReason.Invalid"] = "Revoke reason is invalid",
            ["Commerce.TariffNotFound"] = "Tariff was not found",
            ["Commerce.ReceiptNotFound"] = "Payment receipt was not found",
            ["Commerce.ReceiptAlreadyReviewed"] = "Payment receipt has already been reviewed",
            ["Commerce.ReceiptConcurrencyConflict"] = "Payment receipt was reviewed concurrently",
            ["Commerce.EntitlementAlreadyCreated"] = "Entitlement already exists for this payment receipt",
            ["Commerce.ReviewerRequired"] = "Reviewer identity is required",
            ["Commerce.Tariff.Name.Invalid"] = "Tariff name is invalid",
            ["Commerce.Tariff.Price.Invalid"] = "Tariff price is invalid",
            ["Commerce.Tariff.Currency.Invalid"] = "Tariff currency is invalid",
            ["Commerce.Tariff.Currency.Unsupported"] = "Tariff currency is not supported",
            ["Commerce.Tariff.Duration.Invalid"] = "Tariff duration is invalid",
            ["Commerce.Tariff.QrImage.Required"] = "QR image is required",
            ["Commerce.Tariff.Status.Invalid"] = "Tariff status is invalid",
            ["Commerce.Receipt.Image.Required"] = "Receipt image is required",
            ["Commerce.ReviewNote.Invalid"] = "Review note is invalid",
            ["Commerce.QrImage.InvalidType"] = "QR image type is invalid",
            ["Commerce.Receipt.Image.InvalidType"] = "Receipt image type is invalid",
            ["Commerce.Image.TooLarge"] = "Image is too large",
            ["Commerce.Receipt.Image.Corrupted"] = "Receipt image is corrupted",
            ["Commerce.Image.Dimensions.Invalid"] = "Image dimensions exceed the allowed limits",
            ["Commerce.Receipt.ImageNotFound"] = "Receipt image was not found",
            ["Commerce.Receipt.Status.Invalid"] = "Payment receipt status is invalid",
            ["Pagination.Limit.Invalid"] = "Page limit is invalid",
            ["Pagination.Cursor.Invalid"] = "Page cursor is invalid",
            ["DateRange.Invalid"] = "Date range is invalid",
            ["Idempotency.PayloadMismatch"] = "Idempotency key was used with a different payload"
        }
    };
    private static readonly Dictionary<string, Dictionary<string, string>> LocalizedMmtMessages = new(StringComparer.OrdinalIgnoreCase)
    {
        ["tg-TJ"] = new(StringComparer.OrdinalIgnoreCase)
        {
            ["MMT.ClusterNotFound"] = "Кластери ММТ ёфт нашуд",
            ["MMT.ClusterSubjectInvalid"] = "Як ё якчанд фани интихобшуда вуҷуд надорад ё фаъол нест",
            ["MMT.UniversityNotFound"] = "Донишгоҳ ёфт нашуд",
            ["MMT.SpecialtyNotFound"] = "Ихтисос ёфт нашуд",
            ["MMT.ProgramNotFound"] = "Барномаи қабул ёфт нашуд",
            ["MMT.ScoreNotFound"] = "Балли гузариш ёфт нашуд",
            ["MMT.ClusterCodeExists"] = "Рамзи кластер аллакай мавҷуд аст",
            ["MMT.UniversityExists"] = "Донишгоҳ аллакай мавҷуд аст",
            ["MMT.SpecialtyCodeExists"] = "Рамзи ихтисос аллакай мавҷуд аст",
            ["MMT.ProgramExists"] = "Барномаи қабул аллакай мавҷуд аст",
            ["MMT.ScoreExists"] = "Балли ин сол аллакай мавҷуд аст",
            ["MMT.ReferenceInactive"] = "Маълумоти вобастаи барнома бояд мавҷуд ва фаъол бошад",
            ["MMT.PublishInvalid"] = "Барномаро дар ҳолати ҷорӣ нашр кардан мумкин нест",
            ["MMT.ImportFileInvalid"] = "Файли воридот нодуруст аст",
            ["MMT.ImportExistingScore"] = "Файл балли гузариши мавҷударо дар бар мегирад",
            ["MMT.ImportConflict"] = "Маълумот ҳангоми воридот тағйир ёфт; пешнамоиши нав созед",
            ["MMT.YearInvalid"] = "Сол бояд аз 2000 то 2100 бошад",
            ["MMT.SeatsInvalid"] = "Шумораи ҷойҳо манфӣ буда наметавонад",
            ["MMT.ScoreInvalid"] = "Балли гузариш нодуруст аст",
            ["MMT.EnumInvalid"] = "Қимати интихобшуда нодуруст аст",
            ["MMT.ValueTooLong"] = "Қимат аз дарозии иҷозашуда зиёд аст",
            ["MMT.StudentProfileNotFound"] = "Профили фаъоли ММТ ёфт нашуд",
            ["MMT.EvaluationNotFound"] = "Арзёбии ММТ ёфт нашуд",
            ["MMT.AdmissionYearUnavailable"] = "Соли қабули дархостшуда ҳоло дастрас нест",
            ["MMT.GoalProgramInvalid"] = "Барномаи ҳадаф ба кластер ва соли қабул мувофиқ нест",
            ["MMT.ChoiceProgramInvalid"] = "Ҳар интихоб бояд барномаи фаъол ва нашршудаи кластеру соли профил бошад",
            ["MMT.TooManyChoices"] = "На бештар аз 12 интихоб иҷозат аст",
            ["MMT.DuplicateChoiceProgram"] = "Як барномаро ду бор интихоб кардан мумкин нест",
            ["MMT.DuplicateChoicePriority"] = "Афзалиятҳои интихоб бояд ягона бошанд",
            ["MMT.InvalidChoiceOrder"] = "Афзалиятҳо бояд пайдарпай аз 1 оғоз шаванд",
            ["MMT.ChoicesRequired"] = "Пеш аз симулятсия ақаллан як барномаро интихоб кунед",
            ["MMT.ProfileConflict"] = "Профил ҳамзамон тағйир ёфт; саҳифаро нав кунед",
            ["MMT.ChoiceUpdateConflict"] = "Интихобҳо ҳамзамон тағйир ёфтанд; саҳифаро нав кунед"
        },
        ["ru-RU"] = new(StringComparer.OrdinalIgnoreCase)
        {
            ["MMT.ClusterNotFound"] = "Кластер ММТ не найден",
            ["MMT.ClusterSubjectInvalid"] = "Один или несколько выбранных предметов не существуют или не активны",
            ["MMT.UniversityNotFound"] = "Университет не найден",
            ["MMT.SpecialtyNotFound"] = "Специальность не найдена",
            ["MMT.ProgramNotFound"] = "Программа поступления не найдена",
            ["MMT.ScoreNotFound"] = "Проходной балл не найден",
            ["MMT.ClusterCodeExists"] = "Код кластера уже существует",
            ["MMT.UniversityExists"] = "Университет уже существует",
            ["MMT.SpecialtyCodeExists"] = "Код специальности уже существует",
            ["MMT.ProgramExists"] = "Программа поступления уже существует",
            ["MMT.ScoreExists"] = "Балл за этот год уже существует",
            ["MMT.ReferenceInactive"] = "Связанные справочники программы должны существовать и быть активными",
            ["MMT.PublishInvalid"] = "Программу нельзя опубликовать в текущем состоянии",
            ["MMT.ImportFileInvalid"] = "Файл импорта некорректен",
            ["MMT.ImportExistingScore"] = "Импорт содержит существующий проходной балл",
            ["MMT.ImportConflict"] = "Данные изменились во время импорта; создайте новый предпросмотр",
            ["MMT.YearInvalid"] = "Год должен быть от 2000 до 2100",
            ["MMT.SeatsInvalid"] = "Количество мест не может быть отрицательным",
            ["MMT.ScoreInvalid"] = "Проходной балл некорректен",
            ["MMT.EnumInvalid"] = "Выбранное значение некорректно",
            ["MMT.ValueTooLong"] = "Значение превышает допустимую длину",
            ["MMT.StudentProfileNotFound"] = "Активный профиль ММТ не найден",
            ["MMT.EvaluationNotFound"] = "Оценивание ММТ не найдено",
            ["MMT.AdmissionYearUnavailable"] = "Запрошенный год поступления сейчас недоступен",
            ["MMT.GoalProgramInvalid"] = "Целевая программа не соответствует кластеру и году поступления",
            ["MMT.ChoiceProgramInvalid"] = "Каждый выбор должен быть активной опубликованной программой кластера и года профиля",
            ["MMT.TooManyChoices"] = "Разрешено не более 12 вариантов",
            ["MMT.DuplicateChoiceProgram"] = "Одну программу нельзя выбрать дважды",
            ["MMT.DuplicateChoicePriority"] = "Приоритеты вариантов должны быть уникальными",
            ["MMT.InvalidChoiceOrder"] = "Приоритеты должны идти подряд, начиная с 1",
            ["MMT.ChoicesRequired"] = "Перед симуляцией выберите хотя бы одну программу",
            ["MMT.ProfileConflict"] = "Профиль был изменён одновременно; обновите страницу",
            ["MMT.ChoiceUpdateConflict"] = "Варианты были изменены одновременно; обновите страницу"
        }
    };

    private static readonly Dictionary<string, Dictionary<string, string>> TestingMessages = new(StringComparer.OrdinalIgnoreCase)
    {
        ["tg-TJ"] = new(StringComparer.OrdinalIgnoreCase)
        {
            ["Testing.RedListNotEnough"] = "Барои машқ ҳадди ақал 20 саволи фаъоли рӯйхати хатоҳо лозим аст",
            ["Testing.NotEnoughQuestions"] = "Барои ин тест саволҳои фаъол кофӣ нестанд",
            ["Testing.AttemptNotFound"] = "Кӯшиши тестёбӣ ёфт нашуд",
            ["Testing.AttemptAlreadySubmitted"] = "Ин кӯшиш аллакай анҷом дода шудааст",
            ["Testing.AttemptExpired"] = "Вақти ин кӯшиш ба охир расидааст",
            ["Testing.AttemptRewardCalculationFailed"] = "Ҳисобкунии XP анҷом наёфт; кӯшиш тағйир дода нашуд",
            ["Testing.AttemptRewardConflict"] = "Мукофоти XP барои ин кӯшиш аллакай сабт шудааст",
            ["Testing.InvalidMode"] = "Реҷаи интихобшудаи тест нодуруст аст",
            ["Testing.ImmediateCheckNotAllowed"] = "Санҷиши фаврии ҷавоб барои ин навъи тест дастрас нест",
            ["Testing.QuestionNotInAttempt"] = "Савол ба ин кӯшиши тест дохил нест",
            ["Testing.AnswerRequired"] = "Пеш аз санҷидан ҷавобро интихоб кунед",
            ["Testing.InvalidQuestionCount"] = "Миқдори интихобшудаи саволҳо нодуруст аст",
            ["Testing.MmtProfileRequired"] = "Пеш аз оғози тест профили фаъоли ММТ созед",
            ["Testing.MmtChoicesRequired"] = "Пеш аз имтиҳони моҳона ҳамаи 12 комбинатсияи қабулро нигоҳ доред",
            ["Testing.MonthlyExamClosed"] = "Имтиҳони моҳона ҳоло дастрас нест",
            ["Testing.MonthlyExamAlreadyStarted"] = "Шумо дар ин равзана имтиҳони моҳонаро аллакай оғоз кардаед",
            ["Testing.RedListItemNotFound"] = "Савол дар рӯйхати хатоҳо ёфт нашуд"
        },
        ["ru-RU"] = new(StringComparer.OrdinalIgnoreCase)
        {
            ["Testing.RedListNotEnough"] = "Для практики требуется минимум 20 активных вопросов из списка ошибок",
            ["Testing.NotEnoughQuestions"] = "Недостаточно активных вопросов для этого теста",
            ["Testing.AttemptNotFound"] = "Попытка тестирования не найдена",
            ["Testing.AttemptAlreadySubmitted"] = "Эта попытка уже завершена",
            ["Testing.AttemptExpired"] = "Время этой попытки истекло",
            ["Testing.AttemptRewardCalculationFailed"] = "Не удалось рассчитать XP; попытка не была изменена",
            ["Testing.AttemptRewardConflict"] = "Награда XP для этой попытки уже зарегистрирована",
            ["Testing.InvalidMode"] = "Выбран недопустимый режим тестирования",
            ["Testing.ImmediateCheckNotAllowed"] = "Мгновенная проверка ответа недоступна для этого режима теста",
            ["Testing.QuestionNotInAttempt"] = "Вопрос не входит в эту попытку тестирования",
            ["Testing.AnswerRequired"] = "Выберите ответ перед проверкой",
            ["Testing.InvalidQuestionCount"] = "Выбрано недопустимое количество вопросов",
            ["Testing.MmtProfileRequired"] = "Перед началом теста создайте активный профиль ММТ",
            ["Testing.MmtChoicesRequired"] = "Перед месячным экзаменом сохраните все 12 комбинаций поступления",
            ["Testing.MonthlyExamClosed"] = "Месячный экзамен сейчас недоступен",
            ["Testing.MonthlyExamAlreadyStarted"] = "Вы уже начали месячный экзамен в этом окне",
            ["Testing.RedListItemNotFound"] = "Вопрос не найден в списке ошибок"
        },
        ["en-US"] = new(StringComparer.OrdinalIgnoreCase)
        {
            ["Testing.RedListNotEnough"] = "At least 20 active Red List questions are required",
            ["Testing.NotEnoughQuestions"] = "There are not enough active questions for this test",
            ["Testing.AttemptNotFound"] = "Test attempt was not found",
            ["Testing.AttemptAlreadySubmitted"] = "This test attempt has already been submitted",
            ["Testing.AttemptExpired"] = "This test attempt has expired",
            ["Testing.AttemptRewardCalculationFailed"] = "XP calculation failed; the attempt was not changed",
            ["Testing.AttemptRewardConflict"] = "The XP reward for this attempt has already been recorded",
            ["Testing.InvalidMode"] = "The selected test mode is invalid",
            ["Testing.ImmediateCheckNotAllowed"] = "Immediate answer checking is unavailable for this test mode",
            ["Testing.QuestionNotInAttempt"] = "The question does not belong to this test attempt",
            ["Testing.AnswerRequired"] = "Select an answer before checking",
            ["Testing.InvalidQuestionCount"] = "The selected question count is invalid",
            ["Testing.MmtProfileRequired"] = "Create an active MMT profile before starting this test",
            ["Testing.MmtChoicesRequired"] = "Save all 12 admission choices before starting the monthly exam",
            ["Testing.MonthlyExamClosed"] = "The monthly exam is not available now",
            ["Testing.MonthlyExamAlreadyStarted"] = "You have already started the monthly exam in this window",
            ["Testing.RedListItemNotFound"] = "The Red List item was not found"
        }
    };

    private static readonly Dictionary<string, string> ClusterLockedMessages = new(StringComparer.OrdinalIgnoreCase)
    {
        ["tg-TJ"] = "Кластери ММТ баъди оғози Роҳи Қабул тағйир дода намешавад",
        ["ru-RU"] = "Кластер ММТ нельзя изменить после начала Пути поступления",
        ["en-US"] = "The MMT cluster cannot be changed after the admission path has started"
    };

    private static readonly Dictionary<string, string> MmtMessages = new(StringComparer.OrdinalIgnoreCase)
    {
        ["MMT.ClusterNotFound"] = "MMT cluster was not found",
        ["MMT.ClusterSubjectInvalid"] = "One or more selected subjects do not exist or are inactive",
        ["MMT.UniversityNotFound"] = "University was not found",
        ["MMT.SpecialtyNotFound"] = "Specialty was not found",
        ["MMT.ProgramNotFound"] = "Admission program was not found",
        ["MMT.ScoreNotFound"] = "Passing score was not found",
        ["MMT.ClusterCodeExists"] = "MMT cluster code already exists",
        ["MMT.UniversityExists"] = "University already exists",
        ["MMT.SpecialtyCodeExists"] = "Specialty code already exists",
        ["MMT.ProgramExists"] = "Admission program already exists",
        ["MMT.ScoreExists"] = "Passing score already exists for this year",
        ["MMT.ReferenceInactive"] = "Admission program references must exist and be active",
        ["MMT.PublishInvalid"] = "Admission program cannot be published in its current state",
        ["MMT.ImportExistingScore"] = "Import contains an existing passing score",
        ["MMT.ImportConflict"] = "Import conflicted with another data change; retry with a fresh preview",
        ["MMT.YearInvalid"] = "Year must be between 2000 and 2100",
        ["MMT.SeatsInvalid"] = "Seats count cannot be negative",
        ["MMT.ScoreInvalid"] = "Passing score is invalid",
        ["MMT.EnumInvalid"] = "Selected value is invalid",
        ["MMT.ValueTooLong"] = "Value exceeds the allowed length",
        ["MMT.StudentProfileNotFound"] = "Active MMT profile was not found",
        ["MMT.EvaluationNotFound"] = "MMT evaluation was not found",
        ["MMT.AdmissionYearUnavailable"] = "The requested MMT admission year is not currently available",
        ["MMT.GoalProgramInvalid"] = "Goal admission program does not match the selected cluster and admission year",
        ["MMT.ChoiceProgramInvalid"] = "Every choice must be an active published program for the profile cluster and year",
        ["MMT.TooManyChoices"] = "At most 12 admission choices are allowed",
        ["MMT.DuplicateChoiceProgram"] = "An admission program cannot be selected more than once",
        ["MMT.DuplicateChoicePriority"] = "Admission choice priorities must be unique",
        ["MMT.InvalidChoiceOrder"] = "Admission choices must use consecutive priorities starting at 1",
        ["MMT.ChoicesRequired"] = "Select at least one admission choice before running the simulation",
        ["MMT.Accepted"] = "Your score reaches the threshold for one of your priority choices",
        ["MMT.NearMiss"] = "You are close to your target; focused progress can bridge the remaining gap",
        ["MMT.ProgressNeeded"] = "Keep building your score step by step toward your admission goal",
        ["MMT.NoThresholdData"] = "Passing-score data is not available yet; your progress has still been saved",
        ["MMT.ProfileConflict"] = "The MMT profile changed concurrently; reload and try again",
        ["MMT.ChoiceUpdateConflict"] = "Admission choices changed concurrently; reload and try again"
    };

    private static readonly Dictionary<string, Dictionary<string, string>> VocabularyMessages = new(StringComparer.OrdinalIgnoreCase)
    {
        ["tg-TJ"] = new(StringComparer.OrdinalIgnoreCase)
        {
            ["Vocabulary.NotFound"] = "Маълумоти луғат ёфт нашуд",
            ["Vocabulary.LanguageNotFound"] = "Забони омӯзишӣ ёфт нашуд",
            ["Vocabulary.TopicNotFound"] = "Мавзӯи луғат ёфт нашуд",
            ["Vocabulary.WordNotFound"] = "Калима ёфт нашуд",
            ["Vocabulary.QuestionNotFound"] = "Саволи луғат ёфт нашуд",
            ["Vocabulary.SessionNotFound"] = "Машқи луғат ёфт нашуд",
            ["Vocabulary.CourseRequired"] = "Аввал забони омӯзиширо интихоб кунед",
            ["Vocabulary.StudentRequired"] = "Профили донишҷӯ лозим аст",
            ["Vocabulary.StudentUnavailable"] = "Профили донишҷӯ дастрас нест",
            ["Progression.NotFound"] = "Маълумоти лига ёфт нашуд",
            ["Progression.LeagueInvalid"] = "Маълумоти лига нодуруст аст",
            ["Progression.ThresholdsInvalid"] = "Ҳудудҳои XP-и лигаҳо бояд пайдарпай ва бе такрор бошанд",
            ["Progression.StructuralLocked"] = "Дар вақти мавсими фаъол сохтори лигаҳоро тағйир додан мумкин нест",
            ["Progression.SeasonActive"] = "Мавсими лига аллакай фаъол аст",
            ["Progression.SeasonUnavailable"] = "Мавсими фаъоли лига дастрас нест",
            ["Progression.StudentUnavailable"] = "Донишҷӯ барои иштирок дар лига дастрас нест",
            ["Progression.AvatarInvalid"] = "Тасвири лига нодуруст ё аз 2 MB калон аст",
            ["Vocabulary.Duplicate"] = "Чунин маълумот аллакай вуҷуд дорад",
            ["Vocabulary.PublishInvalid"] = "Маълумоти луғат дар ҳолати ҷорӣ нашр шуда наметавонад",
            ["Vocabulary.NotEnoughQuestions"] = "Барои ин машқ саволҳои нашршуда кофӣ нестанд",
            ["Vocabulary.SessionCompleted"] = "Ин машқ аллакай анҷом ёфтааст",
            ["Vocabulary.AnswerLocked"] = "Ҷавоби қабулшуда дигар тағйир дода намешавад",
            ["Vocabulary.AnswersIncomplete"] = "Ба ҳамаи саволҳо ҷавоб диҳед",
            ["Vocabulary.Invalid"] = "Маълумоти воридшудаи луғат нодуруст аст"
        },
        ["ru-RU"] = new(StringComparer.OrdinalIgnoreCase)
        {
            ["Vocabulary.NotFound"] = "Данные словаря не найдены",
            ["Vocabulary.LanguageNotFound"] = "Язык обучения не найден",
            ["Vocabulary.TopicNotFound"] = "Тема словаря не найдена",
            ["Vocabulary.WordNotFound"] = "Слово не найдено",
            ["Vocabulary.QuestionNotFound"] = "Вопрос словаря не найден",
            ["Vocabulary.SessionNotFound"] = "Сессия словаря не найдена",
            ["Vocabulary.CourseRequired"] = "Сначала выберите язык обучения",
            ["Vocabulary.StudentRequired"] = "Требуется профиль студента",
            ["Vocabulary.StudentUnavailable"] = "Профиль студента недоступен",
            ["Progression.NotFound"] = "Данные лиги не найдены",
            ["Progression.LeagueInvalid"] = "Данные лиги некорректны",
            ["Progression.ThresholdsInvalid"] = "Диапазоны XP лиг должны быть последовательными и не пересекаться",
            ["Progression.StructuralLocked"] = "Структуру лиг нельзя менять во время активного сезона",
            ["Progression.SeasonActive"] = "Сезон лиги уже активен",
            ["Progression.SeasonUnavailable"] = "Активный сезон лиги недоступен",
            ["Progression.StudentUnavailable"] = "Студент недоступен для участия в лиге",
            ["Progression.AvatarInvalid"] = "Изображение лиги некорректно или превышает 2 МБ",
            ["Vocabulary.Duplicate"] = "Такая запись уже существует",
            ["Vocabulary.PublishInvalid"] = "Запись словаря нельзя опубликовать в текущем состоянии",
            ["Vocabulary.NotEnoughQuestions"] = "Для этого упражнения недостаточно опубликованных вопросов",
            ["Vocabulary.SessionCompleted"] = "Эта сессия уже завершена",
            ["Vocabulary.AnswerLocked"] = "Принятый ответ нельзя изменить",
            ["Vocabulary.AnswersIncomplete"] = "Ответьте на все вопросы",
            ["Vocabulary.Invalid"] = "Данные словаря некорректны"
        },
        ["en-US"] = new(StringComparer.OrdinalIgnoreCase)
        {
            ["Vocabulary.NotFound"] = "Vocabulary data was not found",
            ["Vocabulary.LanguageNotFound"] = "Learning language was not found",
            ["Vocabulary.TopicNotFound"] = "Vocabulary topic was not found",
            ["Vocabulary.WordNotFound"] = "Word was not found",
            ["Vocabulary.QuestionNotFound"] = "Vocabulary question was not found",
            ["Vocabulary.SessionNotFound"] = "Vocabulary session was not found",
            ["Vocabulary.CourseRequired"] = "Select a learning language first",
            ["Vocabulary.StudentRequired"] = "A student profile is required",
            ["Vocabulary.StudentUnavailable"] = "The student profile is unavailable",
            ["Progression.NotFound"] = "League data was not found",
            ["Progression.LeagueInvalid"] = "League data is invalid",
            ["Progression.ThresholdsInvalid"] = "League XP ranges must be contiguous and non-overlapping",
            ["Progression.StructuralLocked"] = "League structure cannot change during an active season",
            ["Progression.SeasonActive"] = "A league season is already active",
            ["Progression.SeasonUnavailable"] = "An active league season is unavailable",
            ["Progression.StudentUnavailable"] = "The student is unavailable for league participation",
            ["Progression.AvatarInvalid"] = "The league image is invalid or larger than 2 MB",
            ["Vocabulary.Duplicate"] = "This record already exists",
            ["Vocabulary.PublishInvalid"] = "Vocabulary content cannot be published in its current state",
            ["Vocabulary.NotEnoughQuestions"] = "There are not enough published questions for this exercise",
            ["Vocabulary.SessionCompleted"] = "This session is already completed",
            ["Vocabulary.AnswerLocked"] = "An accepted answer cannot be changed",
            ["Vocabulary.AnswersIncomplete"] = "Answer every question",
            ["Vocabulary.Invalid"] = "Vocabulary data is invalid"
        }
    };

    public string this[string key]
    {
        get
        {
            var culture = CultureInfo.CurrentUICulture.Name;
            if (key.Equals("MMT.ClusterLocked", StringComparison.OrdinalIgnoreCase)
                && ClusterLockedMessages.TryGetValue(culture, out var clusterLocked)) return clusterLocked;
            if (Messages.TryGetValue(culture, out var localized) && localized.TryGetValue(key, out var message))
            {
                return message;
            }

            if (ContentMessages.TryGetValue(culture, out var contentLocalized) && contentLocalized.TryGetValue(key, out var contentMessage))
            {
                return contentMessage;
            }

            if (LocalizedMmtMessages.TryGetValue(culture, out var mmtLocalized) && mmtLocalized.TryGetValue(key, out var mmtMessage))
            {
                return mmtMessage;
            }

            if (TestingMessages.TryGetValue(culture, out var testingLocalized)
                && testingLocalized.TryGetValue(key, out var testingMessage))
            {
                return testingMessage;
            }

            if (VocabularyMessages.TryGetValue(culture, out var vocabularyLocalized)
                && vocabularyLocalized.TryGetValue(key, out var vocabularyMessage))
            {
                return vocabularyMessage;
            }

            if (Messages["tg-TJ"].TryGetValue(key, out var fallback))
            {
                return fallback;
            }

            if (ContentMessages["tg-TJ"].TryGetValue(key, out var contentFallback))
            {
                return contentFallback;
            }

            if (TestingMessages["tg-TJ"].TryGetValue(key, out var testingFallback))
            {
                return testingFallback;
            }

            if (VocabularyMessages["tg-TJ"].TryGetValue(key, out var vocabularyFallback))
            {
                return vocabularyFallback;
            }

            return MmtMessages.GetValueOrDefault(key, key);
        }
    }
}
