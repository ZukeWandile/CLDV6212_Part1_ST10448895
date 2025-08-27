using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;

namespace ABC_Retailers.Models
{
    public class FileUploadModel
    {
        [Required]
        public IFormFile ProofOfPayment { get; set; }

        [Required]
        public string OrderId { get; set; }   // RowKey

        [Required]
        public string PartitionKey { get; set; } = "Orders"; // Always "Orders"

        [Display(Name = "Customer Name")]
        public string? CustomerName { get; set; }
    }


}
