using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;

namespace Kinda.Licensing.Tests;

public class FloatingLicenseTests
{
    [Test]
    public async Task FloatingLicenseServiceContractSupportsAcquireRenewAndRelease()
    {
        const string poolId = "pool-a";
        const string firstDeviceId = "device-1";
        const string secondDeviceId = "device-2";
        using var signingKey = RSA.Create();
        using var publicKey = CreatePublicKey(signingKey);
        var service = new InMemoryFloatingLicenseService(signingKey, seatLimit: 1);
        var validator = new FloatingLicenseLeaseValidator();

        var acquiredLease = await service.AcquireAsync(poolId, firstDeviceId);
        var acquiredCriteria = validator.Validate(
            acquiredLease, [publicKey], poolId, firstDeviceId, []);
        var duplicateAcquire = await service.AcquireAsync(poolId, firstDeviceId);

        await Assert.That(acquiredCriteria.Id).IsEqualTo(
            new LicenseCriteriaSerializer().Deserialize(duplicateAcquire.Document).Id);
        await Assert.That(async () => await service.AcquireAsync(poolId, secondDeviceId))
            .ThrowsExactly<LicenseViolationException>();

        var renewedLease = await service.RenewAsync(poolId, acquiredCriteria.Id, firstDeviceId);
        var renewedCriteria = validator.Validate(
            renewedLease, [publicKey], poolId, firstDeviceId, []);

        await Assert.That(renewedCriteria.Id).IsEqualTo(acquiredCriteria.Id);
        await Assert.That(renewedCriteria.ExpirationDate)
            .IsGreaterThan(acquiredCriteria.ExpirationDate);
        await Assert.That(renewedCriteria.ExpirationDate - renewedCriteria.IssueDate)
            .IsLessThanOrEqualTo(TimeSpan.FromDays(7));

        await service.ReleaseAsync(poolId, renewedCriteria.Id, firstDeviceId);
        var secondLease = await service.AcquireAsync(poolId, secondDeviceId);
        var secondCriteria = validator.Validate(
            secondLease, [publicKey], poolId, secondDeviceId, []);
        await Assert.That(secondCriteria.Id).IsNotEqualTo(acquiredCriteria.Id);

        await service.ReleaseAsync(poolId, secondCriteria.Id, secondDeviceId);
        await service.ReleaseAsync(poolId, secondCriteria.Id, secondDeviceId);
    }

    [Test]
    public async Task FloatingLicenseServiceContractAllocatesSeatsAtomically()
    {
        using var signingKey = RSA.Create();
        var service = new InMemoryFloatingLicenseService(signingKey, seatLimit: 1);

        var results = await Task.WhenAll(
            Task.Run(() => TryAcquireAsync(service, "pool-a", "device-1")),
            Task.Run(() => TryAcquireAsync(service, "pool-a", "device-2")));

        await Assert.That(results.Count(acquired => acquired)).IsEqualTo(1);
    }

    [Test]
    public async Task FloatingLicenseLeaseValidatorChecksKeyRotationAndLeaseClaims()
    {
        const string poolId = "pool-a";
        const string deviceId = "device-1";
        var leaseId = Guid.NewGuid();
        var issueDate = DateTimeOffset.UtcNow.AddMinutes(-1);
        using var retiringPrivateKey = RSA.Create();
        using var retiringPublicKey = CreatePublicKey(retiringPrivateKey);
        using var currentPublicKey = RSA.Create();
        var validator = new FloatingLicenseLeaseValidator();
        var lease = CreateLease(
            retiringPrivateKey, leaseId, poolId, deviceId, issueDate, issueDate.AddDays(1));

        var criteria = validator.Validate(
            lease, [currentPublicKey, retiringPublicKey], poolId, deviceId, []);

        await Assert.That(criteria.Id).IsEqualTo(leaseId);
        await Assert.That(() => validator.Validate(
                lease, [currentPublicKey], poolId, deviceId, []))
            .ThrowsExactly<InvalidLicenseException>();
        await Assert.That(() => validator.Validate(
                lease, [retiringPublicKey], "another-pool", deviceId, []))
            .ThrowsExactly<AggregateException>();
        await Assert.That(() => validator.Validate(
                lease, [retiringPublicKey], poolId, "another-device", []))
            .ThrowsExactly<AggregateException>();
        await Assert.That(() => validator.Validate(
                lease, [retiringPublicKey], poolId, deviceId, [leaseId]))
            .ThrowsExactly<AggregateException>();

        var overlongLease = CreateLease(
            retiringPrivateKey, Guid.NewGuid(), poolId, deviceId, issueDate, issueDate.AddDays(8));
        await Assert.That(() => validator.Validate(
                overlongLease, [retiringPublicKey], poolId, deviceId, []))
            .ThrowsExactly<AggregateException>();

        var expiredLease = CreateLease(
            retiringPrivateKey,
            Guid.NewGuid(),
            poolId,
            deviceId,
            issueDate.AddDays(-2),
            issueDate.AddDays(-1));
        await Assert.That(() => validator.Validate(
                expiredLease, [retiringPublicKey], poolId, deviceId, []))
            .ThrowsExactly<AggregateException>();

        var emptyIdLease = CreateLease(
            retiringPrivateKey, Guid.Empty, poolId, deviceId, issueDate, issueDate.AddDays(1));
        await Assert.That(() => validator.Validate(
                emptyIdLease, [retiringPublicKey], poolId, deviceId, []))
            .ThrowsExactly<AggregateException>();
    }

