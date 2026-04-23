namespace Licensing.Portal.Models;

public class LicenseRequest
{
    public required string SerialNumber { get; set; }
    public required LicenseType LicenseType { get; set; }
    public required DateTime IssuedAt { get; set; }
    public DateTime? ExpiresAt { get; set; }
}
