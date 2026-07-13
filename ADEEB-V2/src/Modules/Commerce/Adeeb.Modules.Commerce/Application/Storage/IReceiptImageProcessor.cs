using Adeeb.SharedKernel.Results;

namespace Adeeb.Modules.Commerce.Application.Storage;

public sealed record ValidatedReceiptImage(
    byte[] Content,
    string ContentType,
    string Sha256,
    int Width,
    int Height);

public interface IReceiptImageProcessor
{
    Task<Result<ValidatedReceiptImage>> ProcessAsync(
        Stream input,
        long declaredLength,
        CancellationToken cancellationToken);
}
