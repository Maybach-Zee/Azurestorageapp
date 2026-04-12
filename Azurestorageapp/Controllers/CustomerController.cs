using Azurestorageapp.Models;
using Azurestorageapp.Services;
using Microsoft.AspNetCore.Mvc;

namespace AzureStorageApp.Controllers
{
    public class CustomerController : Controller
    {
        private readonly CustomerTableService _customerService;
        private readonly TableService _productService;
        private readonly OrderTableService _orderService;
        private readonly QueueService _queueService;
        private readonly FileService _fileService;
        private readonly BlobService _blobService;

        public CustomerController(
            CustomerTableService customerService,
            TableService productService,
            OrderTableService orderService,
            QueueService queueService,
            FileService fileService,
            BlobService blobService)
        {
            _customerService = customerService;
            _productService = productService;
            _orderService = orderService;
            _queueService = queueService;
            _fileService = fileService;
            _blobService = blobService;
        }

        // ── INDEX ─────────────────────────────────────────────────────────
        public async Task<IActionResult> Index()
            => View(await _customerService.GetAllCustomersAsync());

        // ── DETAILS ───────────────────────────────────────────────────────
        public async Task<IActionResult> Details(string partitionKey, string rowKey)
        {
            var customer = await _customerService.GetCustomerAsync(partitionKey, rowKey);
            if (customer == null) return NotFound();

            // Orders are stored with PartitionKey = customer.RowKey
            var orders = await _orderService.GetOrdersByCustomerAsync(customer.RowKey);
            var totalSpent = orders.Sum(o => o.TotalPrice);

            ViewBag.Orders = orders;
            ViewBag.TotalSpent = totalSpent;
            return View(customer);
        }

        // ── CREATE ────────────────────────────────────────────────────────
        public IActionResult Create() => View(new CustomerViewModel());

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CustomerViewModel model)
        {
            if (!ModelState.IsValid) return View("Create", model);

            var customer = new CustomerEntity
            {
                FirstName = model.FirstName,
                LastName = model.LastName,
                Email = model.Email,
                Phone = model.Phone,
                Address = model.Address,
                City = model.City
            };

            await _customerService.AddCustomerAsync(customer);

            await _queueService.SendMessageAsync(
                $"[CUSTOMER] New customer registered: {customer.FullName} | Email: {customer.Email} | ID: {customer.RowKey}");

            await _fileService.WriteLogAsync(
                $"customer_create_{DateTime.UtcNow:yyyyMMddHHmmss}.txt",
                $"[{DateTime.UtcNow:u}] CUSTOMER CREATED | ID: {customer.RowKey} | Name: {customer.FullName} | Email: {customer.Email}");

            TempData["Success"] = $"Customer '{customer.FullName}' added!";
            return RedirectToAction(nameof(Index));
        }

