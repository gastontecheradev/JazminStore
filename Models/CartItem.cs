using System.ComponentModel.DataAnnotations;

namespace Jazmin.Models;

public class CartItem
{
    public int Id { get; set; }

    [Required]
    public string UserId { get; set; } = string.Empty;
    public ApplicationUser? User { get; set; }

    public int ProductId { get; set; }
    public Product? Product { get; set; }

    [StringLength(10)]
    public string? Size { get; set; }

    public int Quantity { get; set; } = 1;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
