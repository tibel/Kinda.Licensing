using System;

namespace Kinda.Licensing;

/// <summary>Validates that a license is bound to an application-supplied device identifier.</summary>
public sealed class LicenseMachineCodeRule : ILicenseValidationRule
{
    private readonly string _machineCode;

    /// <summary>Initializes the rule for the specified application-supplied device identifier.</summary>
    /// <param name="deviceId">The stable device identifier supplied by the host application.</param>
    /// <exception cref="ArgumentException"><paramref name="deviceId"/> is null, empty, or whitespace.</exception>
    public LicenseMachineCodeRule(string deviceId)
    {
        _machineCode = MachineCodeGenerator.Generate(deviceId);
    }

    /// <summary>Validates the machine code stored in the license metadata.</summary>
    /// <param name="licenseCriteria">The criteria to validate.</param>
    /// <exception cref="ArgumentNullException"><paramref name="licenseCriteria"/> is null.</exception>
    /// <exception cref="LicenseViolationException">The license has no matching machine code.</exception>
    public void Validate(LicenseCriteria licenseCriteria)
    {
        ArgumentNullException.ThrowIfNull(licenseCriteria);

        if (!licenseCriteria.MetaData.TryGetValue(MachineCodeGenerator.MetadataKey, out var machineCode) ||
            !string.Equals(machineCode, _machineCode, StringComparison.Ordinal))
            throw new LicenseViolationException("License is not valid for this device.");
    }
}
