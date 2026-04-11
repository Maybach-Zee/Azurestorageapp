using Azure;
using Azure.Data.Tables;
using Azurestorageapp.Models;

namespace Azurestorageapp.Services
{
    /// <summary>
    /// Handles Azure Table Storage CRUD for ProductEntity.
    /// Table name: "Products"
    /// </summary>
    public class TableService
    {
        private readonly TableClient _tableClient;

        public TableService(IConfiguration configuration)
        {
            var connectionString = configuration["AzureStorage:ConnectionString"]!;
            var tableServiceClient = new TableServiceClient(connectionString);
            _tableClient = tableServiceClient.GetTableClient("Products");
            _tableClient.CreateIfNotExists();
        }

        public async Task AddProductAsync(ProductEntity product)
            => await _tableClient.AddEntityAsync(product);

        public async Task<List<ProductEntity>> GetAllProductsAsync()
        {
            var list = new List<ProductEntity>();
            await foreach (var e in _tableClient.QueryAsync<ProductEntity>())
                list.Add(e);
            return list;
        }

        public async Task<ProductEntity?> GetProductAsync(string partitionKey, string rowKey)
        {
            try { return (await _tableClient.GetEntityAsync<ProductEntity>(partitionKey, rowKey)).Value; }
            catch (RequestFailedException) { return null; }
        }

        public async Task UpdateProductAsync(ProductEntity product)
            => await _tableClient.UpsertEntityAsync(product, TableUpdateMode.Merge);

        public async Task DeleteProductAsync(string partitionKey, string rowKey)
            => await _tableClient.DeleteEntityAsync(partitionKey, rowKey);
    }
}
