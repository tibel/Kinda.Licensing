using System;

namespace Kinda.Licensing;

/// <summary>The exception thrown when a license signature is invalid.</summary>
public class InvalidLicenseException : Exception
{
    /// <summary>Initializes a new instance of the <see cref="InvalidLicenseException"/> class.</summary>
    public InvalidLicenseException()
    {
    }

    /// <summary>Initializes a new instance of the <see cref="InvalidLicenseException"/> class with a message.</summary>
    /// <param name="message">The message that describes the error.</param>
    public InvalidLicenseException(string message) : base(message)
    {
    }

    /// <summary>Initializes a new instance of the <see cref="InvalidLicenseException"/> class with a message and inner exception.</summary>
    /// <param name="message">The message that describes the error.</param>
    /// <param name="innerException">The exception that caused this exception.</param>
    public InvalidLicenseException(string message, Exception innerException) : base(message, innerException)
    {
    }
}
