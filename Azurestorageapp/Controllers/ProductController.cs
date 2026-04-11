using Azurestorageapp.Models;
using Azurestorageapp.Services;
using Microsoft.AspNetCore.Mvc;

namespace AzureStorageApp.Controllers
{
    public class ProductController : Controller
    {
        private readonly BlobService _blobService;
        private readonly TableService _tableService;
        private readonly OrderTableService _orderService;
        private readonly QueueService _queueService;
        private readonly FileService _fileService;

        public ProductController(
            BlobService blobService,
            TableService tableService,
            OrderTableService orderService,
            QueueService queueService,
            FileService fileService)
        {
            _blobService = blobService;
            _tableService = tableService;
            _orderService = orderService;
            _queueService = queueService;
            _fileService = fileService;
        }

        // ── INDEX ─────────────────────────────────────────────────────────
        public async Task<IActionResult> Index()
        {
            var products = await _tableService.GetAllProductsAsync();

            // Regenerate fresh SAS URLs
            foreach (var p in products.Where(p => !string.IsNullOrEmpty(p.ImageUrl)))
                p.ImageUrl = _blobService.GenerateSasUrl(
                    BlobService.GetFileNameFromUrl(p.ImageUrl!), TimeSpan.FromHours(2));

            // Build a set of product RowKeys that have at least one order
            var allOrders = await _orderService.GetAllOrdersAsync();
            var orderedProductKeys = allOrders
                .Select(o => o.ProductRowKey)
                .ToHashSet();

            ViewBag.OrderedProductKeys = orderedProductKeys;
            return View(products);
        }

        // ── DETAILS ───────────────────────────────────────────────────────
        public async Task<IActionResult> Details(string partitionKey, string rowKey)
        {
            var product = await _tableService.GetProductAsync(partitionKey, rowKey);
            if (product == null) return NotFound();
            if (!string.IsNullOrEmpty(product.ImageUrl))
                product.ImageUrl = _blobService.GenerateSasUrl(
                    BlobService.GetFileNameFromUrl(product.ImageUrl), TimeSpan.FromHours(2));
            return View(product);
        }

        // ── CREATE GET ────────────────────────────────────────────────────
        public IActionResult Create() => View(new ProductViewModel());

        // ── CREATE POST ───────────────────────────────────────────────────
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(ProductViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            var product = new ProductEntity
            {
                Name = model.Name,
                Description = model.Description,
                Category = model.Category,
                Price = model.Price,
                Quantity = model.Quantity
            };

            if (model.ImageFile != null && model.ImageFile.Length > 0)
            {
                var fileName = $"{Guid.NewGuid()}{Path.GetExtension(model.ImageFile.FileName)}";
                using var stream = model.ImageFile.OpenReadStream();
                await _blobService.UploadBlobAsync(stream, fileName, model.ImageFile.ContentType);
                product.ImageUrl = fileName;
                await _queueService.SendImageUploadMessageAsync(model.ImageFile.FileName);
            }

            await _tableService.AddProductAsync(product);
            await _queueService.SendInventoryUpdateMessageAsync(product.Name, product.Quantity);
            await _fileService.WriteLogAsync(
                $"product_create_{DateTime.UtcNow:yyyyMMddHHmmss}.txt",
                $"[{DateTime.UtcNow:u}] PRODUCT CREATED | ID: {product.RowKey} | Name: {product.Name} | Price: R{product.Price} | Qty: {product.Quantity}");

            TempData["Success"] = $"Product '{product.Name}' created successfully!";
            return RedirectToAction(nameof(Index));
        }

        // ── EDIT GET ──────────────────────────────────────────────────────
        public async Task<IActionResult> Edit(string partitionKey, string rowKey)
        {
            var product = await _tableService.GetProductAsync(partitionKey, rowKey);
            if (product == null) return NotFound();

            string? previewUrl = null;
            if (!string.IsNullOrEmpty(product.ImageUrl))
                previewUrl = _blobService.GenerateSasUrl(product.ImageUrl, TimeSpan.FromMinutes(30));

            return View(new ProductViewModel
            {
                RowKey = product.RowKey,
                Name = product.Name,
                Description = product.Description,
                Category = product.Category,
                Price = product.Price,
                Quantity = product.Quantity,
                ExistingImageUrl = previewUrl
            });
        }

