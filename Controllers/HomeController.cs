using System.Diagnostics;
using ABC_Retailers.Models;
using ABC_Retailers.Models.ViewModel;
using ABC_Retailers.Services;
using Microsoft.AspNetCore.Mvc;

namespace ABC_Retailers.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly IAzureStorageService _azureStorageService;

        public HomeController(ILogger<HomeController> logger, IAzureStorageService azureStorageService)
        {
            _logger = logger;
            _azureStorageService = azureStorageService;
        }

        public async Task<IActionResult> Index()
        {
            try
            {
                // Load data from Azure Tables
                var products = await _azureStorageService.GetAllEntitiesAsync<Product>();
                var customers = await _azureStorageService.GetAllEntitiesAsync<Customer>();
                var orders = await _azureStorageService.GetAllEntitiesAsync<Order>();

                // Select top 5 products as featured (you can add your own criteria)
                var featuredProducts = products.Take(5).ToList();

                var viewModel = new HomeViewModel
                {
                    FeaturedProducts = featuredProducts,
                    CustomerCount = customers.Count,
                    ProductCount = products.Count,
                    OrderCount = orders.Count
                };

                return View(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading dashboard data");
                TempData["ErrorMessage"] = "Failed to load dashboard data.";
                return View(new HomeViewModel());
            }
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
