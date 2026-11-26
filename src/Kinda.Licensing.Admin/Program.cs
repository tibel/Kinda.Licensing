using System;
using System.Collections.Generic;
using System.CommandLine;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Xml;
using System.Xml.Linq;
using Kinda.Licensing;

namespace Kinda.Licensing.Admin;

internal static class Program
{
    public static int Main(string[] args)
    {
        return BuildRootCommand().Parse(args).Invoke();
    }

    private static RootCommand BuildRootCommand()
    {
        var privateKeyOption = new Option<string>("--private-key")
        {
            Description = "Issuer RSA private key in PEM format.",
            Required = true
        };
        var outputOption = new Option<string>("--output")
        {
            Description = "Path for the signed license XML.",
            Required = true
        };
        var typeOption = new Option<string>("--type")
        {
            Description = "License type.",
            Required = true
        };
        var expiresOption = new Option<string>("--expires")
        {
            Description = "License expiry as an ISO 8601 date-time.",
            Required = true
        };
        var metadataOption = new Option<string[]>("--metadata")
        {
            Description = "License metadata in name=value format; may be repeated.",
            AllowMultipleArgumentsPerToken = true
        };
        var publicKeyOutputOption = new Option<string>("--public-key-output")
        {
            Description = "Optional path for the exported public key in PEM format."
        };

        var issueCommand = new Command("issue", "Issue a signed license with at most a seven-day lease.")
        {
            privateKeyOption,
            outputOption,
            typeOption,
            expiresOption,
            metadataOption,
            publicKeyOutputOption
        };
        issueCommand.SetAction(parseResult => Execute(() => Issue(
            RequiredValue(parseResult, privateKeyOption),
            RequiredValue(parseResult, outputOption),
            RequiredValue(parseResult, typeOption),
            RequiredValue(parseResult, expiresOption),
            parseResult.GetValue(publicKeyOutputOption),
            parseResult.GetValue(metadataOption) ?? [])));

        var licenseOption = new Option<string>("--license")
        {
            Description = "Path to the license XML.",
            Required = true
        };
        var publicKeyOption = new Option<string>("--public-key")
        {
            Description = "Issuer RSA public key in PEM format.",
            Required = true
        };
        var inspectCommand = new Command("inspect", "Verify and display a license.")
        {
            licenseOption,
            publicKeyOption
        };
        inspectCommand.SetAction(parseResult => Execute(() => Inspect(
            RequiredValue(parseResult, licenseOption),
            RequiredValue(parseResult, publicKeyOption))));

        var rootCommand = new RootCommand("Issue and inspect signed Kinda.Licensing licenses.")
        {
            issueCommand,
            inspectCommand
        };
        return rootCommand;
    }

    private static int Execute(Func<int> action)
    {
        try
        {
            return action();
        }
        catch (AggregateException exception)
        {
            foreach (var innerException in exception.InnerExceptions)
                Console.Error.WriteLine($"Error: {innerException.Message}");
            return 1;
        }
        catch (Exception exception) when (
            exception is ArgumentException or
            IOException or
            UnauthorizedAccessException or
            CryptographicException or
            XmlException or
            FormatException or
            InvalidOperationException or
            InvalidLicenseException or
            LicenseViolationException or
            KeyNotFoundException)
        {
            Console.Error.WriteLine($"Error: {exception.Message}");
            return 1;
        }
    }

    private static string RequiredValue(ParseResult parseResult, Option<string> option) =>
        parseResult.GetValue(option) ?? throw new InvalidOperationException($"Required option is missing: {option.Name}");

