using Adeeb.SharedKernel.Results;

namespace Adeeb.Application.Abstractions.Students;

public sealed record StudentProvisioningReference(Guid StudentId, Guid IdentityUserId);

public interface IStudentRegistrationProvisioner
{
    Task<Result<StudentProvisioningReference>> ProvisionForIdentityUserAsync(Guid identityUserId, CancellationToken cancellationToken);
}

public sealed class NoOpStudentRegistrationProvisioner : IStudentRegistrationProvisioner
{
    public Task<Result<StudentProvisioningReference>> ProvisionForIdentityUserAsync(Guid identityUserId, CancellationToken cancellationToken) =>
        Task.FromResult(Result<StudentProvisioningReference>.Success(new StudentProvisioningReference(Guid.Empty, identityUserId)));
}
