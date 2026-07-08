using System.Net.Mail;
using Adeeb.Application.Abstractions.Localization;
using Adeeb.Modules.Identity.Contracts;
using Adeeb.Modules.Identity.Infrastructure.Passwords;
using Adeeb.SharedKernel.Errors;
using Adeeb.SharedKernel.Results;

namespace Adeeb.Modules.Identity.Application;

internal static class Validation
{
    public static Result ValidateRegister(RegisterRequest request, PasswordPolicy passwordPolicy)
    {
        var errors = new Dictionary<string, IReadOnlyList<Error>>(StringComparer.OrdinalIgnoreCase);
        ValidateEmail(request.Email, errors);
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
        ValidateEmail(request.Email, errors);
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
            errors["device"] = [Error.Validation("auth.device.required", "Validation.Required")];
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
