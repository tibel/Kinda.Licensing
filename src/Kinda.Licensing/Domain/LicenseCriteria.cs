using System;
using System.Collections.Generic;

namespace Kinda.Licensing
{
    public sealed class LicenseCriteria
    {
        public Guid Id { get; set; }

        public DateTimeOffset IssueDate { get; set; }

        public DateTimeOffset ExpirationDate { get; set; }

        public string Type { get; set; }

        public Dictionary<string, string> MetaData { get; set; }
    }
}
