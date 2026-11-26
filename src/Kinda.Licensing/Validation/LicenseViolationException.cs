using System;

namespace Kinda.Licensing;

public class LicenseViolationException : Exception
{
    public LicenseViolationException()
    {
    }

    public LicenseViolationException(string message) : base(message)
    {
    }

    public LicenseViolationException(string message, Exception innerException) : base(message, innerException)
    {
    }
}
