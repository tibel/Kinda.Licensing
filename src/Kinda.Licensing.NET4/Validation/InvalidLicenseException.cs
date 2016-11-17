using System;
using System.Runtime.Serialization;

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

        protected InvalidLicenseException(SerializationInfo serializationInfo, StreamingContext streamingContext) : base(serializationInfo, streamingContext)
        {
        }
    }
}
