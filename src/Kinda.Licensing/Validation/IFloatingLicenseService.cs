using System;
using System.Threading;
using System.Threading.Tasks;

namespace Kinda.Licensing;

/// <summary>Defines the host-supplied client contract for a floating-license authority.</summary>
/// <remarks>
/// Implementations provide transport, authentication, persistence, atomic seat allocation, and signing.
/// Implementations must authorize each operation and bind lease identifiers to the requested pool and device.
/// A seat remains reserved until its lease expires or a release succeeds. Releasing a seat does not
/// invalidate copies of an offline lease held by clients that have not refreshed their revocation snapshot.
/// </remarks>
public interface IFloatingLicenseService
{
    /// <summary>Acquires a signed lease for a device in the specified license pool.</summary>
    /// <param name="poolId">The application-defined license pool identifier.</param>
    /// <param name="deviceId">The stable, application-supplied device identifier.</param>
    /// <param name="cancellationToken">A token used to cancel the request.</param>
    /// <returns>A signed license with a unique, non-empty identifier that serves as the lease identifier.</returns>
    /// <remarks>
    /// An implementation must allocate seats atomically, count distinct devices rather than requests,
    /// and return the existing active lease when the same device acquires again. The signed license must
    /// include the pool identifier under <see cref="FloatingLicenseLeaseValidator.PoolIdMetadataKey"/>,
    /// the device code under <see cref="MachineCodeGenerator.MetadataKey"/>, and an expiry no more than
    /// seven days after its issue date.
    /// </remarks>
    Task<License> AcquireAsync(
        string poolId,
        string deviceId,
        CancellationToken cancellationToken = default);

    /// <summary>Renews an active lease for the same pool and device.</summary>
    /// <param name="poolId">The application-defined license pool identifier.</param>
    /// <param name="leaseId">The identifier of the lease returned by <see cref="AcquireAsync"/>.</param>
    /// <param name="deviceId">The stable, application-supplied device identifier.</param>
    /// <param name="cancellationToken">A token used to cancel the request.</param>
    /// <returns>A newly signed lease with the same identifier and a renewed expiry.</returns>
    /// <remarks>
    /// The renewed issue date must reflect the renewal time, expiry must be no more than seven days later,
    /// and the host must reject a lease ID that is not active for the specified pool and device.
    /// </remarks>
    Task<License> RenewAsync(
        string poolId,
        Guid leaseId,
        string deviceId,
        CancellationToken cancellationToken = default);

    /// <summary>Releases the specified lease and frees its seat at the authority.</summary>
    /// <param name="poolId">The application-defined license pool identifier.</param>
    /// <param name="leaseId">The identifier of the lease returned by <see cref="AcquireAsync"/>.</param>
    /// <param name="deviceId">The stable, application-supplied device identifier.</param>
    /// <param name="cancellationToken">A token used to cancel the request.</param>
    /// <remarks>
    /// Implementations must make release idempotent and only free a lease belonging to the specified pool
    /// and device. Completing this request does not stop a disconnected client from using its still-valid
    /// signed lease until expiry unless that client receives a current revocation snapshot.
    /// </remarks>
    Task ReleaseAsync(
        string poolId,
        Guid leaseId,
        string deviceId,
        CancellationToken cancellationToken = default);
}
