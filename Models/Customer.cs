using Azure;
using Azure.Data.Tables;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace ABC_Retailers.Models
{
    public class Customer : ITableEntity
    {
        public string? PartitionKey { get; set; }
        public string? RowKey { get; set; }
        

        [Required]
        public string FirstName { get; set; }

        [Required]
        public string LastName { get; set; }

        [Required]
        [EmailAddress]
        public string Email { get; set; }

        [Required]
        public string UserName { get; set; }

        public string ShippingAddress { get; set; }

        public DateTimeOffset? Timestamp { get; set; }
        public ETag ETag { get; set; }
    }
}
