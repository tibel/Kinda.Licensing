using System;
using System.Xml.Linq;

namespace Kinda.Licensing
{
    public sealed class License
    {
        public License(XDocument document)
        {
            if (document == null)
                throw new ArgumentNullException(nameof(document));

            Document = document;
        }

        public XDocument Document { get; }
    }
}
