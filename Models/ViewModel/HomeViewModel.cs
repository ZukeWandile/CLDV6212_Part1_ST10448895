namespace ABC_Retailers.Models.ViewModel
{
    public class HomeViewModel
    {
        // List of featured products for the homepage
        public List<Product> FeaturedProducts { get; set; } = new List<Product>();

        // Dashboard statistics
        public int CustomerCount { get; set; }
        public int ProductCount { get; set; }
        public int OrderCount { get; set; }
    }
}
