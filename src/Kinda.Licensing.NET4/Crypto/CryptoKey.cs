using System;

namespace Kinda.Licensing
{
    public sealed class CryptoKey : IEquatable<CryptoKey>
    {
        public CryptoKey(string content)
        {
            if (string.IsNullOrEmpty(content))
                throw new ArgumentNullException(nameof(content));

            Contents = content;
        }

        public string Contents { get; }

        public bool Equals(CryptoKey other)
        {
            if (ReferenceEquals(null, other))
            {
                return false;
            }
            if (ReferenceEquals(this, other))
            {
                return true;
            }

            return string.Equals(this.Contents, other.Contents);
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as CryptoKey);
        }

        public override int GetHashCode()
        {
            return this.Contents != null ? this.Contents.GetHashCode() : 0;
        }

        public static bool operator ==(CryptoKey left, CryptoKey right)
        {
            return Equals(left, right);
        }

        public static bool operator !=(CryptoKey left, CryptoKey right)
        {
            return !Equals(left, right);
        }
    }
}
