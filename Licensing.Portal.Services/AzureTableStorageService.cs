using Azure;
using Azure.Data.Tables;
using Licensing.Portal.Models;
using Microsoft.Extensions.Configuration;

namespace Licensing.Portal.Services
{
    public class AzureTableStorageService
    {
        private readonly TableClient _tableClient;
        private const string TableName = "Dealers";

        public AzureTableStorageService(IConfiguration configuration)
        {
            var connectionString = configuration.GetConnectionString("AzureTableStorage");
            
            if (string.IsNullOrEmpty(connectionString))
            {
                throw new InvalidOperationException("Azure Table Storage connection string is not configured.");
            }

            var serviceClient = new TableServiceClient(connectionString);
            _tableClient = serviceClient.GetTableClient(TableName);
            
            // Create table if it doesn't exist
            _tableClient.CreateIfNotExists();
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

        public async Task<List<DealerTableEntity>> GetDealersByCityAsync(string city)
        {
            var dealers = new List<DealerTableEntity>();
            var filter = $"PartitionKey eq 'Dealers' and City eq '{city}'";
            
            await foreach (var dealer in _tableClient.QueryAsync<DealerTableEntity>(filter: filter))
            {
                dealers.Add(dealer);
            }
            
            return dealers;
        }

        public async Task<List<DealerTableEntity>> GetDealersByStateAsync(string state)
        {
            var dealers = new List<DealerTableEntity>();
            var filter = $"PartitionKey eq 'Dealers' and State eq '{state}'";
            
            await foreach (var dealer in _tableClient.QueryAsync<DealerTableEntity>(filter: filter))
            {
                dealers.Add(dealer);
            }
            
            return dealers;
        }

        public async Task<DealerTableEntity?> GetDealerByPhoneNumberAsync(string phoneNumber)
        {
            var filter = $"PartitionKey eq 'Dealers' and PhoneNumber eq '{phoneNumber}'";
            
            await foreach (var dealer in _tableClient.QueryAsync<DealerTableEntity>(filter: filter))
            {
                return dealer;
            }
            
            return null;
        }

        public async Task<int> GetNextLicenseSequenceAsync(string dealerCode)
        {
            var dealer = await GetDealerAsync(dealerCode);
            
            if (dealer == null)
            {
                throw new InvalidOperationException($"Dealer with code {dealerCode} not found.");
            }

            dealer.LicenseSequence++;
            await UpdateDealerAsync(dealer.ToDealer());
            
            return dealer.LicenseSequence;
        }
    }
}
