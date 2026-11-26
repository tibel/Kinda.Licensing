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

        if (!new DocumentSigning().Validate(license.Document, publicKey))
        {
            throw new InvalidLicenseException();
        }

        return ValidateRules(license, validationRules);
    }

    /// <summary>Verifies a license against multiple trusted keys, then applies each supplied validation rule.</summary>
    /// <param name="license">The signed license to validate.</param>
    /// <param name="publicKeys">Caller-owned trusted RSA public keys, including current and retiring keys during rotation.</param>
    /// <param name="validationRules">The rules to apply after signature verification.</param>
    /// <returns>The validated criteria.</returns>
    /// <exception cref="ArgumentNullException">An argument or key in <paramref name="publicKeys"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="publicKeys"/> contains no keys.</exception>
    /// <exception cref="InvalidLicenseException">The license signature is missing, malformed, or invalid for every supplied key.</exception>
    /// <exception cref="AggregateException">One or more validation rules rejected the license; all supplied rules are evaluated.</exception>
    /// <remarks>Retain retiring public keys until leases signed by them have expired. Key storage and rotation remain the caller's responsibility.</remarks>
    public LicenseCriteria ValidateWithAnyPublicKey(
        License license,
        IEnumerable<RSA> publicKeys,
        IEnumerable<ILicenseValidationRule> validationRules)
    {
        ArgumentNullException.ThrowIfNull(license);
        ArgumentNullException.ThrowIfNull(publicKeys);
        ArgumentNullException.ThrowIfNull(validationRules);

        var keys = new List<RSA>();
        foreach (var publicKey in publicKeys)
        {
            ArgumentNullException.ThrowIfNull(publicKey);
            keys.Add(publicKey);
        }

        if (keys.Count == 0)
            throw new ArgumentException("At least one trusted public key is required.", nameof(publicKeys));

        foreach (var publicKey in keys)
        {
            try
            {
                return Validate(license, publicKey, validationRules);
            }
            catch (InvalidLicenseException)
            {
            }
        }

        throw new InvalidLicenseException();
    }

    private static LicenseCriteria ValidateRules(
        License license,
        IEnumerable<ILicenseValidationRule> validationRules)
    {
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
