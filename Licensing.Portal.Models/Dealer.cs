namespace Licensing.Portal.Models
{
    public class Dealer
    {
        // DealerCode is now the primary key (used as RowKey in Azure Table Storage)
        public string DealerCode { get; set; } = string.Empty;
        public string DealerName { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public string City { get; set; } = string.Empty;
        public string State { get; set; } = string.Empty;
        public string Pincode { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public string InternalDealerId { get; set; } = string.Empty;
        public string TemporaryPassword { get; set; } = string.Empty;
        public string? Password { get; set; }
        public bool PasswordChangeRequired { get; set; } = true;
        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
        public int LicenseSequence { get; set; } = 0;
    }
}
