using System;
using System.Collections.Generic;

namespace Kinda.Licensing;

/// <summary>Describes the data and validity period of a software license.</summary>
public sealed class LicenseCriteria
{
    /// <summary>Gets or sets the unique identifier of the license.</summary>
    public Guid Id { get; set; }

    /// <summary>Gets or sets when the license was issued.</summary>
    public DateTimeOffset IssueDate { get; set; }

    /// <summary>Gets or sets when the license expires.</summary>
    public DateTimeOffset ExpirationDate { get; set; }

    /// <summary>Gets or sets the license type.</summary>
    public string Type { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets application-specific license metadata. Each key is serialized as an XML element name.
    /// </summary>
    /// <remarks>Keys must be valid XML element names and must not collide with the reserved license fields.</remarks>
    public Dictionary<string, string> MetaData { get; set; } = new();
}
