using Azure.Storage;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Azure.Storage.Sas;

namespace Azurestorageapp.Services
{
    /// <summary>
    /// Handles all Azure Blob Storage operations.
    /// Uses a PRIVATE container + SAS tokens so images display in the browser
    /// even when "Allow Blob public access" is disabled on the storage account.
    /// </summary>
    public class BlobService
    {
        private readonly BlobContainerClient _containerClient;
        private readonly string _accountName;
        private readonly string _accountKey;

        public BlobService(IConfiguration configuration)
        {
            var connectionString = configuration["AzureStorage:ConnectionString"]!;
            var containerName = configuration["AzureStorage:BlobContainerName"]!;

            _accountName = ParseConnectionStringValue(connectionString, "AccountName");
            _accountKey = ParseConnectionStringValue(connectionString, "AccountKey");

            var blobServiceClient = new BlobServiceClient(connectionString);
            _containerClient = blobServiceClient.GetBlobContainerClient(containerName);

            // Create as PRIVATE — no public access needed
            _containerClient.CreateIfNotExists(PublicAccessType.None);
        }

        /// <summary>Uploads an image and stores it privately.</summary>
        public async Task<string> UploadBlobAsync(Stream imageStream, string fileName, string contentType)
        {
            var blobClient = _containerClient.GetBlobClient(fileName);
            await blobClient.UploadAsync(imageStream, new BlobUploadOptions
            {
                HttpHeaders = new BlobHttpHeaders { ContentType = contentType }
            });
            // Return only the file name — SAS URLs are generated fresh on each page load
            return fileName;
        }

        /// <summary>
        /// Generates a time-limited SAS URL for reading a blob.
        /// Always accepts a plain filename (e.g. "abc123.jpg").
        /// </summary>
        public string GenerateSasUrl(string fileName, TimeSpan validFor)
        {
            // Safety: strip any leftover SAS query string or full URL prefix
            fileName = GetFileNameFromUrl(fileName);

            var sasBuilder = new BlobSasBuilder
            {
                BlobContainerName = _containerClient.Name,
                BlobName = fileName,
                Resource = "b",
                ExpiresOn = DateTimeOffset.UtcNow.Add(validFor)
            };
            sasBuilder.SetPermissions(BlobSasPermissions.Read);

            var credential = new StorageSharedKeyCredential(_accountName, _accountKey);
            var sasToken = sasBuilder.ToSasQueryParameters(credential).ToString();

            return $"{_containerClient.GetBlobClient(fileName).Uri}?{sasToken}";
        }

        /// <summary>
        /// Extracts the blob file name from EITHER format:
        ///   - plain filename:  "abc123.jpg"
        ///   - full/SAS URL:    "https://account.blob.core.windows.net/container/abc123.jpg?sv=..."
        /// This ensures old records (full URL) and new records (filename only) both work.
        /// </summary>
        public static string GetFileNameFromUrl(string url)
        {
            if (string.IsNullOrWhiteSpace(url)) return url;

            // Already a plain filename — no scheme present
            if (!url.StartsWith("http://", StringComparison.OrdinalIgnoreCase) &&
                !url.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
                return Path.GetFileName(url);

            // Strip SAS query string, then extract the last path segment
            return Path.GetFileName(new Uri(url.Split('?')[0]).AbsolutePath);
        }

        public async Task DeleteBlobAsync(string fileName)
        {
            fileName = GetFileNameFromUrl(fileName); // normalise before deleting
            await _containerClient.GetBlobClient(fileName).DeleteIfExistsAsync();
        }

        public async Task<List<string>> ListBlobsAsync()
        {
            var blobs = new List<string>();
            await foreach (var item in _containerClient.GetBlobsAsync())
                blobs.Add(item.Name);
            return blobs;
        }

        // ?? helpers ???????????????????????????????????????????????????????
        private static string ParseConnectionStringValue(string connectionString, string key)
        {
            foreach (var part in connectionString.Split(';'))
            {
                var idx = part.IndexOf('=');
                if (idx > 0 && part[..idx].Trim().Equals(key, StringComparison.OrdinalIgnoreCase))
                    return part[(idx + 1)..].Trim();
            }
            throw new InvalidOperationException($"Key '{key}' not found in connection string.");
        }
    }
}
