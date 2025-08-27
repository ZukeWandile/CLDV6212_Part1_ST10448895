using Azure.Data.Tables;
using Azure; // Required for ETag and DateTimeOffset

namespace ABC_Retailers.Models
{
    public class Order : ITableEntity
    {
        public string PartitionKey { get; set; } = "Orders";
        public string RowKey { get; set; }
        public string? ProductID { get; set; }
        public string UserName { get; set; }  // This is currently storing CustomerId
        public DateTime OrderDate { get; set; }
        public double Quantity { get; set; }
        public double TotalPrice { get; set; }
        public string Status { get; set; } = "Pending";
        public string? ProofOfPaymentUrl { get; set; }
        public DateTimeOffset? Timestamp { get; set; }
        public ETag ETag { get; set; }

        // Extra properties for display only (not stored in Azure Table)
        public string? ProductName { get; set; }
        public string? CustomerName { get; set; }
    }


}
