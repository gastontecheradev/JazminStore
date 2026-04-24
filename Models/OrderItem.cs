using System.ComponentModel.DataAnnotations;

namespace Jazmin.Models;

public class OrderItem
{
    public int Id { get; set; }

    public int OrderId { get; set; }
    public Order? Order { get; set; }

    public int ProductId { get; set; }
    public Product? Product { get; set; }

    [Required, StringLength(120)]
    public string ProductName { get; set; } = string.Empty;

    [StringLength(10)]
    public string? Size { get; set; }

    public decimal UnitPrice { get; set; }
    public int Quantity { get; set; }

    public decimal LineTotal => UnitPrice * Quantity;
}
