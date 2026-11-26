using System;
using System.Security.Cryptography;

namespace Kinda.Licensing;

/// <summary>Creates signed XML licenses from license criteria.</summary>
public sealed class LicenseGenerator
{
    /// <summary>Creates a signed license using the specified RSA private key.</summary>
    /// <param name="privateKey">The caller-owned RSA private key.</param>
    /// <param name="licenseCriteria">The criteria to include in the license.</param>
    /// <returns>A license containing the signed XML document.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="privateKey"/> or <paramref name="licenseCriteria"/> is <see langword="null"/>.</exception>
    /// <exception cref="CryptographicException">The key cannot sign the license.</exception>
    public License Generate(RSA privateKey, LicenseCriteria licenseCriteria)
    {
        ArgumentNullException.ThrowIfNull(privateKey);
        ArgumentNullException.ThrowIfNull(licenseCriteria);

        var serializer = new LicenseCriteriaSerializer();
        var document = serializer.Serialize(licenseCriteria);

        var documentSigning = new DocumentSigning();
        documentSigning.Sign(document, privateKey);

        return new License(document);
    }
}
