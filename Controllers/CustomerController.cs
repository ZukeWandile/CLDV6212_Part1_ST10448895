using Microsoft.AspNetCore.Mvc;
using ABC_Retailers.Models;
using ABC_Retailers.Services;   // for IAzureStorageService
using System;
using System.Threading.Tasks;
using ABC_Retailers.Models;
using ABC_Retailers.Services;
using System.Text.Json;

namespace ABC_Retailers.Controllers
{
    public class CustomerController : Controller
    {
        private readonly IAzureStorageService _azureStorageService;

        public CustomerController(IAzureStorageService azureStorageService)
        {
            _azureStorageService = azureStorageService;
        }

        // Index - List all customers or single customer if id is provided
        public async Task<IActionResult> Index(string? id)
        {
            if (!string.IsNullOrEmpty(id))
            {
                var customer = await _azureStorageService.GetEntityAsync<Customer>("Customers", id);
                if (customer == null)
                    return NotFound();

                return View("Details", customer);
            }

            var customers = await _azureStorageService.GetAllEntitiesAsync<Customer>();
            return View(customers);
        }

        // GET: Create customer
        [HttpGet]
        public IActionResult Create()
        {
            return View();
        }

        // POST: Create customer
        [HttpPost]
        public async Task<IActionResult> Create(Customer model)
        {
            model.PartitionKey = "Customers";
            model.RowKey = Guid.NewGuid().ToString();

            if (ModelState.IsValid)
            {
                await _azureStorageService.AddEntityAsync(model);

                // Queue a welcome email
                var message = JsonSerializer.Serialize(new
                {
                    CustomerId = model.RowKey,
                    Email = model.Email,
                    FirstName = model.FirstName
                });

                await _azureStorageService.SendMessageAsync("welcome-emails", message);
                

                return RedirectToAction("Index");
            }

            return View(model);
        }


        // GET: Edit
        [HttpGet]
        public async Task<IActionResult> Edit(string id)
        {
            var customer = await _azureStorageService.GetEntityAsync<Customer>("Customers", id);
            if (customer == null)
                return NotFound();

            return View(customer);
        }

        // POST: Edit
        [HttpPost]
        public async Task<IActionResult> Edit(Customer model)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    // Get original entity by PartitionKey & RowKey
                    var existing = await _azureStorageService.GetEntityAsync<Customer>("Customers", model.RowKey);

                    if (existing == null)
                        return NotFound();

                    // Update fields
                    existing.FirstName = model.FirstName;
                    existing.LastName = model.LastName;
                    existing.Email = model.Email;
                    existing.UserName = model.UserName;
                    existing.ShippingAddress = model.ShippingAddress;

                    // Update in table
                    await _azureStorageService.UpdateEntityAsync(existing);

                    return RedirectToAction("Index");
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError(string.Empty, $"Failed to update customer: {ex.Message}");
                }
            }

            return View(model);
        }


        // GET: Delete
        [HttpGet]
        public async Task<IActionResult> Delete(string id)
        {
            var customer = await _azureStorageService.GetEntityAsync<Customer>("Customers", id);
            if (customer == null)
                return NotFound();

            return View(customer);
        }

        // POST: Delete
        [HttpPost, ActionName("DeleteConfirmed")]
        public async Task<IActionResult> DeleteConfirmed(string PartitionKey, string RowKey)
        {
            if (string.IsNullOrEmpty(PartitionKey) || string.IsNullOrEmpty(RowKey))
                return BadRequest();

            await _azureStorageService.DeleteEntityAsync<Customer>(PartitionKey, RowKey);
            return RedirectToAction("Index");
        }


        // Details
        public async Task<IActionResult> Details(string id)
        {
            var customer = await _azureStorageService.GetEntityAsync<Customer>("Customers", id);
            if (customer == null)
                return NotFound();

            return View(customer);
        }
    }
}
