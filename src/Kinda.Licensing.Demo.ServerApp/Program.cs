using System;
using System.Collections.Generic;
using System.IO;

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
        var publicKeyPath = Path.Combine(outputDirectory, "public-key.xml");
        var licensePath = Path.Combine(outputDirectory, "license.xml");
        if (File.Exists(publicKeyPath) || File.Exists(licensePath))
        {
            Console.Error.WriteLine("The output directory already contains a public key or license.");
            return 1;
        }

        Directory.CreateDirectory(outputDirectory);

        var provider = new CryptoKeyProvider();
        var privateKey = provider.GenerateKey();
        var publicKey = provider.ExtractPublicKey(privateKey);
        var criteria = new LicenseCriteria
        {
            ExpirationDate = DateTimeOffset.UtcNow.AddDays(30),
            IssueDate = DateTimeOffset.UtcNow,
            Id = Guid.NewGuid(),
            MetaData = new Dictionary<string, string> { { "LicensedCores", Environment.ProcessorCount.ToString() } },
            Type = "Subscription"
        };
        var license = new LicenseGenerator().Generate(privateKey, criteria);

        using (var stream = new FileStream(publicKeyPath, FileMode.CreateNew, FileAccess.Write))
        using (var writer = new StreamWriter(stream))
            writer.Write(publicKey.Contents);

        using (var stream = new FileStream(licensePath, FileMode.CreateNew, FileAccess.Write))
            license.Document.Save(stream);

        Console.WriteLine("Public key: " + publicKeyPath);
        Console.WriteLine("License: " + licensePath);
        Console.WriteLine("The demo signing key was not saved.");
        return 0;
    }
}
