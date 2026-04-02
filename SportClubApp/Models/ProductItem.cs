namespace SportClubApp.Models
{
    public sealed class ProductItem
    {
        public int ProductId { get; set; }
        public string Name { get; set; }
        public string Category { get; set; }
        public decimal Price { get; set; }
        public decimal? OldPrice { get; set; }
        public int? DiscountPercent { get; set; }
        public string ImagePath { get; set; }
        public int StockQty { get; set; }
        public decimal DiscountedPrice => OldPrice.HasValue && DiscountPercent.HasValue
            ? OldPrice.Value * (100 - DiscountPercent.Value) / 100m
            : Price;
    }
}
