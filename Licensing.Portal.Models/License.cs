namespace Licensing.Portal.Models
{
    public class License
    {
        public string DealerCode { get; set; } = string.Empty;
        public string InternalDealerId { get; set; } = string.Empty;
        public string SerialNumber { get; set; } = string.Empty;
        public string LicenseType { get; set; } = string.Empty; // "Metered" or "Permanent"
        public DateTime IssueDate { get; set; } = DateTime.UtcNow;
        public DateTime? ExpiryDate { get; set; }
        public int? ExpiryDays { get; set; } // Only used for Metered licenses
        public string LicenseKey { get; set; } = string.Empty;
        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
    }
}
