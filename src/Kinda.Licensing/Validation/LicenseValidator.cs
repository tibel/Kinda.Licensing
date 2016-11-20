using System;
using System.Collections.Generic;

namespace Kinda.Licensing
{
    public sealed class LicenseValidator
    {
        public LicenseCriteria Validate(License license, CryptoKey publicKey, IEnumerable<ILicenseValidationRule> validationRules)
        {
            if (license == null)
                throw new ArgumentNullException(nameof(license));
            if (publicKey == null)
                throw new ArgumentNullException(nameof(publicKey));
            if (validationRules == null)
                throw new ArgumentNullException(nameof(validationRules));

            var documentSigning = new DocumentSigning();
            if (!documentSigning.Validate(license.Document, publicKey))
            {
                throw new InvalidLicenseException();
            }

            var serializer = new LicenseCriteriaSerializer();
            var licenseCriteria = serializer.Deserialize(license.Document);
 
            var exceptions = new List<Exception>();
            foreach (var rule in validationRules)
            {
                try
                {
                    rule.Validate(licenseCriteria);
                }
                catch (Exception exception)
                {
                    exceptions.Add(exception);
                }
            }

            if (exceptions.Count > 0)
            {
                throw new AggregateException(exceptions);
            }

            return licenseCriteria;
        }
    }
}
