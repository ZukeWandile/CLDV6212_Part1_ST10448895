using Azure;
using Azure.Data.Tables;

namespace ABC_Retailers.Models
{
    public class Product: ITableEntity
    {
        public string PartitionKey { get; set; } = "Products"; // Default partition key for products
        public string RowKey { get; set; }// acts as product ID


        public string ProductName { get; set; }
        public string Description { get; set; }
        public double Price { get; set; }
        public int StockAvailabile { get; set; }
        public string ImageUrl { get; set; }

        public DateTimeOffset? Timestamp { get; set; }
        public ETag ETag { get; set; }
        // Additional properties can be added as needed

    }
}
