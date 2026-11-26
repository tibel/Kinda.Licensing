using System;
using System.Collections.Generic;

namespace Kinda.Licensing;

/// <summary>Rejects licenses whose identifiers appear in a host-provided revocation snapshot.</summary>
public sealed class LicenseRevocationRule : ILicenseValidationRule
{
    private readonly HashSet<Guid> _revokedLicenseIds;

    /// <summary>Initializes a rule with the identifiers in the current revocation snapshot.</summary>
    /// <param name="revokedLicenseIds">License identifiers to reject. The values are copied at construction.</param>
    /// <exception cref="ArgumentNullException"><paramref name="revokedLicenseIds"/> is <see langword="null"/>.</exception>
    public LicenseRevocationRule(IEnumerable<Guid> revokedLicenseIds)
    {
        ArgumentNullException.ThrowIfNull(revokedLicenseIds);
        _revokedLicenseIds = new HashSet<Guid>(revokedLicenseIds);
    }

    /// <summary>Rejects a license if its identifier is in the revocation snapshot.</summary>
    /// <param name="licenseCriteria">The criteria to validate.</param>
    /// <exception cref="ArgumentNullException"><paramref name="licenseCriteria"/> is <see langword="null"/>.</exception>
    /// <exception cref="LicenseViolationException">The license has been revoked.</exception>
    public void Validate(LicenseCriteria licenseCriteria)
    {
        ArgumentNullException.ThrowIfNull(licenseCriteria);
        if (_revokedLicenseIds.Contains(licenseCriteria.Id))
            throw new LicenseViolationException("License has been revoked.");
    }
}
