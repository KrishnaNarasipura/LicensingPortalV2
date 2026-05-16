using Azure;
using Azure.Data.Tables;
using Licensing.Portal.Models;
using Microsoft.Extensions.Configuration;

namespace Licensing.Portal.Services
{
    public class AzureTableStorageService
    {
        private readonly TableClient _tableClient;
        private readonly TableClient _appSettingsTableClient;
        private const string TableName = "Dealers";
        private const string AppSettingsTableName = "AppSettings";

        public AzureTableStorageService(IConfiguration configuration)
        {
            var connectionString = configuration.GetConnectionString("AzureTableStorage");
            
            if (string.IsNullOrEmpty(connectionString))
            {
                throw new InvalidOperationException("Azure Table Storage connection string is not configured.");
            }

            var serviceClient = new TableServiceClient(connectionString);
            _tableClient = serviceClient.GetTableClient(TableName);
            _appSettingsTableClient = serviceClient.GetTableClient(AppSettingsTableName);
            
            // Create tables if they don't exist
            _tableClient.CreateIfNotExists();
            _appSettingsTableClient.CreateIfNotExists();
        }

        public async Task<DealerTableEntity?> GetDealerAsync(string dealerCode)
        {
            try
            {
                var response = await _tableClient.GetEntityAsync<DealerTableEntity>("Dealers", dealerCode);
                return response.Value;
            }
            catch (RequestFailedException ex) when (ex.Status == 404)
            {
                return null;
            }
        }

        public async Task<List<DealerTableEntity>> GetAllDealersAsync()
        {
            var dealers = new List<DealerTableEntity>();
            
            await foreach (var dealer in _tableClient.QueryAsync<DealerTableEntity>(filter: "PartitionKey eq 'Dealers'"))
            {
                dealers.Add(dealer);
            }
            
            return dealers;
        }

        public async Task<DealerTableEntity> AddDealerAsync(Dealer dealer)
        {
            var entity = new DealerTableEntity(dealer);
            
            await _tableClient.AddEntityAsync(entity);
            return entity;
        }

        public async Task<DealerTableEntity> UpdateDealerAsync(Dealer dealer)
        {
            var entity = new DealerTableEntity(dealer);
            await _tableClient.UpdateEntityAsync(entity, ETag.All, TableUpdateMode.Replace);
            return entity;
        }

        public async Task<DealerTableEntity> UpsertDealerAsync(Dealer dealer)
        {
            var entity = new DealerTableEntity(dealer);
            await _tableClient.UpsertEntityAsync(entity, TableUpdateMode.Replace);
            return entity;
        }

        public async Task DeleteDealerAsync(string dealerCode)
        {
            await _tableClient.DeleteEntityAsync("Dealers", dealerCode);
        }

        public async Task<bool> DealerExistsAsync(string dealerCode)
        {
            try
            {
                await _tableClient.GetEntityAsync<DealerTableEntity>("Dealers", dealerCode);
                return true;
            }
            catch (RequestFailedException ex) when (ex.Status == 404)
            {
                return false;
            }
        }

        
        public async Task<DealerTableEntity?> GetDealerByInternalDealerIdAsync(string internalDealerId)
        {
            var filter = $"PartitionKey eq 'Dealers' and InternalDealerId eq '{internalDealerId}'";
            
            await foreach (var dealer in _tableClient.QueryAsync<DealerTableEntity>(filter: filter))
            {
                return dealer;
            }
            
            return null;
        }

        /// <summary>
        /// Gets a setting value from the AppSettings table
        /// </summary>
        public async Task<string?> GetAppSettingAsync(string settingName)
        {
            try
            {
                var response = await _appSettingsTableClient.GetEntityAsync<AppSettingEntity>("AppSettings", "Secrets");
                
                // Use reflection to get the property value dynamically
                var property = typeof(AppSettingEntity).GetProperty(settingName);
                if (property != null)
                {
                    return property.GetValue(response.Value)?.ToString();
                }
                
                return null;
            }
            catch (RequestFailedException ex) when (ex.Status == 404)
            {
                return null;
            }
        }
    }

}
