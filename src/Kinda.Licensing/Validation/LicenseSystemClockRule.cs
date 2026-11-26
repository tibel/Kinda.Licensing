using System;
using System.Diagnostics;

namespace Kinda.Licensing;

/// <summary>Rejects validation when the local system clock differs from a recently fetched trusted time.</summary>
public sealed class LicenseSystemClockRule : ILicenseValidationRule
{
    private static readonly TimeSpan DefaultMaximumClockSkew = TimeSpan.FromMinutes(5);

    private readonly DateTimeOffset _trustedTimeAtCreation;
    private readonly long _monotonicTimestamp;
    private readonly TimeSpan _maximumClockSkew;

    /// <summary>Initializes a rule with five minutes of allowed clock skew.</summary>
    /// <param name="trustedUtcNow">The UTC time recently obtained from a trusted source.</param>
    public LicenseSystemClockRule(DateTimeOffset trustedUtcNow)
        : this(trustedUtcNow, DefaultMaximumClockSkew)
    {
    }

    /// <summary>Initializes a rule with a specified maximum difference from the trusted time.</summary>
    /// <param name="trustedUtcNow">The UTC time recently obtained from a trusted source.</param>
    /// <param name="maximumClockSkew">The maximum allowed difference; must not be negative.</param>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="maximumClockSkew"/> is negative.</exception>
    public LicenseSystemClockRule(DateTimeOffset trustedUtcNow, TimeSpan maximumClockSkew)
    {
        if (maximumClockSkew < TimeSpan.Zero)
            throw new ArgumentOutOfRangeException(nameof(maximumClockSkew));

        _trustedTimeAtCreation = trustedUtcNow.ToUniversalTime();
        _monotonicTimestamp = Stopwatch.GetTimestamp();
        _maximumClockSkew = maximumClockSkew;
    }

    /// <summary>Checks that the local clock remains within the configured skew of the trusted time.</summary>
    /// <param name="licenseCriteria">The criteria being validated.</param>
    /// <exception cref="ArgumentNullException"><paramref name="licenseCriteria"/> is <see langword="null"/>.</exception>
    /// <exception cref="LicenseViolationException">The local system clock differs from the trusted time by more than the allowed skew.</exception>
    public void Validate(LicenseCriteria licenseCriteria)
    {
        ArgumentNullException.ThrowIfNull(licenseCriteria);

        var trustedNow = _trustedTimeAtCreation + Stopwatch.GetElapsedTime(_monotonicTimestamp);
        var clockDifference = (DateTimeOffset.UtcNow - trustedNow).Duration();
        if (clockDifference > _maximumClockSkew)
            throw new LicenseViolationException("The local system clock differs from the trusted online time.");
    }
}
