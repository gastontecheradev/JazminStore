using System.ComponentModel.DataAnnotations;

namespace Jazmin.Models;

public class Category
{
    public int Id { get; set; }

    [Required, StringLength(60)]
    public string Name { get; set; } = string.Empty;

    [Required, StringLength(80)]
    public string Slug { get; set; } = string.Empty;

    [StringLength(250)]
    public string? Description { get; set; }

    public int SortOrder { get; set; }

    public bool IsActive { get; set; } = true;

    public List<Product> Products { get; set; } = new();
}
