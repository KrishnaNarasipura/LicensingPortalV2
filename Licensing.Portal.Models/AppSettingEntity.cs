using Azure.Data.Tables;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Licensing.Portal.Models
{
    public class AppSettingEntity : ITableEntity
    {
        public string PartitionKey { get; set; } = "AppSettings";
        public string RowKey { get; set; } = "Secrets";
        public DateTimeOffset? Timestamp { get; set; }
        public Azure.ETag ETag { get; set; }

        public string? AdminUserName { get; set; }
        public string? AdminPassword { get; set; }
        public string? LicenseKeySecret { get; set; }
        public string? ECDSAPrivateKey { get; set; }
        public string? ECDSAPublicKey { get; set; }
    }
}
