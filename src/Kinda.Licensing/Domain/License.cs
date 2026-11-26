using System;
using System.IO;
using System.Xml;
using System.Xml.Linq;

namespace Kinda.Licensing;

/// <summary>Represents a signed XML license document.</summary>
public sealed class License
{
    private const long DefaultMaximumCharactersInDocument = 1_000_000;

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

    /// <summary>Loads a license from an XML stream with a one-million-character document limit.</summary>
    /// <param name="stream">The stream containing the license XML. The stream remains open.</param>
    /// <returns>The loaded license.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="stream"/> is <see langword="null"/>.</exception>
    /// <exception cref="XmlException">The XML is invalid, contains a DTD, or exceeds the document size limit.</exception>
    /// <exception cref="IOException">An I/O error occurs while reading the stream.</exception>
    public static License Load(Stream stream) => Load(stream, DefaultMaximumCharactersInDocument);

    /// <summary>Loads a license from an XML stream with a caller-specified character limit.</summary>
    /// <param name="stream">The stream containing the license XML. The stream remains open.</param>
    /// <param name="maxCharactersInDocument">The maximum number of XML characters to read; must be positive.</param>
    /// <returns>The loaded license.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="stream"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="maxCharactersInDocument"/> is not positive.</exception>
    /// <exception cref="XmlException">The XML is invalid, contains a DTD, or exceeds the document size limit.</exception>
    /// <exception cref="IOException">An I/O error occurs while reading the stream.</exception>
    public static License Load(Stream stream, long maxCharactersInDocument)
    {
        ArgumentNullException.ThrowIfNull(stream);
        if (maxCharactersInDocument <= 0)
            throw new ArgumentOutOfRangeException(nameof(maxCharactersInDocument));

        var settings = new XmlReaderSettings
        {
            CloseInput = false,
            DtdProcessing = DtdProcessing.Prohibit,
            MaxCharactersInDocument = maxCharactersInDocument,
            XmlResolver = null
        };
        using var reader = XmlReader.Create(stream, settings);
        return new License(XDocument.Load(reader, LoadOptions.PreserveWhitespace));
    }
}