        // ── EDIT ──────────────────────────────────────────────────────────
        public async Task<IActionResult> Edit(string partitionKey, string rowKey)
        {
            var c = await _customerService.GetCustomerAsync(partitionKey, rowKey);
            if (c == null) return NotFound();
            return View(new CustomerViewModel
            {
                RowKey = c.RowKey,
                FirstName = c.FirstName,
                LastName = c.LastName,
                Email = c.Email,
                Phone = c.Phone,
                Address = c.Address,
                City = c.City
            });
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(string partitionKey, string rowKey, CustomerViewModel model)
        {
            if (!ModelState.IsValid) return View("Edit", model);

            var customer = await _customerService.GetCustomerAsync(partitionKey, rowKey);
            if (customer == null) return NotFound();

            customer.FirstName = model.FirstName; customer.LastName = model.LastName;
            customer.Email = model.Email; customer.Phone = model.Phone;
            customer.Address = model.Address; customer.City = model.City;

            await _customerService.UpdateCustomerAsync(customer);
            TempData["Success"] = $"Customer '{customer.FullName}' updated!";
            return RedirectToAction(nameof(Index));
        }

        // ── DELETE ────────────────────────────────────────────────────────
        public async Task<IActionResult> Delete(string partitionKey, string rowKey)
        {
            var customer = await _customerService.GetCustomerAsync(partitionKey, rowKey);
            if (customer == null) return NotFound();
            return View(customer);
        }

        [HttpPost, ActionName("Delete"), ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(string partitionKey, string rowKey)
        {
            var customer = await _customerService.GetCustomerAsync(partitionKey, rowKey);
            if (customer == null) return NotFound();
            await _customerService.DeleteCustomerAsync(partitionKey, rowKey);
            TempData["Success"] = $"Customer '{customer.FullName}' deleted.";
            return RedirectToAction(nameof(Index));
        }

        // ── PLACE ORDER — GET ─────────────────────────────────────────────
        public async Task<IActionResult> PlaceOrder(string partitionKey, string rowKey)
        {
            var customer = await _customerService.GetCustomerAsync(partitionKey, rowKey);
            if (customer == null) return NotFound();

            var products = (await _productService.GetAllProductsAsync())
                .Where(p => p.Quantity > 0)
                .OrderBy(p => p.Name)
                .ToList();

            // Attach fresh SAS image URLs for display
            foreach (var p in products.Where(p => !string.IsNullOrEmpty(p.ImageUrl)))
                p.ImageUrl = _blobService.GenerateSasUrl(
                    BlobService.GetFileNameFromUrl(p.ImageUrl!), TimeSpan.FromMinutes(30));

            ViewBag.Customer = customer;
            return View(products);
        }

        // ── PLACE ORDER — POST ────────────────────────────────────────────
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> PlaceOrder(
            string partitionKey, string rowKey,
            string productRowKey, int quantity)
        {
            var customer = await _customerService.GetCustomerAsync(partitionKey, rowKey);
            if (customer == null) return NotFound();

            // Validate product exists
            var product = await _productService.GetProductAsync("Products", productRowKey);
            if (product == null)
            {
                TempData["Error"] = "Selected product not found. Please try again.";
                return RedirectToAction(nameof(PlaceOrder),
                    new { partitionKey = customer.PartitionKey, rowKey = customer.RowKey });
            }

            // Validate quantity
            if (quantity <= 0 || quantity > product.Quantity)
            {
                TempData["Error"] = $"Invalid quantity. Only {product.Quantity} unit(s) of '{product.Name}' are available.";
                return RedirectToAction(nameof(PlaceOrder),
                    new { partitionKey = customer.PartitionKey, rowKey = customer.RowKey });
            }

            // 1. Deduct stock in Table Storage
            product.Quantity -= quantity;
            await _productService.UpdateProductAsync(product);

            // 2. Save order record — PartitionKey = customer.RowKey for fast lookup
            var orderId = Guid.NewGuid().ToString()[..8].ToUpper();
            var order = new OrderEntity
            {
                PartitionKey = customer.RowKey,
                OrderId = orderId,
                CustomerName = customer.FullName,
                ProductRowKey = product.RowKey,
                ProductName = product.Name,
                ProductCategory = product.Category,
                Quantity = quantity,
                UnitPrice = product.Price,
                TotalPrice = product.Price * quantity,
                Status = "Processing",
                OrderDate = DateTime.UtcNow
            };
            await _orderService.AddOrderAsync(order);

            // 3. Queue: order processing + inventory update
            await _queueService.SendOrderProcessingMessageAsync(
                orderId, customer.FullName, product.Name, quantity);
            await _queueService.SendInventoryUpdateMessageAsync(
                product.Name, product.Quantity);

            // 4. Write log to Azure File Share
            await _fileService.WriteLogAsync(
                $"order_{orderId}_{DateTime.UtcNow:yyyyMMddHHmmss}.txt",
                $"[{DateTime.UtcNow:u}] ORDER PLACED | OrderId: {orderId} | " +
                $"Customer: {customer.FullName} | Product: {product.Name} | " +
                $"Qty: {quantity} | Unit Price: R{product.Price:0.00} | " +
                $"Total: R{product.Price * quantity:0.00} | Remaining Stock: {product.Quantity}");

            // 5. Success message — show on Details page
            TempData["Success"] = product.Quantity == 0
                ? $"✅ Order {orderId} placed! ⚠️ '{product.Name}' is now OUT OF STOCK."
                : $"✅ Order {orderId} placed — {quantity}x {product.Name} (R{product.Price * quantity:0.00}). Stock remaining: {product.Quantity}.";

            // Redirect to Details using the correct keys
            return RedirectToAction(nameof(Details),
                new { partitionKey = customer.PartitionKey, rowKey = customer.RowKey });
        }
    }
}
