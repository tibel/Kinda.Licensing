using System;

namespace Kinda.Licensing;

/// <summary>Rejects licenses whose expiration date is in the past.</summary>
public sealed class SimpleLicenseHasNotExpiredRule : ILicenseValidationRule
{
    /// <summary>Checks that the license has not expired.</summary>
    /// <param name="licenseCriteria">The criteria to validate.</param>
    /// <exception cref="LicenseExpiredException">The license has expired.</exception>
    public void Validate(LicenseCriteria licenseCriteria)
    {
        if (DateTimeOffset.UtcNow > licenseCriteria.ExpirationDate)
        {
            throw new LicenseExpiredException(string.Format("License expired on {0:O}", licenseCriteria.ExpirationDate));
        }
    }
}
