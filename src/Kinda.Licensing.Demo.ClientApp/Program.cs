using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Xml.Linq;

namespace Kinda.Licensing.Demo.ClientApp;

public class Program
{
    public static int Main(string[] args)
    {
        if (args.Length != 1)
        {
            Console.Error.WriteLine("Usage: Kinda.Licensing.Demo.ClientApp <license-directory>");
            return 1;
        }

        try
        {
            using var publicKey = RSA.Create();
            publicKey.ImportFromPem(File.ReadAllText(Path.Combine(args[0], "public-key.pem")));
            var license = new License(XDocument.Load(Path.Combine(args[0], "license.xml")));
            var rules = new List<ILicenseValidationRule>
            {
                new SimpleLicenseHasNotExpiredRule(),
                new ValidNumberOfCoresLicenseRule()
            };

            new LicenseValidator().Validate(license, publicKey, rules);
            Console.WriteLine("No license violations");
            return 0;
        }
        catch (InvalidLicenseException)
        {
            Console.Error.WriteLine("The license signature is invalid.");
        }
        catch (AggregateException exception)
        {
            foreach (var error in exception.InnerExceptions)
                Console.Error.WriteLine(error.Message);
        }
        catch (Exception exception)
        {
            Console.Error.WriteLine("Could not validate the license: " + exception.Message);
        }

        return 1;
    }
}