    private static int Issue(
        string privateKeyInput,
        string outputInput,
        string type,
        string expirationText,
        string? publicKeyOutput,
        IReadOnlyList<string> metadataValues)
    {
        var privateKeyPath = Path.GetFullPath(privateKeyInput);
        var outputPath = Path.GetFullPath(outputInput);
        if (publicKeyOutput is not null)
            publicKeyOutput = Path.GetFullPath(publicKeyOutput);

        if (string.IsNullOrWhiteSpace(type))
            throw new ArgumentException("License type cannot be empty.", "--type");
        if (!DateTimeOffset.TryParse(
                expirationText,
                CultureInfo.InvariantCulture,
                DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal,
                out var expirationDate))
            throw new ArgumentException("--expires must be an ISO 8601 date-time.", "--expires");

        var metadata = ParseMetadata(metadataValues);
        ValidateOutputPaths(privateKeyPath, outputPath, publicKeyOutput);

        var issueDate = DateTimeOffset.UtcNow;
        var criteria = new LicenseCriteria
        {
            Id = Guid.NewGuid(),
            IssueDate = issueDate,
            ExpirationDate = expirationDate,
            Type = type,
            MetaData = metadata
        };
        new LicenseOfflineLeaseRule().Validate(criteria);

        using var privateKey = RSA.Create();
        privateKey.ImportFromPem(File.ReadAllText(privateKeyPath));
        var license = new LicenseGenerator().Generate(privateKey, criteria);
        new LicenseValidator().Validate(license, privateKey, [new LicenseOfflineLeaseRule()]);

        if (publicKeyOutput is not null)
            WriteNewTextFile(publicKeyOutput, privateKey.ExportSubjectPublicKeyInfoPem());

        using (var stream = new FileStream(outputPath, FileMode.CreateNew, FileAccess.Write, FileShare.None))
            license.Document.Save(stream, SaveOptions.DisableFormatting);

        Console.WriteLine($"Issued license {criteria.Id:D} to {outputPath}");
        return 0;
    }

    private static int Inspect(string licenseInput, string publicKeyInput)
    {
        var licensePath = Path.GetFullPath(licenseInput);
        var publicKeyPath = Path.GetFullPath(publicKeyInput);

        using var stream = File.OpenRead(licensePath);
        var license = License.Load(stream);
        using var publicKey = RSA.Create();
        publicKey.ImportFromPem(File.ReadAllText(publicKeyPath));

        var criteria = new LicenseValidator().Validate(
            license,
            publicKey,
            [new LicenseOfflineLeaseRule()]);

        Console.WriteLine($"Signature: valid");
        Console.WriteLine($"Id: {criteria.Id:D}");
        Console.WriteLine($"Type: {criteria.Type}");
        Console.WriteLine($"Issued: {criteria.IssueDate:O}");
        Console.WriteLine($"Expires: {criteria.ExpirationDate:O}");
        foreach (var item in criteria.MetaData.OrderBy(item => item.Key, StringComparer.Ordinal))
            Console.WriteLine($"Metadata {item.Key}: {item.Value}");
        return 0;
    }

    private static Dictionary<string, string> ParseMetadata(IEnumerable<string> values)
    {
        var metadata = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var value in values)
        {
            var separator = value.IndexOf('=');
            if (separator <= 0)
                throw new ArgumentException("Metadata must use the format name=value.", "--metadata");

            var name = value[..separator];
            if (name is "Id" or "IssueDate" or "Type" or "ExpirationDate")
                throw new ArgumentException($"Metadata name '{name}' is reserved.", "--metadata");

            XmlConvert.VerifyNCName(name);
            if (!metadata.TryAdd(name, value[(separator + 1)..]))
                throw new ArgumentException($"Metadata name '{name}' was specified more than once.", "--metadata");
        }

        return metadata;
    }

    private static void ValidateOutputPaths(string privateKeyPath, string outputPath, string? publicKeyOutput)
    {
        var paths = new List<string> { privateKeyPath, outputPath };
        if (publicKeyOutput is not null)
            paths.Add(publicKeyOutput);

        if (paths.Distinct(StringComparer.OrdinalIgnoreCase).Count() != paths.Count)
            throw new ArgumentException("Input and output paths must be different.");
        if (File.Exists(outputPath))
            throw new IOException($"Refusing to overwrite existing file: {outputPath}");
        if (publicKeyOutput is not null && File.Exists(publicKeyOutput))
            throw new IOException($"Refusing to overwrite existing file: {publicKeyOutput}");
    }

    private static void WriteNewTextFile(string path, string content)
    {
        using var stream = new FileStream(path, FileMode.CreateNew, FileAccess.Write, FileShare.None);
        using var writer = new StreamWriter(stream, new UTF8Encoding(false));
        writer.Write(content);
    }
}
