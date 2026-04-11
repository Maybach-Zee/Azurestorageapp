using System.Diagnostics;
using Azurestorageapp.Services;
using Microsoft.AspNetCore.Mvc;

namespace Azurestorageapp.Controllers
{
    public class HomeController : Controller
    {
        private readonly QueueService _queueService;
        private readonly TableService _tableService;
        private readonly CustomerTableService _customerService;

        public HomeController(QueueService queueService, TableService tableService,
            CustomerTableService customerService)
        {
            _queueService = queueService;
            _tableService = tableService;
            _customerService = customerService;
        }

        public async Task<IActionResult> Index()
        {
            ViewBag.QueueMessageCount = await _queueService.GetMessageCountAsync();
            ViewBag.ProductCount = (await _tableService.GetAllProductsAsync()).Count;
            ViewBag.CustomerCount = (await _customerService.GetAllCustomersAsync()).Count;
            return View();
        }
    }
}
