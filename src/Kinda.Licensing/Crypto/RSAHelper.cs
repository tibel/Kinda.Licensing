using System;
using System.Security.Cryptography;
using System.Text;
using System.Xml.Linq;

namespace Kinda.Licensing
{
    /// <summary>
    /// Extensions to <see cref="RSA"/> that are not present .NET Core 2.1 (added in .NET Core 3.0).
    /// </summary>
    internal static class RSAHelper
    {
        public static string ExportToXmlString(RSA rsa, bool includePrivateParameters)
        {
            if (rsa == null)
                throw new ArgumentNullException(nameof(rsa));

            var rsaParams = rsa.ExportParameters(includePrivateParameters);

            var sb = new StringBuilder();
            sb.Append("<RSAKeyValue>");
            sb.Append("<Modulus>" + Convert.ToBase64String(rsaParams.Modulus) + "</Modulus>");
            sb.Append("<Exponent>" + Convert.ToBase64String(rsaParams.Exponent) + "</Exponent>");

            if (includePrivateParameters)
            {
                sb.Append("<P>" + Convert.ToBase64String(rsaParams.P) + "</P>");
                sb.Append("<Q>" + Convert.ToBase64String(rsaParams.Q) + "</Q>");
                sb.Append("<DP>" + Convert.ToBase64String(rsaParams.DP) + "</DP>");
                sb.Append("<DQ>" + Convert.ToBase64String(rsaParams.DQ) + "</DQ>");
                sb.Append("<InverseQ>" + Convert.ToBase64String(rsaParams.InverseQ) + "</InverseQ>");
                sb.Append("<D>" + Convert.ToBase64String(rsaParams.D) + "</D>");
            }

            sb.Append("</RSAKeyValue>");
            return sb.ToString();
        }

        public static void ImportFromXmlString(RSA rsa, string xmlString)
        {
            if (rsa == null)
                throw new ArgumentNullException(nameof(rsa));
            if (xmlString == null)
                throw new ArgumentNullException(nameof(xmlString));

            var document = XDocument.Parse(xmlString);
            var topElement = document.Root;

            var rsaParams = new RSAParameters();

            var modulusString = (string)topElement.Element("Modulus");
            if (modulusString == null)
                throw new CryptographicException("The format of the xmlString parameter is not valid.");
            rsaParams.Modulus = Convert.FromBase64String(modulusString);

            var exponentString = (string)topElement.Element("Exponent");
            if (exponentString == null)
                throw new CryptographicException("The format of the xmlString parameter is not valid.");
            rsaParams.Exponent = Convert.FromBase64String(exponentString);

            var pString = (string)topElement.Element("P");
            if (pString != null)
                rsaParams.P = Convert.FromBase64String(pString);

            var qString = (string)topElement.Element("Q");
            if (qString != null)
                rsaParams.Q = Convert.FromBase64String(qString);

            var dpString = (string)topElement.Element("DP");
            if (dpString != null)
                rsaParams.DP = Convert.FromBase64String(dpString);

            var dqString = (string)topElement.Element("DQ");
            if (dqString != null)
                rsaParams.DQ = Convert.FromBase64String(dqString);

            var inverseQString = (string)topElement.Element("InverseQ");
            if (inverseQString != null)
                rsaParams.InverseQ = Convert.FromBase64String(inverseQString);

            var dString = (string)topElement.Element("D");
            if (dString != null)
                rsaParams.D = Convert.FromBase64String(dString);

            rsa.ImportParameters(rsaParams);
        }
    }
}
