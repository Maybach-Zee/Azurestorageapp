using Azure;
using Azure.Data.Tables;
using System.ComponentModel.DataAnnotations;

namespace Azurestorageapp.Models
{
    /// <summary>
    /// Customer profile entity stored in Azure Table Storage.
    /// PartitionKey = "Customers", RowKey = CustomerId (Guid)
    /// </summary>
    public class CustomerEntity : ITableEntity
    {
        public string PartitionKey { get; set; } = "Customers";
        public string RowKey { get; set; } = Guid.NewGuid().ToString();
        public DateTimeOffset? Timestamp { get; set; }
        public ETag ETag { get; set; }

        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public string City { get; set; } = string.Empty;

        public string FullName => $"{FirstName} {LastName}";
    }

    public class CustomerViewModel
    {
        public string? RowKey { get; set; }

        [Required]
        public string FirstName { get; set; } = string.Empty;

        [Required]
        public string LastName { get; set; } = string.Empty;

        [Required, EmailAddress]
        public string Email { get; set; } = string.Empty;

        public string Phone { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public string City { get; set; } = string.Empty;
    }
}
