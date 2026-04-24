namespace Jazmin.Models.ViewModels;

public class CatalogViewModel
{
    public List<Product> Products { get; set; } = new();
    public List<Category> Categories { get; set; } = new();
    public int? SelectedCategoryId { get; set; }
    public string? Search { get; set; }
    public string Sort { get; set; } = "new"; // new, price_asc, price_desc, rating
    public decimal? MinPrice { get; set; }
    public decimal? MaxPrice { get; set; }
    public Dictionary<int, int> CategoryCounts { get; set; } = new();
    public Dictionary<int, (double avg, int count)> Ratings { get; set; } = new();
    public HashSet<int> FavoriteIds { get; set; } = new();
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 12;
    public int TotalCount { get; set; }
    public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);
}

public class ProductDetailViewModel
{
    public Product Product { get; set; } = null!;
    public double AverageRating { get; set; }
    public int ReviewCount { get; set; }
    public List<Review> Reviews { get; set; } = new();
    public List<Product> Related { get; set; } = new();
    public Dictionary<int, (double avg, int count)> RelatedRatings { get; set; } = new();
    public bool IsFavorite { get; set; }
    public bool CanReview { get; set; } // logged-in AND has a delivered order with this product AND hasn't reviewed yet
    public int? PurchasedOrderId { get; set; } // order that unlocks the review
}
