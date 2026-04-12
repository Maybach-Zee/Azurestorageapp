using Azurestorageapp.Services;
using Microsoft.AspNetCore.Mvc;

namespace Azurestorageapp.Controllers
{
    /// <summary>
    /// Dedicated controller for Azure Blob Storage — shows a visual image gallery
    /// of all blobs in the product-images container.
    /// </summary>
    public class BlobController : Controller
    {
        private readonly BlobService  _blobService;
        private readonly TableService _tableService;

        public BlobController(BlobService blobService, TableService tableService)
        {
            _blobService  = blobService;
            _tableService = tableService;
        }

        /// <summary>Gallery view — lists every blob with a live SAS image preview.</summary>
        public async Task<IActionResult> Index()
        {
            // Get all raw blob file names from the container
            var blobNames = await _blobService.ListBlobsAsync();

            // Build (fileName, sasUrl) pairs for display
            var items = blobNames.Select(name => new BlobGalleryItem
            {
                FileName = name,
                SasUrl   = _blobService.GenerateSasUrl(name, TimeSpan.FromHours(1))
            }).ToList();

            // Also pull product names so we can show which product each blob belongs to
            var products = await _tableService.GetAllProductsAsync();
            foreach (var item in items)
            {
                var match = products.FirstOrDefault(p =>
                    !string.IsNullOrEmpty(p.ImageUrl) &&
                    Azurestorageapp.Services.BlobService.GetFileNameFromUrl(p.ImageUrl) == item.FileName);
                item.ProductName = match?.Name;
            }

            return View(items);
        }
    }

    /// <summary>Simple DTO for the gallery view — not stored in Azure.</summary>
    public class BlobGalleryItem
    {
        public string  FileName    { get; set; } = string.Empty;
        public string  SasUrl      { get; set; } = string.Empty;
        public string? ProductName { get; set; }
    }
}
