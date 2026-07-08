using System.Net.Mail;
using Adeeb.Application.Abstractions.Localization;
using Adeeb.Modules.Identity.Contracts;
using Adeeb.Modules.Identity.Infrastructure.Passwords;
using Adeeb.SharedKernel.Errors;
using Adeeb.SharedKernel.Results;

namespace Adeeb.Modules.Identity.Application;

public static class Validation
{
    public static Result ValidateRegister(RegisterRequest request, PasswordPolicy passwordPolicy)
    {
        var errors = new Dictionary<string, IReadOnlyList<Error>>(StringComparer.OrdinalIgnoreCase);
        ValidateEmail(request.Email, errors);
        ValidateOptionalPhoneNumber(request.PhoneNumber, errors);
        ValidatePassword(request.Password, passwordPolicy, errors);
        ValidateName(request.FirstName, "firstName", errors);
        ValidateName(request.LastName, "lastName", errors);
        ValidateLanguage(request.Language, errors);
        ValidateDevice(request.Device, errors);
        return errors.Count == 0 ? Result.Success() : Result.ValidationFailure(errors);
    }

    public static Result ValidateLogin(LoginRequest request)
    {
        var errors = new Dictionary<string, IReadOnlyList<Error>>(StringComparer.OrdinalIgnoreCase);
        ValidateIdentifier(request.Identifier ?? request.Email, errors);
        if (string.IsNullOrWhiteSpace(request.Password))
        {
            errors["password"] = [Error.Validation("auth.password.required", "Validation.Required")];
        }
        ValidateDevice(request.Device, errors);
        return errors.Count == 0 ? Result.Success() : Result.ValidationFailure(errors);
    }

    public static Result ValidateChangePassword(ChangePasswordRequest request, PasswordPolicy passwordPolicy)
    {
        var errors = new Dictionary<string, IReadOnlyList<Error>>(StringComparer.OrdinalIgnoreCase);
        if (string.IsNullOrWhiteSpace(request.CurrentPassword))
        {
            errors["currentPassword"] = [Error.Validation("auth.password.required", "Validation.Required")];
        }
        ValidatePassword(request.NewPassword, passwordPolicy, errors, "newPassword");
        return errors.Count == 0 ? Result.Success() : Result.ValidationFailure(errors);
    }

    private static void ValidateEmail(string? email, Dictionary<string, IReadOnlyList<Error>> errors)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            errors["email"] = [Error.Validation("auth.email.required", "Validation.Required")];
            return;
        }

        try
        {
            _ = new MailAddress(email);
        }
        catch (FormatException)
        {
            errors["email"] = [Error.Validation("auth.email.invalid", "Validation.InvalidEmail")];
        }
    }

    private static void ValidateIdentifier(string? identifier, Dictionary<string, IReadOnlyList<Error>> errors)
    {
        if (string.IsNullOrWhiteSpace(identifier))
        {
            errors["identifier"] = [Error.Validation("auth.identifier.required", "Validation.Required")];
            return;
        }

        if (identifier.Contains('@', StringComparison.Ordinal))
        {
            var emailErrors = new Dictionary<string, IReadOnlyList<Error>>(StringComparer.OrdinalIgnoreCase);
            ValidateEmail(identifier, emailErrors);
            if (emailErrors.TryGetValue("email", out var emailError))
            {
                errors["identifier"] = emailError;
            }

            return;
        }

        if (NormalizePhoneNumber(identifier) is null)
        {
            errors["identifier"] = [Error.Validation("auth.identifier.invalid", "Validation.Required")];
        }
    }

    private static void ValidateOptionalPhoneNumber(string? phoneNumber, Dictionary<string, IReadOnlyList<Error>> errors)
    {
        if (string.IsNullOrWhiteSpace(phoneNumber))
        {
            return;
        }

        if (NormalizePhoneNumber(phoneNumber) is null)
        {
            errors["phoneNumber"] = [Error.Validation("auth.phone.invalid", "Validation.Required")];
        }
    }

    public static string? NormalizePhoneNumber(string? phoneNumber)
    {
        if (string.IsNullOrWhiteSpace(phoneNumber))
        {
            return null;
        }

        var trimmed = phoneNumber.Trim();
        var startsWithPlus = trimmed.StartsWith('+');
        var digits = new string(trimmed.Where(char.IsDigit).ToArray());

        if (digits.Length is < 7 or > 15)
        {
            return null;
        }

        return startsWithPlus ? $"+{digits}" : digits;
    }

    private static void ValidatePassword(string? password, PasswordPolicy passwordPolicy, Dictionary<string, IReadOnlyList<Error>> errors, string field = "password")
    {
        if (string.IsNullOrWhiteSpace(password))
        {
            errors[field] = [Error.Validation("auth.password.required", "Validation.Required")];
            return;
        }

        if (!passwordPolicy.IsValid(password))
        {
            errors[field] = [Error.Validation("auth.password.policy", "Validation.PasswordPolicy")];
        }
    }

    private static void ValidateName(string? name, string field, Dictionary<string, IReadOnlyList<Error>> errors)
    {
        if (string.IsNullOrWhiteSpace(name) || name.Trim().Length > 80)
        {
            errors[field] = [Error.Validation($"auth.{field}.required", "Validation.Required")];
        }
    }

    private static void ValidateLanguage(string? language, Dictionary<string, IReadOnlyList<Error>> errors)
    {
        if (!SupportedLanguageExtensions.TryParseCulture(language, out _))
        {
            errors["language"] = [Error.Validation("auth.language.unsupported", "Validation.UnsupportedLanguage")];
        }
    }

    private static void ValidateDevice(DeviceRequest? device, Dictionary<string, IReadOnlyList<Error>> errors)
    {
        if (device is null)
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(device.DeviceId) || device.DeviceId.Length > 128)
        {
            errors["device.deviceId"] = [Error.Validation("auth.device_id.required", "Validation.Required")];
        }

        if (string.IsNullOrWhiteSpace(device.DeviceName) || device.DeviceName.Length > 120)
        {
            errors["device.deviceName"] = [Error.Validation("auth.device_name.required", "Validation.Required")];
        }

        if (string.IsNullOrWhiteSpace(device.Platform) || device.Platform.Length > 40)
        {
            errors["device.platform"] = [Error.Validation("auth.device_platform.required", "Validation.Required")];
        }

        if (device.AppVersion?.Length > 40)
        {
            errors["device.appVersion"] = [Error.Validation("auth.device_app_version.invalid", "Validation.Required")];
        }
    }
}
