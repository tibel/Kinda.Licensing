using System;
using System.Runtime.Serialization;

namespace Kinda.Licensing
{
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

        protected LicenseViolationException(SerializationInfo serializationInfo, StreamingContext streamingContext) : base(serializationInfo, streamingContext)
        {
        }
    }
}
