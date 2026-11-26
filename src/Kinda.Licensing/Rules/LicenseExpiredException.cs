using System;

namespace Kinda.Licensing;

/// <summary>The exception thrown when a license has expired.</summary>
public sealed class LicenseExpiredException : LicenseViolationException
{
    /// <summary>Initializes a new instance of the <see cref="LicenseExpiredException"/> class.</summary>
    public LicenseExpiredException()
    {
    }

    /// <summary>Initializes a new instance of the <see cref="LicenseExpiredException"/> class with a message.</summary>
    /// <param name="message">The message that describes the expiration.</param>
    public LicenseExpiredException(string message) : base(message)
    {
    }

    /// <summary>Initializes a new instance of the <see cref="LicenseExpiredException"/> class with a message and inner exception.</summary>
    /// <param name="message">The message that describes the expiration.</param>
    /// <param name="innerException">The exception that caused this exception.</param>
    public LicenseExpiredException(string message, Exception innerException) : base(message, innerException)
    {
    }
}
