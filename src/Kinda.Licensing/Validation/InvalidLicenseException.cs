using System;
#if COMPAT_NET4
using System.Runtime.Serialization;
#endif

namespace Kinda.Licensing
{
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

#if COMPAT_NET4
        protected InvalidLicenseException(SerializationInfo serializationInfo, StreamingContext streamingContext) : base(serializationInfo, streamingContext)
        {
        }
#endif
    }
}
