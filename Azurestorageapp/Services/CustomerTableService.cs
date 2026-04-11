using Azure;
using Azure.Data.Tables;
using Azurestorageapp.Models;

namespace Azurestorageapp.Services
{
    /// <summary>
    /// Handles Azure Table Storage CRUD for CustomerEntity.
    /// Table name: "Customers"
    /// </summary>
    public class CustomerTableService
    {
        private readonly TableClient _tableClient;

        public CustomerTableService(IConfiguration configuration)
        {
            var connectionString = configuration["AzureStorage:ConnectionString"]!;
            var tableServiceClient = new TableServiceClient(connectionString);
            _tableClient = tableServiceClient.GetTableClient("Customers");
            _tableClient.CreateIfNotExists();
        }

        public async Task AddCustomerAsync(CustomerEntity customer)
            => await _tableClient.AddEntityAsync(customer);

        public async Task<List<CustomerEntity>> GetAllCustomersAsync()
        {
            var list = new List<CustomerEntity>();
            await foreach (var e in _tableClient.QueryAsync<CustomerEntity>())
                list.Add(e);
            return list;
        }

        public async Task<CustomerEntity?> GetCustomerAsync(string partitionKey, string rowKey)
        {
            try { return (await _tableClient.GetEntityAsync<CustomerEntity>(partitionKey, rowKey)).Value; }
            catch (RequestFailedException) { return null; }
        }

        public async Task UpdateCustomerAsync(CustomerEntity customer)
            => await _tableClient.UpsertEntityAsync(customer, TableUpdateMode.Merge);

        public async Task DeleteCustomerAsync(string partitionKey, string rowKey)
            => await _tableClient.DeleteEntityAsync(partitionKey, rowKey);
    }
}
