using System;

namespace Kinda.Licensing
{
    public sealed class SimpleLicenseHasNotExpiredRule : ILicenseValidationRule
    {
        public void Validate(LicenseCriteria licenseCriteria)
        {
            if (DateTimeOffset.UtcNow > licenseCriteria.ExpirationDate)
            {
                throw new LicenseExpiredException(string.Format("License expired on {0:O}", licenseCriteria.ExpirationDate));
            }
        }
    }
}
