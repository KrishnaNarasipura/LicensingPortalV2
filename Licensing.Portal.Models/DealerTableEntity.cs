using Azure;
using Azure.Data.Tables;

namespace Licensing.Portal.Models
{
    public class DealerTableEntity : ITableEntity
    {
        public string PartitionKey { get; set; } = "Dealers";
        public string RowKey { get; set; } = string.Empty; // DealerCode
        public DateTimeOffset? Timestamp { get; set; }
        public ETag ETag { get; set; }

        // Dealer properties
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

        public DealerTableEntity()
        {
        }

        public DealerTableEntity(Dealer dealer)
        {
            PartitionKey = "Dealers";
            RowKey = dealer.DealerCode;
            DealerCode = dealer.DealerCode;
            DealerName = dealer.DealerName;
            Address = dealer.Address;
            City = dealer.City;
            State = dealer.State;
            Pincode = dealer.Pincode;
            PhoneNumber = dealer.PhoneNumber;
            InternalDealerId = dealer.InternalDealerId;
            TemporaryPassword = dealer.TemporaryPassword;
            Password = dealer.Password;
            PasswordChangeRequired = dealer.PasswordChangeRequired;
            CreatedDate = dealer.CreatedDate;
            LicenseSequence = dealer.LicenseSequence;
        }

        public Dealer ToDealer()
        {
            return new Dealer
            {
                DealerCode = RowKey, // Use RowKey as DealerCode
                DealerName = DealerName,
                Address = Address,
                City = City,
                State = State,
                Pincode = Pincode,
                PhoneNumber = PhoneNumber,
                InternalDealerId = InternalDealerId,
                TemporaryPassword = TemporaryPassword,
                Password = Password,
                PasswordChangeRequired = PasswordChangeRequired,
                CreatedDate = CreatedDate,
                LicenseSequence = LicenseSequence
            };
        }
    }
}
