using ABC_Retailers.Models;
using ABC_Retailers.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Text.Json;

namespace ABC_Retailers.Controllers
{
    public class UploadController : Controller
    {
        private readonly IAzureStorageService _azureStorageService;
        private readonly ILogger<UploadController> _logger;

        public UploadController(IAzureStorageService azureStorageService, ILogger<UploadController> logger)
        {
            _azureStorageService = azureStorageService;
            _logger = logger;
        }

        // GET: Upload form
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            await LoadPendingOrders();
            return View(new FileUploadModel());
        }

        // POST: Handle upload
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Index(FileUploadModel model)
        {
            if (!ModelState.IsValid)
            {
                await LoadPendingOrders();
                return View(model);
            }

            try
            {
                // Only allow PDF files
                if (Path.GetExtension(model.ProofOfPayment.FileName).ToLower() != ".pdf")
                {
                    ModelState.AddModelError("", "Only PDF files are allowed for payment proof.");
                    await LoadPendingOrders();
                    return View(model);
                }

                // Upload PDF to Azure File Share, keep original filename
                string proofFileName = model.ProofOfPayment.FileName;
                await _azureStorageService.UploadToFileShareAsync(
                    model.ProofOfPayment,
                    "contracts",   // File share name
                    "payments"     // Directory
                );

                // Retrieve order
                var order = await _azureStorageService.GetEntityAsync<Order>(model.PartitionKey, model.OrderId);
                if (order == null)
                {
                    ModelState.AddModelError("", "Order not found.");
                    await LoadPendingOrders();
                    return View(model);
                }

                // Update order status + save proof filename
                order.Status = "Completed";
                order.ProofOfPaymentUrl = proofFileName; // store original name
                await _azureStorageService.UpdateEntityAsync(order);

                // --- Send order notification to queue ---
                var orderMessage = JsonSerializer.Serialize(new
                {
                    OrderId = order.RowKey,
                    CustomerName = order.UserName,
                    TotalPrice = order.TotalPrice,
                    Status = order.Status,
                    ProofUrl = order.ProofOfPaymentUrl
                });

                await _azureStorageService.SendMessageAsync("order-notifications", orderMessage);

                TempData["SuccessMessage"] = $"Payment proof uploaded. Order {order.RowKey} marked as Completed.";
                return RedirectToAction("Index", "Order");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error uploading payment proof.");
                ModelState.AddModelError("", "An error occurred while uploading. Please try again.");
                await LoadPendingOrders();
                return View(model);
            }
        }

        // Helper to reload dropdown
        private async Task LoadPendingOrders()
        {
            try
            {
                var orders = await _azureStorageService.GetAllEntitiesAsync<Order>();

                var pendingOrders = orders
                    .Where(o => o.Status == "Pending")
                    .Select(o => new SelectListItem
                    {
                        Value = o.RowKey,
                        Text = $"Order {o.RowKey} - {o.UserName} ({o.TotalPrice:C})"
                    }).ToList();

                ViewData["PendingOrders"] = pendingOrders;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading pending orders for upload.");
                ViewData["PendingOrders"] = new List<SelectListItem>();
                TempData["ErrorMessage"] = "Could not load pending orders.";
            }
        }
    }
}
