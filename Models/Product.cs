using System.ComponentModel.DataAnnotations;

namespace Jazmin.Models;

public class Product
{
    public int Id { get; set; }

    [Required, StringLength(120)]
    public string Name { get; set; } = string.Empty;

    [Required, StringLength(140)]
    public string Slug { get; set; } = string.Empty;

    [StringLength(2000)]
    public string? Description { get; set; }

    [StringLength(300)]
    public string? ShortDescription { get; set; }

    [Range(0, 9999999)]
    public decimal Price { get; set; }

    [Range(0, 9999999)]
    public decimal? ComparePrice { get; set; } // original price for discount display

    public int Stock { get; set; }

    public bool IsActive { get; set; } = true;
    public bool IsFeatured { get; set; }
    public bool IsNew { get; set; }

    public int CategoryId { get; set; }
    public Category? Category { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Sizes as CSV (e.g., "XS,S,M,L")
    [StringLength(100)]
    public string SizesCsv { get; set; } = "XS,S,M,L";

    // Colors as CSV (optional)
    [StringLength(200)]
    public string? ColorsCsv { get; set; }

    public List<ProductImage> Images { get; set; } = new();
    public List<Review> Reviews { get; set; } = new();
    public List<OrderItem> OrderItems { get; set; } = new();
    public List<Favorite> Favorites { get; set; } = new();

    // Computed / not mapped
    public string[] GetSizes() =>
        string.IsNullOrWhiteSpace(SizesCsv) ? Array.Empty<string>() :
        SizesCsv.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

    public string[] GetColors() =>
        string.IsNullOrWhiteSpace(ColorsCsv) ? Array.Empty<string>() :
        ColorsCsv.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

    public string PrimaryImageUrl =>
        Images.OrderByDescending(i => i.IsPrimary).ThenBy(i => i.SortOrder).FirstOrDefault()?.Url
        ?? "/img/placeholder.svg";
}
