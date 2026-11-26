using System.Security.Cryptography;
using System.Security.Cryptography.Xml;
using System.Xml;
using System.Xml.Linq;

namespace Kinda.Licensing.Tests;

public class LicenseTests
{
    [Test]
    public async Task LicenseCriteriaInitializesNonNullableProperties()
    {
        var criteria = new LicenseCriteria();

        await Assert.That(criteria.Type).IsEqualTo(string.Empty);
        await Assert.That(criteria.MetaData).IsNotNull();
        await Assert.That(criteria.MetaData.Count).IsEqualTo(0);
    }

    [Test]
    public async Task GeneratedLicenseRoundTripsAndRunsRules()
    {
        using var privateKey = RSA.Create();
        using var publicKey = CreatePublicKey(privateKey);
        var expected = CreateCriteria(DateTimeOffset.UtcNow.AddDays(1));
        var license = new LicenseGenerator().Generate(privateKey, expected);

        var metadataRule = new MetadataRule();
        var actual = new LicenseValidator().Validate(
            license, publicKey, [new SimpleLicenseHasNotExpiredRule(), metadataRule]);

        await Assert.That(actual.Id).IsEqualTo(expected.Id);
        await Assert.That(actual.Type).IsEqualTo(expected.Type);
        await Assert.That(actual.MetaData["LicensedCores"]).IsEqualTo(expected.MetaData["LicensedCores"]);
        await Assert.That(actual.MetaData["Signature"]).IsEqualTo(expected.MetaData["Signature"]);
        await Assert.That(metadataRule.WasCalled).IsTrue();
    }

    [Test]
    public async Task GeneratedLicenseCanBeVerifiedIndependentlyAsXmlDsig()
    {
        using var privateKey = RSA.Create();
        var publicKeyPem = privateKey.ExportSubjectPublicKeyInfoPem();
        var license = new LicenseGenerator().Generate(privateKey, CreateCriteria(DateTimeOffset.UtcNow.AddDays(1)));
        var xmlDocument = new XmlDocument { PreserveWhitespace = true };
        xmlDocument.LoadXml(license.Document.ToString(SaveOptions.DisableFormatting));
        var signature = (XmlElement)xmlDocument.GetElementsByTagName(
            "Signature", SignedXml.XmlDsigNamespaceUrl)[0]!;
        var signedXml = new SignedXml(xmlDocument);
        signedXml.LoadXml(signature);

        using var rsa = RSA.Create();
        rsa.ImportFromPem(publicKeyPem);

        await Assert.That(signedXml.CheckSignature(rsa)).IsTrue();
    }

    [Test]
    public async Task RejectsTamperingAndWrongKeyWithoutChangingDocument()
    {
        using var privateKey = RSA.Create();
        using var publicKey = CreatePublicKey(privateKey);
        using var wrongKey = RSA.Create();
        var license = new LicenseGenerator().Generate(privateKey, CreateCriteria(DateTimeOffset.UtcNow.AddDays(1)));
        var original = license.Document.ToString(SaveOptions.DisableFormatting);

        await Assert.That(new DocumentSigning().Validate(license.Document, wrongKey)).IsFalse();
        await Assert.That(license.Document.ToString(SaveOptions.DisableFormatting)).IsEqualTo(original);

        license.Document.Root!.Element("Type")!.Value = "Tampered";
        var tampered = license.Document.ToString(SaveOptions.DisableFormatting);
        await Assert.That(new DocumentSigning().Validate(license.Document, publicKey)).IsFalse();
        await Assert.That(license.Document.ToString(SaveOptions.DisableFormatting)).IsEqualTo(tampered);
    }

    [Test]
    public async Task ReportsExpiredLicenseAsRuleViolation()
    {
        using var key = RSA.Create();
        using var publicKey = CreatePublicKey(key);
        var license = new LicenseGenerator().Generate(key, CreateCriteria(DateTimeOffset.UtcNow.AddDays(-1)));

        var exception = await Assert.That(() => new LicenseValidator().Validate(
            license, publicKey, [new SimpleLicenseHasNotExpiredRule()]))
            .ThrowsExactly<AggregateException>();

        await Assert.That(exception!.InnerExceptions.Count).IsEqualTo(1);
        await Assert.That(exception.InnerExceptions[0] is LicenseExpiredException).IsTrue();
    }

