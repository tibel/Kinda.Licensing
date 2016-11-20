using System;

namespace Kinda.Licensing.Demo.ClientApp
{
    public class ValidNumberOfCoresLicenseRule : ILicenseValidationRule
    {
        public void Validate(LicenseCriteria licenseCriteria)
        {
            var licensedCores = 0;

            if (licenseCriteria.MetaData.ContainsKey("LicensedCores"))
            {
                licensedCores = Convert.ToInt32(licenseCriteria.MetaData["LicensedCores"]);
            }

            if (Environment.ProcessorCount > licensedCores)
            {
                var message = string.Format("This license is only valid for {0} cores.", licensedCores);
                throw new LicensedCoresExceededException(message, Environment.ProcessorCount);
            }
        }
    }
}
