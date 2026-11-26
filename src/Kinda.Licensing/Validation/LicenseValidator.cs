using System;
using System.Collections.Generic;
using System.Security.Cryptography;

namespace Kinda.Licensing;

/// <summary>Verifies signed licenses and applies validation rules.</summary>
public sealed class LicenseValidator
{
    /// <summary>Verifies a license and applies each supplied validation rule.</summary>
    /// <param name="license">The signed license to validate.</param>
    /// <param name="publicKey">The caller-owned RSA public key trusted to verify the license.</param>
    /// <param name="validationRules">The rules to apply after signature verification.</param>
    /// <returns>The validated criteria.</returns>
    /// <exception cref="ArgumentNullException">An argument is <see langword="null"/>.</exception>
    /// <exception cref="InvalidLicenseException">The license signature is missing, malformed, or invalid.</exception>
    /// <exception cref="AggregateException">One or more validation rules rejected the license; all supplied rules are evaluated.</exception>
    public LicenseCriteria Validate(License license, RSA publicKey, IEnumerable<ILicenseValidationRule> validationRules)
    {
        ArgumentNullException.ThrowIfNull(license);
        ArgumentNullException.ThrowIfNull(publicKey);
        ArgumentNullException.ThrowIfNull(validationRules);

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
