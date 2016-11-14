using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Xml.Linq;

namespace Kinda.Licensing
{
    public class LicenseCriteriaSerializer
    {
        public XDocument Serialize(LicenseCriteria licenseCriteria)
        {
            if (licenseCriteria == null)
                throw new ArgumentNullException(nameof(licenseCriteria));

            var document = new XDocument();

            var licenseElement = new XElement(LicenseElements.License);
            document.Add(licenseElement);

            var id = new XElement(LicenseElements.Id, licenseCriteria.Id.ToString("D"));
            licenseElement.Add(id);

            var issueDate = new XElement(LicenseElements.IssueDate, FormatDateTime(licenseCriteria.IssueDate));
            licenseElement.Add(issueDate);

            var type = new XElement(LicenseElements.Type, licenseCriteria.Type);
            licenseElement.Add(type);

            var expirationDate = new XElement(LicenseElements.ExpirationDate, FormatDateTime(licenseCriteria.ExpirationDate));
            licenseElement.Add(expirationDate);

            foreach (var metaData in licenseCriteria.MetaData)
            {
                var element = new XElement(metaData.Key, metaData.Value);
                licenseElement.Add(element);
            }

            return document;
        }

        public LicenseCriteria Deserialize(XDocument document)
        {
            if (document == null)
                throw new ArgumentNullException(nameof(document));

            var license = document.Root;
            if (license == null || license.Name != LicenseElements.License)
                throw new InvalidDataException("Document is invalid. A root node was expected");

            var licenseDetails = new Dictionary<string, string>();

            foreach (var element in license.Elements())
            {
                if (element.Name.LocalName == LicenseElements.Signature)
                    continue;

                licenseDetails.Add(element.Name.LocalName, element.Value);
            }

            var licenseCriteria = new LicenseCriteria
            {
                Id = Guid.ParseExact(licenseDetails[LicenseElements.Id], "D"),
                IssueDate = ParseDateTime(licenseDetails[LicenseElements.IssueDate]),
                Type = licenseDetails[LicenseElements.Type],
                ExpirationDate = ParseDateTime(licenseDetails[LicenseElements.ExpirationDate]),
            };

            licenseDetails.Remove(LicenseElements.Id);
            licenseDetails.Remove(LicenseElements.IssueDate);
            licenseDetails.Remove(LicenseElements.Type);
            licenseDetails.Remove(LicenseElements.ExpirationDate);
            
            licenseCriteria.MetaData = licenseDetails;

            return licenseCriteria;
        }

        private static DateTime ParseDateTime(string s)
        {
            return DateTime.ParseExact(s, "r", CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal);
        }

        private static string FormatDateTime(DateTime d)
        {
            return d.ToUniversalTime().ToString("r");
        }
    }
}
