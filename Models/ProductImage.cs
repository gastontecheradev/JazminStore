using System.ComponentModel.DataAnnotations;

namespace Jazmin.Models;

public class ProductImage
{
    public int Id { get; set; }

    public int ProductId { get; set; }
    public Product? Product { get; set; }

    [Required, StringLength(400)]
    public string Url { get; set; } = string.Empty;

    [StringLength(120)]
    public string? Alt { get; set; }

    public bool IsPrimary { get; set; }
    public int SortOrder { get; set; }
}
