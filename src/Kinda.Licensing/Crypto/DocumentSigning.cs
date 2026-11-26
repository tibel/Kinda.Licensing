using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Security.Cryptography;
using System.Security.Cryptography.Xml;
using System.Xml;
using System.Xml.Linq;

namespace Kinda.Licensing;

/// <summary>Signs and validates license documents using the library's XML Digital Signature profile.</summary>
public class DocumentSigning
{
    private static readonly XNamespace XmlDsigNamespace = SignedXml.XmlDsigNamespaceUrl;
    private static readonly XName SignatureName = XmlDsigNamespace + "Signature";

    /// <summary>
    /// Adds an enveloped XMLDSIG signature over the entire document.
    /// </summary>
    /// <param name="document">The document to sign. The signature is appended to its root element.</param>
    /// <param name="privateKey">The caller-owned RSA private key used to sign the document.</param>
    /// <exception cref="ArgumentNullException"><paramref name="document"/> or <paramref name="privateKey"/> is <see langword="null"/>.</exception>
    /// <exception cref="InvalidOperationException">The document has no root element or already contains an XMLDSIG signature.</exception>
    /// <exception cref="CryptographicException">The key cannot sign the document.</exception>
    [UnconditionalSuppressMessage("Trimming", "IL2026", Justification = "Signing uses only fixed XMLDSIG algorithms.")]
    [UnconditionalSuppressMessage("AOT", "IL3050", Justification = "Signing uses only fixed XMLDSIG algorithms.")]
    [DynamicDependency(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor, "System.Security.Cryptography.SHA256Managed", "System.Security.Cryptography")]
    [DynamicDependency(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor, typeof(RSAPKCS1SignatureFormatter))]
    public void Sign(XDocument document, RSA privateKey)
    {
        ArgumentNullException.ThrowIfNull(document);
        ArgumentNullException.ThrowIfNull(privateKey);

        var root = document.Root ?? throw new InvalidOperationException("Document must have a root element.");
        if (root.Descendants(SignatureName).Any())
            throw new InvalidOperationException("Already signed");

        var xmlDocument = ToXmlDocument(document);
        var signedXml = new SignedXml(xmlDocument)
        {
            SigningKey = privateKey
        };
        var signedInfo = signedXml.SignedInfo;
        if (signedInfo == null)
            throw new InvalidOperationException("XML signature information is unavailable.");
        signedInfo.CanonicalizationMethod = SignedXml.XmlDsigC14NTransformUrl;
        signedInfo.SignatureMethod = SignedXml.XmlDsigRSASHA256Url;

        var reference = new Reference
        {
            Uri = string.Empty,
            DigestMethod = SignedXml.XmlDsigSHA256Url
        };
        reference.AddTransform(new XmlDsigEnvelopedSignatureTransform());
        reference.AddTransform(new XmlDsigC14NTransform());
        signedXml.AddReference(reference);
        signedXml.ComputeSignature();

        root.Add(XElement.Parse(signedXml.GetXml().OuterXml));
    }

    /// <summary>
    /// Verifies the document's XMLDSIG signature using the supplied trusted public key.
    /// </summary>
    /// <param name="document">The signed document to verify.</param>
    /// <param name="publicKey">The caller-owned RSA public key trusted to verify the document.</param>
    /// <returns><see langword="true"/> if the document has one supported, valid signature; otherwise, <see langword="false"/>.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="document"/> or <paramref name="publicKey"/> is <see langword="null"/>.</exception>
    /// <remarks>The accepted profile signs the whole document with an enveloped RSA-SHA256 XMLDSIG signature. The document is not modified.</remarks>
    [UnconditionalSuppressMessage("Trimming", "IL2026", Justification = "Validation rejects unapproved XMLDSIG algorithms before parsing the signature.")]
    [UnconditionalSuppressMessage("AOT", "IL3050", Justification = "Validation rejects unapproved XMLDSIG algorithms before parsing the signature.")]
    [DynamicDependency(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor, "System.Security.Cryptography.SHA256Managed", "System.Security.Cryptography")]
    [DynamicDependency(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor, typeof(RSAPKCS1SignatureDeformatter))]
    public bool Validate(XDocument document, RSA publicKey)
    {
        ArgumentNullException.ThrowIfNull(document);
        ArgumentNullException.ThrowIfNull(publicKey);

        var root = document.Root;
        var signatures = document.Descendants(SignatureName).ToArray();
        if (root == null ||
            signatures.Length != 1 ||
            signatures[0].Parent != root ||
            !HasExpectedSignatureProfile(signatures[0]))
            return false;

        var xmlDocument = ToXmlDocument(document);
        var signature = xmlDocument.GetElementsByTagName("Signature", SignedXml.XmlDsigNamespaceUrl)[0] as XmlElement;
        if (signature == null)
            return false;

        var signedXml = new SignedXml(xmlDocument);
        try
        {
            signedXml.LoadXml(signature);
        }
        catch (CryptographicException)
        {
            return false;
        }
        catch (XmlException)
        {
            return false;
        }
        catch (FormatException)
        {
            return false;
        }

        try
        {
            return signedXml.CheckSignature(publicKey);
        }
        catch (CryptographicException)
        {
            return false;
        }
    }

    private static bool HasExpectedSignatureProfile(XElement signature)
    {
        var signatureChildren = signature.Elements().ToArray();
        if (signatureChildren.Length != 2 ||
            signatureChildren[0].Name != XmlDsigNamespace + "SignedInfo" ||
            signatureChildren[1].Name != XmlDsigNamespace + "SignatureValue")
            return false;

        var signedInfoChildren = signatureChildren[0].Elements().ToArray();
        if (signedInfoChildren.Length != 3 ||
            signedInfoChildren[0].Name != XmlDsigNamespace + "CanonicalizationMethod" ||
            (string?)signedInfoChildren[0].Attribute("Algorithm") != SignedXml.XmlDsigC14NTransformUrl ||
            signedInfoChildren[1].Name != XmlDsigNamespace + "SignatureMethod" ||
            (string?)signedInfoChildren[1].Attribute("Algorithm") != SignedXml.XmlDsigRSASHA256Url ||
            signedInfoChildren[2].Name != XmlDsigNamespace + "Reference")
            return false;

        var reference = signedInfoChildren[2];
        var referenceChildren = reference.Elements().ToArray();
        if ((string?)reference.Attribute("URI") != string.Empty ||
            referenceChildren.Length != 3 ||
            referenceChildren[0].Name != XmlDsigNamespace + "Transforms" ||
            referenceChildren[1].Name != XmlDsigNamespace + "DigestMethod" ||
            (string?)referenceChildren[1].Attribute("Algorithm") != SignedXml.XmlDsigSHA256Url ||
            referenceChildren[2].Name != XmlDsigNamespace + "DigestValue")
            return false;

        var transforms = referenceChildren[0].Elements().ToArray();
        if (transforms.Length != 2 ||
            transforms[0].Name != XmlDsigNamespace + "Transform" ||
            (string?)transforms[0].Attribute("Algorithm") != SignedXml.XmlDsigEnvelopedSignatureTransformUrl ||
            transforms[1].Name != XmlDsigNamespace + "Transform" ||
            (string?)transforms[1].Attribute("Algorithm") != SignedXml.XmlDsigC14NTransformUrl)
            return false;

        return true;
    }

    private static XmlDocument ToXmlDocument(XDocument document)
    {
        var xmlDocument = new XmlDocument
        {
            PreserveWhitespace = true,
            XmlResolver = null
        };
        using var reader = document.CreateReader();
        xmlDocument.Load(reader);
        return xmlDocument;
    }
}
