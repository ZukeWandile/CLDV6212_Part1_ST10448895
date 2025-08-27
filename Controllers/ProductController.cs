using System.Text.Json;
using ABC_Retailers.Models;
using ABC_Retailers.Services;
using Microsoft.AspNetCore.Mvc;

namespace ABC_Retailers.Controllers
{
    public class ProductController : Controller
    {
        private readonly IAzureStorageService _azureStorageService;

        public ProductController(IAzureStorageService azureStorageService)
        {
            _azureStorageService = azureStorageService;
        }

        public async Task<IActionResult> Index()
        {
            var products = await _azureStorageService.GetAllEntitiesAsync<Product>();
            return View(products);
        }

        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Create(Product product, IFormFile imageFile)
        {
            product.PartitionKey = "Products";
            product.RowKey = Guid.NewGuid().ToString();

            if (imageFile != null)
            {
                product.ImageUrl = await _azureStorageService.UploadImageAsync(imageFile, "product-images");
            }

            await _azureStorageService.AddEntityAsync(product);
            return RedirectToAction("Index");
        }

        public async Task<IActionResult> Edit(string id)
        {
            var product = await _azureStorageService.GetEntityAsync<Product>("Products", id);
            if (product == null) return NotFound();
            return View(product);
        }

        [HttpPost]
        public async Task<IActionResult> Edit(Product product, IFormFile imageFile)
        {
            var existing = await _azureStorageService.GetEntityAsync<Product>("Products", product.RowKey);
            if (existing == null) return NotFound();

            existing.ProductName = product.ProductName;
            existing.Description = product.Description;
            existing.Price = product.Price;
            existing.StockAvailabile = product.StockAvailabile;

            if (imageFile != null)
            {
                existing.ImageUrl = await _azureStorageService.UploadImageAsync(imageFile, "product-images");
            }

            await _azureStorageService.UpdateEntityAsync(existing);

            // --- Add stock update queue logic here ---
            if (existing.StockAvailabile < 10) // threshold
            {
                var message = JsonSerializer.Serialize(new
                {
                    ProductId = existing.RowKey,
                    ProductName = existing.ProductName,
                    CurrentStock = existing.StockAvailabile
                });

                await _azureStorageService.SendMessageAsync("stock-updates", message);
            }

            return RedirectToAction("Index");
        }


        public async Task<IActionResult> GetProductPrice(string id)
        {
            var product = await _azureStorageService.GetEntityAsync<Product>("Products", id);
            if (product == null) return NotFound();
            return Content($"Price: ${product.Price}");
        }

        // GET: Delete
        public async Task<IActionResult> Delete(string id)
        {
            var product = await _azureStorageService.GetEntityAsync<Product>("Products", id);
            if (product == null) return NotFound();

            return View(product);
        }

        // POST: DeleteConfirmed
        [HttpPost, ActionName("DeleteConfirmed")]
        public async Task<IActionResult> DeleteConfirmed(string PartitionKey, string RowKey)
        {
            if (string.IsNullOrEmpty(PartitionKey) || string.IsNullOrEmpty(RowKey))
                return BadRequest();

            await _azureStorageService.DeleteEntityAsync<Product>(PartitionKey, RowKey);
            return RedirectToAction("Index");
        }
    }
}

