using Azure.Data.Tables;
using Azurestorageapp.Models;

namespace Azurestorageapp.Services
{
    /// <summary>
    /// Handles Azure Table Storage operations for OrderEntity.
    /// Table: "Orders"
    /// PartitionKey = CustomerId — allows fast lookup of all orders per customer.
    /// </summary>
    public class OrderTableService
    {
        private readonly TableClient _tableClient;

        public OrderTableService(IConfiguration configuration)
        {
            var connectionString = configuration["AzureStorage:ConnectionString"]!;
            var tableServiceClient = new TableServiceClient(connectionString);
            _tableClient = tableServiceClient.GetTableClient("Orders");
            _tableClient.CreateIfNotExists();
        }

        public async Task AddOrderAsync(OrderEntity order)
            => await _tableClient.AddEntityAsync(order);

        /// <summary>Returns all orders placed by a specific customer.</summary>
        public async Task<List<OrderEntity>> GetOrdersByCustomerAsync(string customerId)
        {
            var orders = new List<OrderEntity>();
            await foreach (var e in _tableClient.QueryAsync<OrderEntity>(
                filter: $"PartitionKey eq '{customerId}'"))
                orders.Add(e);
            return orders.OrderByDescending(o => o.OrderDate).ToList();
        }

        /// <summary>Returns every order across all customers.</summary>
        public async Task<List<OrderEntity>> GetAllOrdersAsync()
        {
            var orders = new List<OrderEntity>();
            await foreach (var e in _tableClient.QueryAsync<OrderEntity>())
                orders.Add(e);
            return orders.OrderByDescending(o => o.OrderDate).ToList();
        }
    }
}
