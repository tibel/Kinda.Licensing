using System;
using System.Xml.Linq;

namespace Kinda.Licensing;

/// <summary>Represents a signed XML license document.</summary>
public sealed class License
{
    /// <summary>Initializes a license with its XML document.</summary>
    /// <param name="document">The license XML document.</param>
    /// <exception cref="ArgumentNullException"><paramref name="document"/> is <see langword="null"/>.</exception>
    public License(XDocument document)
    {
        ArgumentNullException.ThrowIfNull(document);

        Document = document;
    }

    /// <summary>Gets the underlying XML document.</summary>
    /// <remarks>The document is mutable; changing signed content invalidates its signature.</remarks>
    public XDocument Document { get; }
}
