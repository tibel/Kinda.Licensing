using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Security.Cryptography.Xml;
using System.Xml.Linq;

namespace Kinda.Licensing;

/// <summary>Serializes and deserializes license criteria as an XML document.</summary>
public class LicenseCriteriaSerializer
{
    private static readonly XName SignatureName = XName.Get(LicenseElements.Signature, SignedXml.XmlDsigNamespaceUrl);

    /// <summary>Serializes the specified criteria into a license XML document.</summary>
    /// <param name="licenseCriteria">The criteria to serialize.</param>
    /// <returns>An XML document containing the license criteria.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="licenseCriteria"/> is <see langword="null"/>.</exception>
    public XDocument Serialize(LicenseCriteria licenseCriteria)
    {
        ArgumentNullException.ThrowIfNull(licenseCriteria);

        var document = new XDocument();

        var licenseElement = new XElement(LicenseElements.License);
        document.Add(licenseElement);

        var id = new XElement(LicenseElements.Id, licenseCriteria.Id.ToString("D"));
        licenseElement.Add(id);

        var issueDate = new XElement(LicenseElements.IssueDate, FormatDateTime(licenseCriteria.IssueDate));
        licenseElement.Add(issueDate);

        var type = new XElement(LicenseElements.Type, licenseCriteria.Type);
        licenseElement.Add(type);

        var expirationDate = new XElement(LicenseElements.ExpirationDate, FormatDateTime(licenseCriteria.ExpirationDate));
        licenseElement.Add(expirationDate);

        foreach (var metaData in licenseCriteria.MetaData)
        {
            var element = new XElement(metaData.Key, metaData.Value);
            licenseElement.Add(element);
        }

        return document;
    }

    /// <summary>Deserializes license criteria from an XML document.</summary>
    /// <param name="document">The XML document to deserialize.</param>
    /// <returns>The license criteria contained in the document.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="document"/> is <see langword="null"/>.</exception>
    /// <exception cref="InvalidDataException">The document does not have a license root element.</exception>
    public LicenseCriteria Deserialize(XDocument document)
    {
        ArgumentNullException.ThrowIfNull(document);

        var license = document.Root;
        if (license == null || license.Name != LicenseElements.License)
            throw new InvalidDataException("Document is invalid. A root node was expected");

        var licenseDetails = new Dictionary<string, string>();

        foreach (var element in license.Elements())
        {
            if (element.Name == SignatureName)
                continue;

            licenseDetails.Add(element.Name.LocalName, element.Value);
        }

        var licenseCriteria = new LicenseCriteria
        {
            Id = Guid.ParseExact(licenseDetails[LicenseElements.Id], "D"),
            IssueDate = ParseDateTime(licenseDetails[LicenseElements.IssueDate]),
            Type = licenseDetails[LicenseElements.Type],
            ExpirationDate = ParseDateTime(licenseDetails[LicenseElements.ExpirationDate]),
        };

        licenseDetails.Remove(LicenseElements.Id);
        licenseDetails.Remove(LicenseElements.IssueDate);
        licenseDetails.Remove(LicenseElements.Type);
        licenseDetails.Remove(LicenseElements.ExpirationDate);

        licenseCriteria.MetaData = licenseDetails;

        return licenseCriteria;
    }

    private static DateTimeOffset ParseDateTime(string s)
    {
        return DateTimeOffset.ParseExact(s, "r", CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal);
    }

    private static string FormatDateTime(DateTimeOffset d)
    {
        return d.ToString("r");
    }
}
