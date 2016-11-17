namespace Kinda.Licensing
{
    public interface ILicenseValidationRule
    {
        void Validate(LicenseCriteria licenseCriteria);
    }
}
