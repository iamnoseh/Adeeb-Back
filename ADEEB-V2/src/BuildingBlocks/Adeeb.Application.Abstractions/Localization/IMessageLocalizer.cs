namespace Adeeb.Application.Abstractions.Localization;

public interface IMessageLocalizer
{
    string this[string key] { get; }
}
