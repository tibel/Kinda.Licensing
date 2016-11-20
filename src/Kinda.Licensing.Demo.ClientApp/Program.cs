using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;

namespace Kinda.Licensing.Demo.ClientApp
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var publicKey = new CryptoKey(GetPublicKeyString());
            var license = new License(XDocument.Parse(GetLicenseString()));

            ValidateLicense(publicKey, license);

            Console.WriteLine("No license violations");
            Console.WriteLine("Press any key");

            Console.ReadKey();
        }

        private static void ValidateLicense(CryptoKey publicKey, License license)
        {
            var violations = new List<string>();

            try
            {
                var licenseValidationRules = new List<ILicenseValidationRule>
                {
                    new SimpleLicenseHasNotExpiredRule(),
                    new ValidNumberOfCoresLicenseRule()
                };

                new LicenseValidator().Validate(license, publicKey, licenseValidationRules);
            }
            catch (InvalidLicenseException exception)
            {
                violations.Add(exception.Message);
            }
            catch (AggregateException ex)
            {
                var innerExceptions = ex.InnerExceptions;

                foreach (var exception in innerExceptions)
                {
                    if (exception is LicenseViolationException)
                    {
                        violations.Add(exception.Message);
                    }
                }

                if (!violations.Any())
                {
                    throw;
                }
            }
            catch (Exception)
            {
                violations.Add("Unknown license error");
            }

            if (violations.Any())
            {
                Console.WriteLine("License violations encountered:");
                foreach (var violation in violations)
                {
                    Console.WriteLine(" - " + violation);
                }

                Console.WriteLine("Press any key");
                Console.ReadKey();

                Environment.Exit(-1);
            }
        }

        private static string GetPublicKeyString()
        {
            return "<RSAKeyValue><Modulus>mV72Mj7A3IsD2ex2AjdwLuYw0Spb61Yn9nTd6b8hDY5L+X37eUzPKbCO1coQ3U2rxWRY/VxJncQYpSVN7jEnqUj7z87dO0Bd6XZrqHef8pmkg1TrZuLEKZIVsj9dCYw1fEvsSdiYhQxFVLTLlnRKb8eDX6E38PzesV2kPegZUo4+vAstFwPlgPOVLR+UOZBkq4w674WtggmUVeECIaXnOxAJv4FVLnIkjBSI8Vcbt5Dd+hHw9AoaNe6zgF+Ldpv7gKD3AyHLqdrkZ4BlyTsDqvkEkoWqnUVIVrTRQyi6Ld8k20zpUi87vMG/gJ/SNlRGQBAkKsdfoAyHEHJNqOr+mw==</Modulus><Exponent>AQAB</Exponent></RSAKeyValue>";
        }

        private static string GetLicenseString()
        {
            return @"
<License>
  <Id>4b971ba7-7ec2-4cb1-9b3d-3615fec4b1b9</Id>
  <IssueDate>Sun, 20 Nov 2016 10:35:11 GMT</IssueDate>
  <Type>Subscription</Type>
  <ExpirationDate>Tue, 20 Dec 2016 10:35:11 GMT</ExpirationDate>
  <LicensedCores>2</LicensedCores>
  <Signature Algorithm=""rsa-sha256"">
    <DigestValue>8hLlfGql8wDfyXPitM4wiDwbS4pBUSNqKmZI4r51xv0=</DigestValue>
    <SignatureValue>bSIYTvk9d+r/LQUUqfe/J1AdqLRMccn1amdiKSXrjK/qpJbh36tQ12DRiXpIAABRFL/2UjRhVX2Dezyeh5Re4x5fffTcYqo6J8PxvVobWAenty5mcrNpS+KRYAKRdADcyJTKocK6nAfG7VzDlE+zopLc0HWZoSgniuLs55fU2Y4KK1FIbjc3yceIowz1jUOvvfOnN25xSLrbRJ7cttc9BfZV5sdES9prUU4MYPAh5eY8sdnuEZ/OilnrVrq//hsNf2aMnd52VLqU48uefIe31wTbmTJkgtOx27cod45t7yQe1uMgsF7saLRg1DFGz0DIxbYLMiPalCzEC+jaYL1X0w==</SignatureValue>
  </Signature>
</License>
";
        }
    }
}
