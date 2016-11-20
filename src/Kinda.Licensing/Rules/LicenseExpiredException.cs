using System;

namespace Kinda.Licensing
{
    public sealed class LicenseExpiredException : LicenseViolationException
    {
        public LicenseExpiredException()
        {
        }

        public LicenseExpiredException(string message) : base(message)
        {
        }

        public LicenseExpiredException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}
