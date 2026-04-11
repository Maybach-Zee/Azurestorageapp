using Azure.Storage.Files.Shares;
using Azure.Storage.Files.Shares.Models;

namespace Azurestorageapp.Services
{
    /// <summary>
    /// Handles Azure File Share Storage.
    /// Used to store log files and product documents.
    /// Share: "product-files" / Directory: "logs"
    /// </summary>
    public class FileService
    {
        private readonly ShareClient _shareClient;
        private const string DirectoryName = "logs";

        public FileService(IConfiguration configuration)
        {
            var connectionString = configuration["AzureStorage:ConnectionString"]!;
            var fileShareName = configuration["AzureStorage:FileShareName"]!;

            _shareClient = new ShareClient(connectionString, fileShareName);
            _shareClient.CreateIfNotExists();

            _shareClient.GetDirectoryClient(DirectoryName).CreateIfNotExists();
        }

        private ShareDirectoryClient Dir => _shareClient.GetDirectoryClient(DirectoryName);

        public async Task UploadFileAsync(Stream fileStream, string fileName)
        {
            var fileClient = Dir.GetFileClient(fileName);
            await fileClient.CreateAsync(fileStream.Length);
            await fileClient.UploadAsync(fileStream);
        }

        /// <summary>Creates and uploads a plain-text log entry as a .txt file.</summary>
        public async Task WriteLogAsync(string logFileName, string logContent)
        {
            var bytes = System.Text.Encoding.UTF8.GetBytes(logContent);
            using var ms = new MemoryStream(bytes);
            await UploadFileAsync(ms, logFileName);
        }

        public async Task<List<string>> ListFilesAsync()
        {
            var files = new List<string>();
            await foreach (ShareFileItem item in Dir.GetFilesAndDirectoriesAsync())
                if (!item.IsDirectory) files.Add(item.Name);
            return files;
        }

        public async Task<Stream> DownloadFileAsync(string fileName)
        {
            var result = await Dir.GetFileClient(fileName).DownloadAsync();
            return result.Value.Content;
        }

        public async Task DeleteFileAsync(string fileName)
            => await Dir.GetFileClient(fileName).DeleteIfExistsAsync();
    }
}
