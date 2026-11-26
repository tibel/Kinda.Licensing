using System.Xml.Linq;

namespace Kinda.Licensing.Tests;

public class LicenseTests
{
    [Test]
    public async Task GeneratedLicenseRoundTripsAndRunsRules()
    {
        var provider = new CryptoKeyProvider();
        var privateKey = provider.GenerateKey();
        var publicKey = provider.ExtractPublicKey(privateKey);
        var expected = CreateCriteria(DateTimeOffset.UtcNow.AddDays(1));
        var license = new LicenseGenerator().Generate(privateKey, expected);

        var metadataRule = new MetadataRule();
        var actual = new LicenseValidator().Validate(
            license, publicKey, [new SimpleLicenseHasNotExpiredRule(), metadataRule]);

        await Assert.That(actual.Id).IsEqualTo(expected.Id);
        await Assert.That(actual.Type).IsEqualTo(expected.Type);
        await Assert.That(actual.MetaData["LicensedCores"]).IsEqualTo(expected.MetaData["LicensedCores"]);
        await Assert.That(metadataRule.WasCalled).IsTrue();
    }

    [Test]
    public async Task RejectsTamperingAndWrongKeyWithoutChangingDocument()
    {
        var provider = new CryptoKeyProvider();
        var privateKey = provider.GenerateKey();
        var license = new LicenseGenerator().Generate(privateKey, CreateCriteria(DateTimeOffset.UtcNow.AddDays(1)));
        var wrongKey = provider.ExtractPublicKey(provider.GenerateKey());
        var original = license.Document.ToString(SaveOptions.DisableFormatting);

        await Assert.That(new DocumentSigning().Validate(license.Document, wrongKey)).IsFalse();
        await Assert.That(license.Document.ToString(SaveOptions.DisableFormatting)).IsEqualTo(original);

        license.Document.Root!.Element("Type")!.Value = "Tampered";
        var tampered = license.Document.ToString(SaveOptions.DisableFormatting);
        await Assert.That(new DocumentSigning().Validate(license.Document, provider.ExtractPublicKey(privateKey))).IsFalse();
        await Assert.That(license.Document.ToString(SaveOptions.DisableFormatting)).IsEqualTo(tampered);
    }

    [Test]
    public async Task ReportsExpiredLicenseAsRuleViolation()
    {
        var provider = new CryptoKeyProvider();
        var key = provider.GenerateKey();
        var license = new LicenseGenerator().Generate(key, CreateCriteria(DateTimeOffset.UtcNow.AddDays(-1)));

        var exception = await Assert.That(() => new LicenseValidator().Validate(
            license, provider.ExtractPublicKey(key), [new SimpleLicenseHasNotExpiredRule()]))
            .ThrowsExactly<AggregateException>();

        await Assert.That(exception!.InnerExceptions.Count).IsEqualTo(1);
        await Assert.That(exception.InnerExceptions[0] is LicenseExpiredException).IsTrue();
    }

    [Test]
    [Arguments("missing-algorithm")]
    [Arguments("missing-signature-value")]
    [Arguments("invalid-signature-value")]
    public async Task MalformedSignaturesAreRejectedWithoutChangingDocument(string malformed)
    {
        var provider = new CryptoKeyProvider();
        var key = provider.GenerateKey();
        var license = new LicenseGenerator().Generate(key, CreateCriteria(DateTimeOffset.UtcNow.AddDays(1)));
        var signature = license.Document.Root!.Element("Signature")!;
        switch (malformed)
        {
            case "missing-algorithm":
                signature.Attribute("Algorithm")!.Remove();
                break;
            case "missing-signature-value":
                signature.Element("SignatureValue")!.Remove();
                break;
            case "invalid-signature-value":
                signature.Element("SignatureValue")!.Value = "not base64";
                break;
        }
        var before = license.Document.ToString(SaveOptions.DisableFormatting);

        await Assert.That(() => new LicenseValidator().Validate(
            license, provider.ExtractPublicKey(key), [])).ThrowsExactly<InvalidLicenseException>();
        await Assert.That(license.Document.ToString(SaveOptions.DisableFormatting)).IsEqualTo(before);
    }

    [Test]
    public async Task EmptyDocumentIsRejected()
    {
        var provider = new CryptoKeyProvider();
        var publicKey = provider.ExtractPublicKey(provider.GenerateKey());

        await Assert.That(new DocumentSigning().Validate(new XDocument(), publicKey)).IsFalse();
    }

    private static LicenseCriteria CreateCriteria(DateTimeOffset expiration) => new()
    {
        Id = Guid.NewGuid(),
        IssueDate = DateTimeOffset.UtcNow,
        ExpirationDate = expiration,
        Type = "Subscription",
        MetaData = new Dictionary<string, string> { ["LicensedCores"] = "2" }
    };

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
