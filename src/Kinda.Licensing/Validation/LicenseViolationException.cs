using System;
#if COMPAT_NET4
using System.Runtime.Serialization;
#endif

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

#if COMPAT_NET4
        protected LicenseViolationException(SerializationInfo serializationInfo, StreamingContext streamingContext) : base(serializationInfo, streamingContext)
        {
        }
#endif
    }
}
