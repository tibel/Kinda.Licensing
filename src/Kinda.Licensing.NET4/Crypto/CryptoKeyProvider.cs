using System;
using System.Security.Cryptography;

namespace Kinda.Licensing
{
    public class CryptoKeyProvider
    {
        public CryptoKey GenerateKey()
        {
            using (var rsa = new RSACryptoServiceProvider())
            {
                return new CryptoKey(rsa.ToXmlString(true));
            }
        }

        public CryptoKey ExtractPublicKey(CryptoKey privateKey)
        {
            if (privateKey == null)
                throw new ArgumentNullException(nameof(privateKey));

            using (var rsa = new RSACryptoServiceProvider())
            {
                rsa.FromXmlString(privateKey.Contents);
                return new CryptoKey(rsa.ToXmlString(false));
            }
        }
    }
}
