using Licensing.Portal.Models;
using Microsoft.Extensions.Configuration;
using System.Security.Cryptography;
using System.Text;

namespace Licensing.Portal.Services;

/// <summary>
/// Service for generating compact license keys (XXXX-XXXX-XXXX-XXXX-XXXX-XXXX format - 24 characters)
/// Fixed structure: 11 bytes data + 4 bytes HMAC-SHA256 signature = 15 bytes total → Base32 encodes to 24 chars
/// 
/// Data Layout (11 bytes, 77 bits):
/// - Bits 0-19: Serial Number (20 bits, range: 0 to 1,048,575)
/// - Bits 20-22: Serial Number Length (3 bits, range: 1-7)
/// - Bits 23-38: IssuedAt (16 bits, days since 2024-01-01)
/// - Bits 39-54: ExpiresAt (16 bits, days since 2024-01-01)
/// - Bits 55-71: Sequence Number (17 bits, range: 0 to 131,071)
/// - Bit 72: License Type (1 bit, 1 = PERMANENT, 0 = METERED)
/// - Bits 73-79: Padding (7 bits for future use)
/// 
/// HMAC Signature (4 bytes):
/// - First 4 bytes of HMAC-SHA256(dataHeader[0:10], hmacSecret) for integrity verification
/// 
/// Supports:
/// - Serial numbers: 0 to 1,048,575 (20-bit range)
/// - Serial number lengths: 1-7 digits (tracked in bits 20-22)
/// - Sequence numbers: 0 to 131,071 (17-bit range, auto-incremented per dealer)
/// - Date range: 2024-01-01 to 2203-10-16 (~179 years with day precision)
/// - Time precision: Day resolution (no hour/minute precision)
/// - Two license types: PERMANENT and METERED
/// </summary>
public class LicenseService
{
    private readonly string _hmacSecretKey;
    private readonly DateTime _epoch = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc);

    public LicenseService(IConfiguration configuration)
    {
        _hmacSecretKey = configuration["Licensing:LicenseKeySecret"] 
            ?? throw new InvalidOperationException("LicenseKeySecret not configured");
    }

    public string GenerateLicense(string serialStr, DateTime issueDate, DateTime? expiryDate, LicenseType licenseType, int sequence)
    {
        if (string.IsNullOrEmpty(serialStr) || serialStr.Length > 7)
            throw new ArgumentException("Serial must be 1-7 characters");

        uint serial = uint.Parse(serialStr);
        if (serial > 1048575) throw new ArgumentException("Serial too large for 20 bits");

        uint serialLen = (uint)serialStr.Length;
        if (serialLen > 7) throw new ArgumentException("Serial length too large for 3 bits (max 7)");

        // Validate sequence fits in 17 bits
        uint seq = (uint)sequence;
        if (seq > 131071) throw new ArgumentException("Sequence too large for 17 bits (max 131071)");

        // 2. Calculate issue date and expiry date in days since epoch
        uint issueDays = (uint)(issueDate - _epoch.Date).TotalDays;
        if (issueDays > 65535) throw new ArgumentException("Issue date too far in the future for 16 bits (max ~178 years)");

        DateTime finalExpiryDate = licenseType == LicenseType.PERMANENT ? _epoch.Date.AddDays((1U << 16) - 1) : expiryDate ?? DateTime.Now;
        uint expiryDays = (uint)(finalExpiryDate - _epoch.Date).TotalDays;
        if (expiryDays > 65535) throw new ArgumentException("Expiry date too far in the future for 16 bits (max ~178 years)");

        byte[] finalBytes = new byte[15];

        // 3. Pack data into 11-byte header (77 bits total)
        // Layout: [Serial: 20 bits] [SerialLen: 3 bits] [IssueDays: 16 bits] [ExpiryDays: 16 bits] [Sequence: 17 bits] [License: 1 bit] [Padding: 7 bits]
        byte[] dataHeader = new byte[11];

        // Bytes 0-2: Serial (20 bits) + SerialLen (3 bits) + IssueDays (1 bit)
        dataHeader[0] = (byte)(serial >> 12);
        dataHeader[1] = (byte)(serial >> 4);
        dataHeader[2] = (byte)(((serial & 0xF) << 4) | ((serialLen & 0x7) << 1) | ((issueDays >> 15) & 0x1));
        
        // Bytes 3-4: IssueDays (15 bits) + ExpiryDays (1 bit)
        dataHeader[3] = (byte)(issueDays >> 7);
        dataHeader[4] = (byte)(((issueDays & 0x7F) << 1) | ((expiryDays >> 15) & 0x1));
        
        // Bytes 5-6: ExpiryDays (15 bits) + Sequence (1 bit)
        dataHeader[5] = (byte)(expiryDays >> 7);
        dataHeader[6] = (byte)(((expiryDays & 0x7F) << 1) | ((seq >> 16) & 0x1));
        
        // Bytes 7-8: Sequence (16 bits)
        dataHeader[7] = (byte)((seq >> 8) & 0xFF);
        dataHeader[8] = (byte)(seq & 0xFF);
        
        // Byte 9: License Type (1 bit) + Padding (7 bits)
        dataHeader[9] = (byte)(licenseType == LicenseType.PERMANENT ? 1 : 0);
        
        // Byte 10: Padding
        dataHeader[10] = 0; // Extra byte for alignment

        // 4. Compute HMAC-SHA256 signature and combine with data header
        using (var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(_hmacSecretKey)))
        {
            byte[] fullHash = hmac.ComputeHash(dataHeader, 0, 10); // Hash only the first 10 bytes

            // Combine: 11 bytes data + 4 bytes HMAC (47 bits) = 15 bytes
            Buffer.BlockCopy(dataHeader, 0, finalBytes, 0, 11);
            Buffer.BlockCopy(fullHash, 0, finalBytes, 11, 4);
        }

        // 5. Encode as Base32 and return
        var rawLicenseKey = ToBase32(finalBytes);
        return FormatLicenseKey(rawLicenseKey);

    }

    public bool TryValidateLicense(string licenseKey, out LicenseKeyData? licenseKeyData)
    {
        string serial = "0"; uint issueDays = 0; uint expiryDays = 0; bool isPermanant = false; uint sequence = 0;
        licenseKeyData = new LicenseKeyData();
        try
        {
            string key = licenseKey.Replace("-", "").ToUpper();

            byte[] finalBytes = FromBase32(key);
            if (finalBytes.Length != 15) return false;

            // 1. Separate Data (11 bytes) and HMAC (4 bytes)
            byte[] dataHeader = new byte[11];
            byte[] providedHmac = new byte[4];
            Buffer.BlockCopy(finalBytes, 0, dataHeader, 0, 11);
            Buffer.BlockCopy(finalBytes, 11, providedHmac, 0, 4);

            // 2. Verify HMAC Signature
            using (var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(_hmacSecretKey)))
            {
                byte[] calculatedHash = hmac.ComputeHash(dataHeader, 0, 10);
                for (int i = 0; i < 4; i++)
                {
                    if (providedHmac[i] != calculatedHash[i]) return false;
                }
            }

            // 3. Unpack Data using the same bit-mapping
            // Extract Serial (20 bits) from bytes 0-2
            uint serialNum = (uint)dataHeader[0] << 12;
            serialNum |= (uint)dataHeader[1] << 4;
            serialNum |= (uint)(dataHeader[2] >> 4);

            // Extract SerialLen (3 bits) from byte 2, bits 3-1
            uint serialLen = (uint)((dataHeader[2] >> 1) & 0x7);

            // Extract IssueDays (16 bits) from bytes 2-4
            issueDays = (uint)((dataHeader[2] & 0x1) << 15);
            issueDays |= (uint)dataHeader[3] << 7;
            issueDays |= (uint)(dataHeader[4] >> 1);

            // Extract ExpiryDays (16 bits) from bytes 4-6
            expiryDays = (uint)((dataHeader[4] & 0x1) << 15);
            expiryDays |= (uint)dataHeader[5] << 7;
            expiryDays |= (uint)(dataHeader[6] >> 1);

            // Extract Sequence (17 bits) from bytes 6-8
            sequence = (uint)((dataHeader[6] & 0x1) << 16);
            sequence |= (uint)dataHeader[7] << 8;
            sequence |= (uint)dataHeader[8];

            // Extract License Type (1 bit) from byte 9, bit 0
            isPermanant = (dataHeader[9] & 0x1) == 1;

            // Format serial with leading zeros based on serialLen
            serial = serialNum.ToString().PadLeft((int)serialLen, '0');

            var issuedDate = _epoch.Date.AddDays(issueDays);
            var expiryDate = _epoch.Date.AddDays(expiryDays);

            licenseKeyData.SerialNumber = serial;
            licenseKeyData.IssuedAt = issuedDate;
            licenseKeyData.ExpiresAt = expiryDate;
            licenseKeyData.Sequence = Convert.ToInt32(sequence);
            licenseKeyData.LicenseType = isPermanant ? LicenseType.PERMANENT : LicenseType.METERED;

            return true;
        }
        catch
        {
            return false;
        }
    }

    private string FormatLicenseKey(string base32)
    {
        if (base32.Length != 24)
            return base32;

        return $"{base32.Substring(0, 4)}-{base32.Substring(4, 4)}-{base32.Substring(8, 4)}-{base32.Substring(12, 4)}-{base32.Substring(16, 4)}-{base32.Substring(20, 4)}";
    }

    #region Base32 Encoding (RFC 4648)

    private const string Base32Alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZ234567";

    private static string ToBase32(byte[] input)
    {
        const string alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZ234567";
        var output = new StringBuilder();
        int bitIndex = 0;
        int bitBuffer = 0;

        foreach (byte b in input)
        {
            bitBuffer = (bitBuffer << 8) | b;
            bitIndex += 8;

            while (bitIndex >= 5)
            {
                bitIndex -= 5;
                output.Append(alphabet[(bitBuffer >> bitIndex) & 0x1F]);
            }
        }

        if (bitIndex > 0)
        {
            output.Append(alphabet[(bitBuffer << (5 - bitIndex)) & 0x1F]);
        }

        return output.ToString();
    }

    private static byte[] FromBase32(string input)
    {
        const string alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZ234567";
        var output = new List<byte>();
        int bitIndex = 0;
        int bitBuffer = 0;

        foreach (char c in input)
        {
            int value = alphabet.IndexOf(char.ToUpper(c));
            if (value < 0) throw new ArgumentException("Invalid Base32 character");

            bitBuffer = (bitBuffer << 5) | value;
            bitIndex += 5;

            if (bitIndex >= 8)
            {
                bitIndex -= 8;
                output.Add((byte)((bitBuffer >> bitIndex) & 0xFF));
            }
        }

        return output.ToArray();
    }

    #endregion
}

public class LicenseKeyData
{
    public string SerialNumber { get; set; } = string.Empty;
    public LicenseType LicenseType { get; set; }
    public DateTime IssuedAt { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public int Sequence { get; set; }
}
