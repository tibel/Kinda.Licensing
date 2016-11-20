using System;
using System.Collections.Generic;

namespace Kinda.Licensing.Demo.ServerApp
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var cryptoKeyProvider = new CryptoKeyProvider();

            var privateKey = cryptoKeyProvider.GenerateKey();
            var publicKey = cryptoKeyProvider.ExtractPublicKey(privateKey);

            Console.WriteLine("Public Key:");
            Console.WriteLine(publicKey.Contents);
            Console.WriteLine();

            var licenseCriteria = new LicenseCriteria
            {
                ExpirationDate = DateTimeOffset.UtcNow.AddDays(30),
                IssueDate = DateTimeOffset.UtcNow,
                Id = Guid.NewGuid(),
                MetaData = new Dictionary<string, string> { { "LicensedCores", "2" } },
                Type = "Subscription"
            };

            var license = new LicenseGenerator().Generate(privateKey, licenseCriteria);

            Console.WriteLine(license.Document);
            Console.WriteLine();

            Console.ReadKey();
        }
    }
}
