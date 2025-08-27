using ABC_Retailers.Models;
using ABC_Retailers.Models.ViewModel;
using ABC_Retailers.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Text.Json;

namespace ABC_Retailers.Controllers
{
    public class OrderController : Controller
    {
        private readonly IAzureStorageService _azureStorageService;

        public OrderController(IAzureStorageService azureStorageService)
        {
            _azureStorageService = azureStorageService;
        }

        // Populate dropdowns
        private async Task PopulateDropdownsAsync(OrderCreateViewModel vm)
        {
            var customers = await _azureStorageService.GetAllEntitiesAsync<Customer>();
            var products = await _azureStorageService.GetAllEntitiesAsync<Product>();

            vm.Customers = customers.Select(c => new SelectListItem
            {
                Value = c.RowKey,
                Text = c.UserName
            }).ToList();

            vm.Products = products.Select(p => new SelectListItem
            {
                Value = p.RowKey,
                Text = p.ProductName
            }).ToList();
        }

        // GET: Orders
        public async Task<IActionResult> Index()
        {
            var orders = await _azureStorageService.GetAllEntitiesAsync<Order>();

            var customers = await _azureStorageService.GetAllEntitiesAsync<Customer>();
            var products = await _azureStorageService.GetAllEntitiesAsync<Product>();

            var customerDict = customers.ToDictionary(c => c.RowKey, c => c.UserName);
            var productDict = products.ToDictionary(p => p.RowKey, p => p.ProductName);

            foreach (var order in orders)
            {
                order.CustomerName = customerDict.ContainsKey(order.UserName)
                    ? customerDict[order.UserName]
                    : "Unknown Customer";

                order.ProductName = productDict.ContainsKey(order.ProductID)
                    ? productDict[order.ProductID]
                    : "Unknown Product";
            }

            return View(orders);
        }

        // GET: Create
        [HttpGet]
        public async Task<IActionResult> Create()
        {
            var vm = new OrderCreateViewModel();
            await PopulateDropdownsAsync(vm);

            var products = await _azureStorageService.GetAllEntitiesAsync<Product>();
            var productPrices = products.ToDictionary(p => p.RowKey, p => p.Price);
            ViewData["ProductPricesJson"] = JsonSerializer.Serialize(productPrices);

            return View(vm);
        }

        // POST: Create
        [HttpPost]
        public async Task<IActionResult> Create(OrderCreateViewModel vm)
        {
            if (!ModelState.IsValid)
            {
                await PopulateDropdownsAsync(vm);
                return View(vm);
            }

            try
            {
                var product = await _azureStorageService.GetEntityAsync<Product>("Products", vm.ProductId);

                if (product == null)
                {
                    ModelState.AddModelError("", "Product not found.");
                }
                else if (product.StockAvailabile < vm.Quantity)
                {
                    ModelState.AddModelError("", $"Not enough stock. Only {product.StockAvailabile} available.");
                }
                else
                {
                    product.StockAvailabile -= vm.Quantity;

                    var order = new Order
                    {
                        PartitionKey = "Orders",
                        RowKey = Guid.NewGuid().ToString(),
                        ProductID = vm.ProductId,
                        UserName = vm.CustomerId,
                        OrderDate = DateTime.UtcNow,
                        Quantity = vm.Quantity,
                        Status = "Pending", // default
                        TotalPrice = product.Price * vm.Quantity
                    };

                    await _azureStorageService.AddEntityAsync(order);
                    await _azureStorageService.UpdateEntityAsync(product);

                    TempData["SuccessMessage"] = "Order created successfully!";
                    return RedirectToAction(nameof(Index));
                }
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", $"Error creating order: {ex.Message}");
            }

            await PopulateDropdownsAsync(vm);
            return View(vm);
        }

        // GET: Edit
        [HttpGet]
        public async Task<IActionResult> Edit(string partitionKey, string rowKey)
        {
            var order = await _azureStorageService.GetEntityAsync<Order>(partitionKey, rowKey);
            if (order == null) return NotFound();

            var customer = await _azureStorageService.GetEntityAsync<Customer>("Customers", order.UserName);
            var product = await _azureStorageService.GetEntityAsync<Product>("Products", order.ProductID);

            order.CustomerName = customer?.UserName ?? "Unknown Customer";
            order.ProductName = product?.ProductName ?? "Unknown Product";

            ViewData["Statuses"] = new SelectList(
                new[] { "Pending", "Processing", "Completed", "Cancelled" },
                order.Status
            );

            return View(order);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Order model)
        {
            foreach (var state in ModelState)
            {
                foreach (var error in state.Value.Errors)
                {
                    Console.WriteLine($"Key: {state.Key}, Error: {error.ErrorMessage}");
                }
            }

 
            if (!ModelState.IsValid)
            {
                ViewData["Statuses"] = new SelectList(
                    new[] { "Pending", "Processing", "Completed", "Cancelled" },
                    model.Status
                );
                return View(model);
            }
      

            var existing = await _azureStorageService.GetEntityAsync<Order>(
                model.PartitionKey,
                model.RowKey
            );
            if (existing == null) return NotFound();

            existing.Status = model.Status;
            existing.ETag = Azure.ETag.All;

            await _azureStorageService.UpdateEntityAsync(existing);

            TempData["SuccessMessage"] = "Order status updated successfully!";
            return RedirectToAction(nameof(Index));
        }




        // GET: Details
        [HttpGet]
        public async Task<IActionResult> Details(string partitionKey, string rowKey)
        {
            var order = await _azureStorageService.GetEntityAsync<Order>(partitionKey, rowKey);
          
            if (order == null) return NotFound();

            var customer = await _azureStorageService.GetEntityAsync<Customer>("Customers", order.UserName);
            var product = await _azureStorageService.GetEntityAsync<Product>("Products", order.ProductID);

            order.CustomerName = customer?.UserName ?? "Unknown Customer";
            order.ProductName = product?.ProductName ?? "Unknown Product";

            return View(order);
        }

        // POST: Delete
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(string partitionKey, string rowKey)
        {
            try
            {
                await _azureStorageService.DeleteEntityAsync<Order>(partitionKey, rowKey);
                TempData["SuccessMessage"] = "Order deleted successfully.";
            }
            catch
            {
                TempData["ErrorMessage"] = "Error deleting order.";
            }

            return RedirectToAction(nameof(Index));
        }

        // AJAX: Get product price + stock
        [HttpGet]
        public async Task<IActionResult> GetProductPrice(string rowKey)
        {
            var product = await _azureStorageService.GetEntityAsync<Product>("Products", rowKey);
            if (product == null) return NotFound();
            return Json(new { price = product.Price, stock = product.StockAvailabile });
        }


        // helper to repopulate dropdown
        private async Task LoadProductsDropdown(string selectedProductId)
        {
            var products = await _azureStorageService.GetAllEntitiesAsync<Product>();
            ViewData["Products"] = products.Select(p => new SelectListItem
            {
                Value = p.RowKey,
                Text = p.ProductName,
                Selected = p.RowKey == selectedProductId
            }).ToList();
        }


    }
}
