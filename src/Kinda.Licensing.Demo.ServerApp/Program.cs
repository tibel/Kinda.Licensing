using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Xml.Linq;

namespace Kinda.Licensing.Demo.ServerApp;

public class Program
{
    public static int Main(string[] args)
    {
        if (args.Length != 1)
        {
            Console.Error.WriteLine("Usage: Kinda.Licensing.Demo.ServerApp <output-directory>");
            return 1;
        }

        var outputDirectory = args[0];
        var publicKeyPath = Path.Combine(outputDirectory, "public-key.pem");
        var licensePath = Path.Combine(outputDirectory, "license.xml");
        if (File.Exists(publicKeyPath) || File.Exists(licensePath))
        {
            Console.Error.WriteLine("The output directory already contains a public key or license.");
            return 1;
        }

        Directory.CreateDirectory(outputDirectory);

        using var privateKey = RSA.Create();
        var publicKey = privateKey.ExportSubjectPublicKeyInfoPem();
        var issueDate = DateTimeOffset.UtcNow;
        var criteria = new LicenseCriteria
        {
            ExpirationDate = issueDate.AddDays(7),
            IssueDate = issueDate,
            Id = Guid.NewGuid(),
            MetaData = new Dictionary<string, string> { { "LicensedCores", Environment.ProcessorCount.ToString() } },
            Type = "Subscription"
        };
        var license = new LicenseGenerator().Generate(privateKey, criteria);

        File.WriteAllText(publicKeyPath, publicKey);

        using (var stream = new FileStream(licensePath, FileMode.CreateNew, FileAccess.Write))
            license.Document.Save(stream, SaveOptions.DisableFormatting);

        Console.WriteLine("Public key: " + publicKeyPath);
        Console.WriteLine("License: " + licensePath);
        Console.WriteLine("The demo private key was not saved.");
        return 0;
    }
}
