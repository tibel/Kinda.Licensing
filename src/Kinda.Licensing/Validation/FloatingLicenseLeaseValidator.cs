using System;
using System.Collections.Generic;
using System.Security.Cryptography;

namespace Kinda.Licensing;

/// <summary>Validates signed floating-license leases against trusted keys and current host data.</summary>
public sealed class FloatingLicenseLeaseValidator
{
    /// <summary>The license metadata key containing the floating-license pool identifier.</summary>
    public const string PoolIdMetadataKey = "FloatingLicensePool";

    /// <summary>Verifies a floating-license lease and applies its pool, device, expiry, and revocation checks.</summary>
    /// <param name="lease">The signed lease returned by the floating-license service.</param>
    /// <param name="trustedPublicKeys">Current and retiring issuer public keys trusted by the host.</param>
    /// <param name="poolId">The expected application-defined pool identifier.</param>
    /// <param name="deviceId">The stable device identifier supplied by the host application.</param>
    /// <param name="revokedLeaseIds">The current revocation snapshot, refreshed by the host when reconnecting.</param>
    /// <returns>The validated lease criteria.</returns>
    /// <exception cref="ArgumentNullException">An argument is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="poolId"/> or <paramref name="deviceId"/> is empty or whitespace.</exception>
    /// <exception cref="InvalidLicenseException">The lease signature is invalid for every trusted key.</exception>
    /// <exception cref="AggregateException">A validation rule rejects the lease.</exception>
    public LicenseCriteria Validate(
        License lease,
        IEnumerable<RSA> trustedPublicKeys,
        string poolId,
        string deviceId,
        IEnumerable<Guid> revokedLeaseIds)
    {
        ArgumentNullException.ThrowIfNull(lease);
        ArgumentNullException.ThrowIfNull(trustedPublicKeys);
        ArgumentNullException.ThrowIfNull(revokedLeaseIds);
        ArgumentException.ThrowIfNullOrWhiteSpace(poolId);
        ArgumentException.ThrowIfNullOrWhiteSpace(deviceId);

        return new LicenseValidator().ValidateWithAnyPublicKey(
            lease,
            trustedPublicKeys,
            [
                new LicenseOfflineLeaseRule(),
                new LicenseMachineCodeRule(deviceId),
                new LicenseRevocationRule(revokedLeaseIds),
                new PoolIdRule(poolId)
            ]);
    }

    private sealed class PoolIdRule : ILicenseValidationRule
    {
        private readonly string _poolId;

        public PoolIdRule(string poolId)
        {
            _poolId = poolId;
        }

        public void Validate(LicenseCriteria licenseCriteria)
        {
            ArgumentNullException.ThrowIfNull(licenseCriteria);

            if (licenseCriteria.Id == Guid.Empty ||
                licenseCriteria.MetaData is null ||
                !licenseCriteria.MetaData.TryGetValue(PoolIdMetadataKey, out var actualPoolId) ||
                !string.Equals(actualPoolId, _poolId, StringComparison.Ordinal))
                throw new LicenseViolationException("Lease identifier or pool is invalid.");
        }
    }
}
