using System;

namespace Kinda.Licensing;

/// <summary>The exception thrown when a license fails a validation rule.</summary>
public class LicenseViolationException : Exception
{
    /// <summary>Initializes a new instance of the <see cref="LicenseViolationException"/> class.</summary>
    public LicenseViolationException()
    {
    }

    /// <summary>Initializes a new instance of the <see cref="LicenseViolationException"/> class with a message.</summary>
    /// <param name="message">The message that describes the violation.</param>
    public LicenseViolationException(string message) : base(message)
    {
    }

    /// <summary>Initializes a new instance of the <see cref="LicenseViolationException"/> class with a message and inner exception.</summary>
    /// <param name="message">The message that describes the violation.</param>
    /// <param name="innerException">The exception that caused this exception.</param>
    public LicenseViolationException(string message, Exception innerException) : base(message, innerException)
    {
    }
}
