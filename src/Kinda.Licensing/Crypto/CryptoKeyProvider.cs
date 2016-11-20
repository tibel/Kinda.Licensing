using System;
using System.Security.Cryptography;

namespace Kinda.Licensing
{
    public class CryptoKeyProvider
    {
        public CryptoKey GenerateKey()
        {
#if COMPAT_NET4
            using (var rsa = new RSACryptoServiceProvider())
#else
            using (var rsa = RSA.Create())
#endif
            {
                return new CryptoKey(rsa.ToXmlString(true));
            }
        }

        public CryptoKey ExtractPublicKey(CryptoKey privateKey)
        {
            if (privateKey == null)
                throw new ArgumentNullException(nameof(privateKey));

#if COMPAT_NET4
            using (var rsa = new RSACryptoServiceProvider())
#else
            using (var rsa = RSA.Create())
#endif
            {
                rsa.FromXmlString(privateKey.Contents);
                return new CryptoKey(rsa.ToXmlString(false));
            }
        }
    }
}
