using System;
using System.Security.Cryptography;
using System.Text;

namespace Kinda.Licensing;

/// <summary>Creates a machine code from an application-supplied device identifier.</summary>
public static class MachineCodeGenerator
{
    /// <summary>The license metadata key used to store a machine code.</summary>
    public const string MetadataKey = "MachineCode";

    /// <summary>Generates an uppercase SHA-256 code from the UTF-8 bytes of a device identifier.</summary>
    /// <param name="deviceId">A stable, opaque identifier supplied and persisted by the host application.</param>
    /// <returns>The uppercase hexadecimal SHA-256 hash of <paramref name="deviceId"/>.</returns>
    /// <exception cref="ArgumentException"><paramref name="deviceId"/> is null, empty, or whitespace.</exception>
    /// <remarks>
    /// This method does not collect hardware information. Hashing does not make low-entropy or personal
    /// identifiers secret, and the result does not prevent a user from bypassing validation in a client they control.
    /// </remarks>
    public static string Generate(string deviceId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(deviceId);

        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(deviceId)));
    }
}
