namespace Kinda.Licensing.Demo.ClientApp
{
    public class LicensedCoresExceededException : LicenseViolationException
    {
        public LicensedCoresExceededException(int actualCoreCount)
        {
            ActualCoreCount = actualCoreCount;
        }

        public LicensedCoresExceededException(string message, int actualCoreCount) : base(message)
        {
            ActualCoreCount = actualCoreCount;
        }

        public int ActualCoreCount { get; }
    }
}
