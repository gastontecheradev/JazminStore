using System.ComponentModel.DataAnnotations;

namespace Jazmin.Models;

public class Review
{
    public int Id { get; set; }

    public int ProductId { get; set; }
    public Product? Product { get; set; }

    public string UserId { get; set; } = string.Empty;
    public ApplicationUser? User { get; set; }

    // Link to the order that allowed this review (must be delivered)
    public int OrderId { get; set; }
    public Order? Order { get; set; }

    [Range(1, 5)]
    public int Rating { get; set; }

    [StringLength(1500)]
    public string? Comment { get; set; }

    public bool IsApproved { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
