using System;
using System.Collections.Generic;

namespace Kinda.Licensing
{
    public sealed class LicenseCriteria
    {
        public Guid Id { get; set; }

        public DateTime IssueDate { get; set; }

        public DateTime ExpirationDate { get; set; }

        public string Type { get; set; }

        public Dictionary<string, string> MetaData { get; set; }
    }
}
