using System;
using System.IO;
using System.Security.Cryptography;
using System.Xml.Linq;

namespace Kinda.Licensing
{
    public class DocumentSigning
    {
        private const string SignatureElementName = "Signature";
        private const string DigestValueElementName = "DigestValue";
        private const string SignatureValueElementName = "SignatureValue";

        private const string AlgorithmAttributeName = "Algorithm";
        private const string AlgorithmAttributeValue = "rsa-sha256";

        public void Sign(XDocument document, CryptoKey privateKey)
        {
            if (document == null)
                throw new ArgumentNullException(nameof(document));
            if (privateKey == null)
                throw new ArgumentNullException(nameof(privateKey));

            var signatureElement = GetSignatureElement(document);
            if (signatureElement != null)
                throw new InvalidOperationException("Already signed");

            var hash = ComputeHash(document);

#if COMPAT_NET4
            using (var rsa = new RSACryptoServiceProvider())
#else
            using (var rsa = RSA.Create())
#endif
            {
                rsa.FromXmlString(privateKey.Contents);

#if COMPAT_NET4
                var signature = rsa.SignHash(hash, "SHA256");
#else
                var signature = rsa.SignHash(hash, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
#endif

                signatureElement = new XElement(SignatureElementName);
                signatureElement.SetAttributeValue(AlgorithmAttributeName, AlgorithmAttributeValue);
                signatureElement.SetElementValue(DigestValueElementName, Convert.ToBase64String(hash));
                signatureElement.SetElementValue(SignatureValueElementName, Convert.ToBase64String(signature));
                document.Root.Add(signatureElement);
            }
        }

        public bool Validate(XDocument document, CryptoKey publicKey)
        {
            if (document == null)
                throw new ArgumentNullException(nameof(document));
            if (publicKey == null)
                throw new ArgumentNullException(nameof(publicKey));

            var signatureElement = GetSignatureElement(document);
            if (signatureElement == null)
                return false;

            try
            {
                signatureElement.Remove();

                var digestValue = (string)signatureElement.Element(DigestValueElementName);

                var hash = ComputeHash(document);
                if (digestValue != Convert.ToBase64String(hash))
                    return false;

                var signatureValue = (string)signatureElement.Element(SignatureValueElementName);
                var signature = Convert.FromBase64String(signatureValue);

#if COMPAT_NET4
            using (var rsa = new RSACryptoServiceProvider())
#else
                using (var rsa = RSA.Create())
#endif
                {
                    rsa.FromXmlString(publicKey.Contents);

#if COMPAT_NET4
                    return rsa.VerifyHash(hash, "SHA256", signature);
#else
                    return rsa.VerifyHash(hash, signature, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
#endif
                }
            }
            finally
            {
                document.Root.Add(signatureElement);
            }
        }

        private static XElement GetSignatureElement(XDocument document)
        {
            var signature = document.Root.LastNode as XElement;
            if (signature == null || signature.Name != SignatureElementName)
                return null;

            var algorithmAttribute = signature.Attribute(AlgorithmAttributeName);
            if (algorithmAttribute.Value != AlgorithmAttributeValue)
                return null;

            return signature;
        }

        private static byte[] ComputeHash(XDocument document)
        {
            using (var hashAlgorithm = SHA256.Create())
            using (var stream = new MemoryStream())
            {
                document.Save(stream);
                stream.Position = 0;
                return hashAlgorithm.ComputeHash(stream);
            }
        }
    }
}
