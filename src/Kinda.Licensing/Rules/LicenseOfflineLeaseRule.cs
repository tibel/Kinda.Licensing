using System;

namespace Kinda.Licensing;

/// <summary>Validates an offline license lease's issue date, expiry, and maximum lifetime.</summary>
public sealed class LicenseOfflineLeaseRule : ILicenseValidationRule
{
    private static readonly TimeSpan DefaultMaximumLeaseDuration = TimeSpan.FromDays(7);
    private static readonly TimeSpan DefaultAllowedClockSkew = TimeSpan.FromMinutes(5);

    private readonly TimeSpan _maximumLeaseDuration;
    private readonly TimeSpan _allowedClockSkew;

    /// <summary>Initializes a rule with a seven-day maximum lease and five minutes of clock skew.</summary>
    public LicenseOfflineLeaseRule()
        : this(DefaultMaximumLeaseDuration, DefaultAllowedClockSkew)
    {
    }

    /// <summary>Initializes a rule with a specified maximum lease duration and five minutes of clock skew.</summary>
    /// <param name="maximumLeaseDuration">The maximum time from issue to expiry; must be positive.</param>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="maximumLeaseDuration"/> is not positive.</exception>
    public LicenseOfflineLeaseRule(TimeSpan maximumLeaseDuration)
        : this(maximumLeaseDuration, DefaultAllowedClockSkew)
    {
    }

    /// <summary>Initializes a rule with a specified maximum lease duration and clock skew.</summary>
    /// <param name="maximumLeaseDuration">The maximum time from issue to expiry; must be positive.</param>
    /// <param name="allowedClockSkew">The tolerated difference between issuer and validator clocks; must not be negative.</param>
    /// <exception cref="ArgumentOutOfRangeException">A duration is outside its allowed range.</exception>
    public LicenseOfflineLeaseRule(TimeSpan maximumLeaseDuration, TimeSpan allowedClockSkew)
    {
        if (maximumLeaseDuration <= TimeSpan.Zero)
            throw new ArgumentOutOfRangeException(nameof(maximumLeaseDuration));
        if (allowedClockSkew < TimeSpan.Zero)
            throw new ArgumentOutOfRangeException(nameof(allowedClockSkew));

        _maximumLeaseDuration = maximumLeaseDuration;
        _allowedClockSkew = allowedClockSkew;
    }

    /// <summary>Validates the issue date and expiry of the offline license lease.</summary>
    /// <param name="licenseCriteria">The criteria to validate.</param>
    /// <exception cref="ArgumentNullException"><paramref name="licenseCriteria"/> is <see langword="null"/>.</exception>
    /// <exception cref="LicenseViolationException">The issue date is in the future or the lease exceeds its maximum lifetime.</exception>
    /// <exception cref="LicenseExpiredException">The license has expired.</exception>
    public void Validate(LicenseCriteria licenseCriteria)
    {
        ArgumentNullException.ThrowIfNull(licenseCriteria);

        var now = DateTimeOffset.UtcNow;
        if (licenseCriteria.IssueDate - now > _allowedClockSkew)
            throw new LicenseViolationException("License issue date is in the future.");
        if (licenseCriteria.ExpirationDate <= licenseCriteria.IssueDate)
            throw new LicenseViolationException("License expiry must be after its issue date.");
        if (licenseCriteria.ExpirationDate - licenseCriteria.IssueDate > _maximumLeaseDuration)
            throw new LicenseViolationException("License exceeds the maximum offline lease duration.");
        if (now > licenseCriteria.ExpirationDate)
            throw new LicenseExpiredException(string.Format("License expired on {0:O}", licenseCriteria.ExpirationDate));
    }
}
