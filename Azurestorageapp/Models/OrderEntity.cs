using Azure;
using Azure.Data.Tables;

namespace Azurestorageapp.Models
{
    /// <summary>
    /// Order entity stored in Azure Table Storage.
    /// PartitionKey = CustomerId, RowKey = OrderId (Guid)
    /// This allows efficient querying of all orders for a specific customer.
    /// </summary>
    public class OrderEntity : ITableEntity
    {
        public string PartitionKey { get; set; } = string.Empty; // CustomerId
        public string RowKey { get; set; } = Guid.NewGuid().ToString();
        public DateTimeOffset? Timestamp { get; set; }
        public ETag ETag { get; set; }

        public string OrderId { get; set; } = string.Empty;
        public string CustomerName { get; set; } = string.Empty;
        public string ProductRowKey { get; set; } = string.Empty;
        public string ProductName { get; set; } = string.Empty;
        public string ProductCategory { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public double UnitPrice { get; set; }
        public double TotalPrice { get; set; }
        public string Status { get; set; } = "Processing";
        public DateTime OrderDate { get; set; } = DateTime.UtcNow;
    }
}
