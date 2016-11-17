using System;

namespace Kinda.Licensing
{
    public sealed class LicenseGenerator
    {
        public License Generate(CryptoKey privateKey, LicenseCriteria licenseCriteria)
        {
            if (privateKey == null)
                throw new ArgumentNullException(nameof(privateKey));
            if (licenseCriteria == null)
                throw new ArgumentNullException(nameof(licenseCriteria));

            var serializer = new LicenseCriteriaSerializer();
            var document = serializer.Serialize(licenseCriteria);

            var documentSigning = new DocumentSigning();
            documentSigning.Sign(document, privateKey);

            return new License(document);
        }
    }
}