    private static License CreateLease(
        RSA signingKey,
        Guid leaseId,
        string poolId,
        string deviceId,
        DateTimeOffset issueDate,
        DateTimeOffset expirationDate)
    {
        var criteria = new LicenseCriteria
        {
            Id = leaseId,
            IssueDate = issueDate,
            ExpirationDate = expirationDate,
            Type = "Floating",
            MetaData = new Dictionary<string, string>
            {
                [FloatingLicenseLeaseValidator.PoolIdMetadataKey] = poolId,
                [MachineCodeGenerator.MetadataKey] = MachineCodeGenerator.Generate(deviceId)
            }
        };
        return new LicenseGenerator().Generate(signingKey, criteria);
    }

    private static RSA CreatePublicKey(RSA privateKey)
    {
        var publicKey = RSA.Create();
        publicKey.ImportFromPem(privateKey.ExportSubjectPublicKeyInfoPem());
        return publicKey;
    }

    private static async Task<bool> TryAcquireAsync(
        IFloatingLicenseService service,
        string poolId,
        string deviceId)
    {
        try
        {
            await service.AcquireAsync(poolId, deviceId);
            return true;
        }
        catch (LicenseViolationException)
        {
            return false;
        }
    }

    private sealed class InMemoryFloatingLicenseService : IFloatingLicenseService
    {
        private readonly RSA _signingKey;
        private readonly int _seatLimit;
        private readonly object _gate = new();
        private readonly Dictionary<(string PoolId, string DeviceId), License> _leases = [];

        public InMemoryFloatingLicenseService(RSA signingKey, int seatLimit)
        {
            _signingKey = signingKey;
            _seatLimit = seatLimit;
        }

        public Task<License> AcquireAsync(
            string poolId,
            string deviceId,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            lock (_gate)
            {
                var now = DateTimeOffset.UtcNow;
                var expiredLeaseKeys = _leases
                    .Where(pair =>
                        pair.Key.PoolId == poolId &&
                        new LicenseCriteriaSerializer().Deserialize(pair.Value.Document).ExpirationDate <= now)
                    .Select(pair => pair.Key)
                    .ToArray();
                foreach (var expiredLeaseKey in expiredLeaseKeys)
                    _leases.Remove(expiredLeaseKey);

                var key = (poolId, deviceId);
                if (_leases.TryGetValue(key, out var existingLease))
                    return Task.FromResult(existingLease);

                if (_leases.Keys.Count(lease => lease.PoolId == poolId) >= _seatLimit)
                    throw new LicenseViolationException("No seats are available.");

                var lease = CreateLease(_signingKey, Guid.NewGuid(), poolId, deviceId, now, now.AddDays(7));
                _leases.Add(key, lease);
                return Task.FromResult(lease);
            }
        }

        public Task<License> RenewAsync(
            string poolId,
            Guid leaseId,
            string deviceId,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            lock (_gate)
            {
                var key = (poolId, deviceId);
                var now = DateTimeOffset.UtcNow;
                if (!_leases.TryGetValue(key, out var existingLease))
                    throw new LicenseViolationException("The lease is not active for this device and pool.");

                var existingCriteria = new LicenseCriteriaSerializer().Deserialize(existingLease.Document);
                if (existingCriteria.Id != leaseId || existingCriteria.ExpirationDate <= now)
                    throw new LicenseViolationException("The lease is not active for this device and pool.");

                if (now.ToUnixTimeSeconds() <= existingCriteria.IssueDate.ToUnixTimeSeconds())
                    now = existingCriteria.IssueDate.AddSeconds(1);
                var renewedLease = CreateLease(_signingKey, leaseId, poolId, deviceId, now, now.AddDays(7));
                _leases[key] = renewedLease;
                return Task.FromResult(renewedLease);
            }
        }

        public Task ReleaseAsync(
            string poolId,
            Guid leaseId,
            string deviceId,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            lock (_gate)
            {
                var key = (poolId, deviceId);
                if (_leases.TryGetValue(key, out var lease) &&
                    new LicenseCriteriaSerializer().Deserialize(lease.Document).Id == leaseId)
                    _leases.Remove(key);
            }

            return Task.CompletedTask;
        }
    }
}
