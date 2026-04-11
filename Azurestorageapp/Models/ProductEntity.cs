using Azure;
using Azure.Data.Tables;
using System.ComponentModel.DataAnnotations;

namespace Azurestorageapp.Models
{
    /// <summary>
    /// Product entity stored in Azure Table Storage.
    /// PartitionKey = "Products", RowKey = ProductId (Guid)
    /// </summary>
    public class ProductEntity : ITableEntity
    {
        public string PartitionKey { get; set; } = "Products";
        public string RowKey { get; set; } = Guid.NewGuid().ToString();
        public DateTimeOffset? Timestamp { get; set; }
        public ETag ETag { get; set; }

        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public double Price { get; set; }
        public int Quantity { get; set; }

        /// <summary>URL of the product image stored in Azure Blob Storage.</summary>
        public string? ImageUrl { get; set; }
    }

    public class ProductViewModel
    {
        public string? RowKey { get; set; }

        [Required]
        public string Name { get; set; } = string.Empty;

        [Required]
        public string Description { get; set; } = string.Empty;

        public string Category { get; set; } = "General";

        [Range(0, double.MaxValue)]
        public double Price { get; set; }

        [Range(0, int.MaxValue)]
        public int Quantity { get; set; }

        public IFormFile? ImageFile { get; set; }
        public string? ExistingImageUrl { get; set; }
    }
}