        // ── EDIT POST ─────────────────────────────────────────────────────
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(string partitionKey, string rowKey, ProductViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            var product = await _tableService.GetProductAsync(partitionKey, rowKey);
            if (product == null) return NotFound();

            product.Name = model.Name;
            product.Description = model.Description;
            product.Category = model.Category;
            product.Price = model.Price;
            product.Quantity = model.Quantity;

            if (model.ImageFile != null && model.ImageFile.Length > 0)
            {
                if (!string.IsNullOrEmpty(product.ImageUrl))
                    await _blobService.DeleteBlobAsync(product.ImageUrl);

                var newFileName = $"{Guid.NewGuid()}{Path.GetExtension(model.ImageFile.FileName)}";
                using var stream = model.ImageFile.OpenReadStream();
                await _blobService.UploadBlobAsync(stream, newFileName, model.ImageFile.ContentType);
                product.ImageUrl = newFileName;
                await _queueService.SendImageUploadMessageAsync(model.ImageFile.FileName);
            }

            await _tableService.UpdateProductAsync(product);
            await _queueService.SendInventoryUpdateMessageAsync(product.Name, product.Quantity);
            await _fileService.WriteLogAsync(
                $"product_update_{DateTime.UtcNow:yyyyMMddHHmmss}.txt",
                $"[{DateTime.UtcNow:u}] PRODUCT UPDATED | ID: {product.RowKey} | Name: {product.Name} | Qty: {product.Quantity}");

            TempData["Success"] = $"Product '{product.Name}' updated!";
            return RedirectToAction(nameof(Index));
        }

        // ── DELETE GET ────────────────────────────────────────────────────
        public async Task<IActionResult> Delete(string partitionKey, string rowKey)
        {
            var product = await _tableService.GetProductAsync(partitionKey, rowKey);
            if (product == null) return NotFound();

            if (!string.IsNullOrEmpty(product.ImageUrl))
                product.ImageUrl = _blobService.GenerateSasUrl(
                    product.ImageUrl, TimeSpan.FromMinutes(10));

            // Check if this product has any orders
            var allOrders = await _orderService.GetAllOrdersAsync();
            var orderCount = allOrders.Count(o => o.ProductRowKey == rowKey);

            ViewBag.OrderCount = orderCount;
            return View(product);
        }

        // ── DELETE POST ───────────────────────────────────────────────────
        [HttpPost, ActionName("Delete"), ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(string partitionKey, string rowKey)
        {
            var product = await _tableService.GetProductAsync(partitionKey, rowKey);
            if (product == null) return NotFound();

            // Hard block — re-check on the server so it can't be bypassed via direct POST
            var allOrders = await _orderService.GetAllOrdersAsync();
            var orderCount = allOrders.Count(o => o.ProductRowKey == rowKey);
            if (orderCount > 0)
            {
                TempData["Error"] = $"Cannot delete '{product.Name}' — it has {orderCount} order(s) on record.";
                return RedirectToAction(nameof(Index));
            }

            if (!string.IsNullOrEmpty(product.ImageUrl))
                await _blobService.DeleteBlobAsync(product.ImageUrl);

            await _tableService.DeleteProductAsync(partitionKey, rowKey);
            await _queueService.SendMessageAsync(
                $"[INVENTORY] Product REMOVED: {product.Name} | ID: {product.RowKey}");
            await _fileService.WriteLogAsync(
                $"product_delete_{DateTime.UtcNow:yyyyMMddHHmmss}.txt",
                $"[{DateTime.UtcNow:u}] PRODUCT DELETED | ID: {product.RowKey} | Name: {product.Name}");

            TempData["Success"] = $"Product '{product.Name}' deleted.";
            return RedirectToAction(nameof(Index));
        }

        // ── QUEUE ─────────────────────────────────────────────────────────
        public async Task<IActionResult> QueueMessages()
            => View(await _queueService.PeekMessagesAsync());

        [HttpPost]
        public async Task<IActionResult> DequeueMessage()
        {
            TempData["DequeuedMessage"] = await _queueService.DequeueMessageAsync() ?? "Queue is empty.";
            return RedirectToAction(nameof(QueueMessages));
        }

        // ── FILE SHARE ────────────────────────────────────────────────────
        public async Task<IActionResult> Files()
            => View(await _fileService.ListFilesAsync());

        [HttpPost]
        public async Task<IActionResult> UploadFile(IFormFile file)
        {
            if (file != null && file.Length > 0)
            {
                using var stream = file.OpenReadStream();
                await _fileService.UploadFileAsync(stream, file.FileName);
                TempData["Success"] = $"File '{file.FileName}' uploaded to Azure File Share.";
            }
            return RedirectToAction(nameof(Files));
        }

        public async Task<IActionResult> DownloadFile(string fileName)
            => File(await _fileService.DownloadFileAsync(fileName), "application/octet-stream", fileName);

        [HttpPost]
        public async Task<IActionResult> DeleteFile(string fileName)
        {
            await _fileService.DeleteFileAsync(fileName);
            TempData["Success"] = $"File '{fileName}' deleted.";
            return RedirectToAction(nameof(Files));
        }
    }
}
