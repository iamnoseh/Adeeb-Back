using Adeeb.SharedKernel.Results;
using Adeeb.SharedKernel.Errors;

namespace Adeeb.Application.Abstractions.Students;

public sealed record StudentProvisioningReference(Guid StudentId, Guid IdentityUserId);

public interface IStudentRegistrationProvisioner
{
    Task<Result<StudentProvisioningReference>> ProvisionForIdentityUserAsync(Guid identityUserId, CancellationToken cancellationToken);
}

public sealed class MissingStudentRegistrationProvisioner : IStudentRegistrationProvisioner
{
    private static readonly Error ProvisioningUnavailable =
        Error.Conflict("student.provisioning_unavailable", "Student.ProvisioningUnavailable");

    public Task<Result<StudentProvisioningReference>> ProvisionForIdentityUserAsync(Guid identityUserId, CancellationToken cancellationToken) =>
        Task.FromResult(Result<StudentProvisioningReference>.Failure(ProvisioningUnavailable));
}