    [Test]
    [Arguments("unsupported-algorithm")]
    [Arguments("unsupported-transform")]
    [Arguments("missing-signature-value")]
    [Arguments("invalid-signature-value")]
    [Arguments("external-reference")]
    [Arguments("multiple-signatures")]
    public async Task MalformedSignaturesAreRejectedWithoutChangingDocument(string malformed)
    {
        using var key = RSA.Create();
        using var publicKey = CreatePublicKey(key);
        var license = new LicenseGenerator().Generate(key, CreateCriteria(DateTimeOffset.UtcNow.AddDays(1)));
        XNamespace signatureNamespace = SignedXml.XmlDsigNamespaceUrl;
        var signature = license.Document.Root!.Element(signatureNamespace + "Signature")!;
        switch (malformed)
        {
            case "unsupported-algorithm":
                signature.Element(signatureNamespace + "SignedInfo")!
                    .Element(signatureNamespace + "SignatureMethod")!
                    .SetAttributeValue("Algorithm", SignedXml.XmlDsigRSASHA1Url);
                break;
            case "unsupported-transform":
                signature.Element(signatureNamespace + "SignedInfo")!
                    .Element(signatureNamespace + "Reference")!
                    .Element(signatureNamespace + "Transforms")!
                    .Elements(signatureNamespace + "Transform")
                    .First()
                    .SetAttributeValue("Algorithm", SignedXml.XmlDsigXsltTransformUrl);
                break;
            case "missing-signature-value":
                signature.Element(signatureNamespace + "SignatureValue")!.Remove();
                break;
            case "invalid-signature-value":
                signature.Element(signatureNamespace + "SignatureValue")!.Value = "not base64";
                break;
            case "external-reference":
                signature.Element(signatureNamespace + "SignedInfo")!
                    .Element(signatureNamespace + "Reference")!
                    .SetAttributeValue("URI", "https://example.invalid/license");
                break;
            case "multiple-signatures":
                license.Document.Root!.Add(new XElement(signature));
                break;
        }
        var before = license.Document.ToString(SaveOptions.DisableFormatting);

        await Assert.That(() => new LicenseValidator().Validate(
            license, publicKey, [])).ThrowsExactly<InvalidLicenseException>();
        await Assert.That(license.Document.ToString(SaveOptions.DisableFormatting)).IsEqualTo(before);
    }

    [Test]
    public async Task EmptyDocumentIsRejected()
    {
        using var publicKey = RSA.Create();

        await Assert.That(new DocumentSigning().Validate(new XDocument(), publicKey)).IsFalse();
    }

    [Test]
    public async Task CannotSignDocumentWithoutRoot()
    {
        using var key = RSA.Create();

        await Assert.That(() => new DocumentSigning().Sign(new XDocument(), key))
            .ThrowsExactly<InvalidOperationException>();
    }

    private static LicenseCriteria CreateCriteria(DateTimeOffset expiration) => new()
    {
        Id = Guid.NewGuid(),
        IssueDate = DateTimeOffset.UtcNow,
        ExpirationDate = expiration,
        Type = "Subscription",
        MetaData = new Dictionary<string, string>
        {
            ["LicensedCores"] = "2",
            ["Signature"] = "metadata value"
        }
    };

    private static RSA CreatePublicKey(RSA privateKey)
    {
        var publicKey = RSA.Create();
        publicKey.ImportFromPem(privateKey.ExportSubjectPublicKeyInfoPem());
        return publicKey;
    }

    private sealed class MetadataRule : ILicenseValidationRule
    {
        public bool WasCalled { get; private set; }

        public void Validate(LicenseCriteria licenseCriteria)
        {
            WasCalled = true;
            if (licenseCriteria.MetaData["LicensedCores"] != "2")
                throw new LicenseViolationException("Invalid licensed core count.");
        }
    }
}
