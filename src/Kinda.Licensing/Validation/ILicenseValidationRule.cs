namespace Kinda.Licensing;

/// <summary>Defines a rule that validates license criteria.</summary>
public interface ILicenseValidationRule
{
    /// <summary>Validates the specified criteria.</summary>
    /// <param name="licenseCriteria">The criteria to validate.</param>
    /// <exception cref="LicenseViolationException">The criteria violates this rule.</exception>
    void Validate(LicenseCriteria licenseCriteria);
}
