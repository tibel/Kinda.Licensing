using System;

namespace Kinda.Licensing;

public class InvalidLicenseException : Exception
{
    public InvalidLicenseException()
    {
    }

    public InvalidLicenseException(string message) : base(message)
    {
    }

    public InvalidLicenseException(string message, Exception innerException) : base(message, innerException)
    {
    }
}
