using System;

namespace Kinda.Licensing
{
    public sealed class LicenseHasNotExpiredRule : ILicenseValidationRule
    {
        public void Validate(LicenseCriteria licenseCriteria)
        {
            //TODO: get from a time server.
            if (DateTime.UtcNow > licenseCriteria.ExpirationDate)
            {
                throw new LicenseExpiredException(string.Format("License expired on {0:O}", licenseCriteria.ExpirationDate));
            }
        }
    }
}
